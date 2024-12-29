using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Akka.Actor;
using Akka.Event;

using BlazorChatApp.Shared;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;


namespace BlazorChatApp.Server.Hubs
{
    // Define Tick message class
    public class Tick { }

    public class RoomActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        public Dictionary<string,UpdateUserPos> users = new Dictionary<string,UpdateUserPos>();

        private string roomName;

        private int userAutoNo = 0;

        private readonly IServiceScopeFactory scopeFactory;

        private readonly ConcurrentQueue<ChatMessage> chatHistory = new ConcurrentQueue<ChatMessage>();
        
        private const int MaxChatHistoryCount = 50;


        private List<UserInfo> borUserInfos = new List<UserInfo>();
        private List<UpdateUserPos> botUpdateUserPosList = new List<UpdateUserPos>();



        Random random = new Random();

        public RoomActor(string _roomName, IServiceScopeFactory _scopeFactory)
        {
            scopeFactory = _scopeFactory;            

            roomName = _roomName;

            log.Info($"Create Room{roomName}");

            string RandomColor = string.Format("#{0:X6}", random.Next(0xFFFFFF));

            string RandomColor2 = string.Format("#{0:X6}", random.Next(0xFFFFFF));

            // Schedule a timer to send a Tick message every 10 seconds
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(1),
                Self,
                new Tick(),
                Self
            );

            for (int i = 0; i < 10; i++)
            {
                string RandomColorN = string.Format("#{0:X6}", random.Next(0xFFFFFF));

                UserInfo userInfo = new UserInfo()
                {
                    Id = $"bot-{i}",
                    Name = $"Bot-{i}",
                    Color = RandomColorN
                };

                UpdateUserPos updateUserPos = new UpdateUserPos()
                {
                    Id = userInfo.Id,
                    Name = userInfo.Name,
                    PosX = random.Next(0, 800),
                    PosY = random.Next(0, 600),
                    AbsPosX = random.Next(0, 800),
                    AbsPosY = random.Next(0, 600),
                    ConnectionId = $"bot-connection-{i}"
                };

                users[userInfo.Id] = updateUserPos;
                borUserInfos.Add(userInfo);
                botUpdateUserPosList.Add(updateUserPos);
            }


            Receive<RoomCmd>(cmd => {
                log.Info("Received String message: {0}", cmd);
                //Sender.Tell(message);                
            });

            Receive<JoinRoom>(async cmd => {
                userAutoNo++;
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received JoinRoom message: {0}", jsonString);                
                string RandomColor =  string.Format("#{0:X6}", random.Next(0xFFFFFF));
                
                UserInfo userInfo = new UserInfo()
                { 
                    Id=cmd.UserInfo.Id,
                    Name=$"User-{userAutoNo}",
                    Color=RandomColor
                };

                // Default Position
                double posx = random.Next(0,300);
                double posy = random.Next(400,550);

                UpdateUserPos updateUserPos= new UpdateUserPos()
                { 
                    Id=cmd.UserInfo.Id,
                    Name=$"User-{userAutoNo}",
                    PosX=posx,PosY=posy,
                    AbsPosX=posx,AbsPosY=posy,
                    ConnectionId = cmd.ConnectionId
                };

                if(!users.ContainsKey(updateUserPos.Id))
                    users[cmd.UserInfo.Id] = updateUserPos;

                
                await OnJoinRoom(cmd.RoomInfo, userInfo, updateUserPos);
            });

            Receive<SyncRoom>(async cmd => {           
                //userAutoNo++;
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received SyncRoom message: {0}", jsonString);

                List<UpdateUserPos> updateUserPosList = users.Values.ToList();                
                //await hubConnection.SendAsync("OnSyncRoom", cmd.UserInfo, updateUserPosList);
                string RandomColor =  string.Format("#{0:X6}", random.Next(0xFFFFFF));

                UserInfo userInfo = new UserInfo()
                { 
                    Id=cmd.UserInfo.Id,
                    Name=$"User-{userAutoNo}",
                    Color=RandomColor
                };

                foreach (var botUserInfo in borUserInfos)
                {                    
                    await OnSyncRoom(botUserInfo, updateUserPosList);
                }

                await OnSyncRoom(userInfo, updateUserPosList);

            });

            Receive<ChatMessage>(async cmd => {           
                //userAutoNo++;
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received ChatMessage message: {0}", jsonString);

                ChatMessage chatMessage = new ChatMessage()
                { 
                    From = cmd.From,
                    Message = cmd.Message
                };

                AddChatMessageToHistory(chatMessage);

                await OnChatMessage(chatMessage);

            });

            Receive<UpdateUserPos>(async cmd => { 
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received UpdateUserPos message: {0}", jsonString);

                double AbsPosX = users[cmd.Id].AbsPosX + cmd.PosX;
                double AbsPosY= users[cmd.Id].AbsPosY + cmd.PosY;                  

                if(users.ContainsKey(cmd.Id))
                {
                    users[cmd.Id].PosX=cmd.PosX;
                    users[cmd.Id].PosY=cmd.PosY;
                    users[cmd.Id].AbsPosX=AbsPosX;
                    users[cmd.Id].AbsPosY=AbsPosY;
                }

                log.Info($"UpdateUser : X=>{users[cmd.Id].AbsPosX} Y=>{users[cmd.Id].AbsPosY}");

                UpdateUserPos updateUserPos = new UpdateUserPos()
                {
                    Id = cmd.Id,
                    Name = cmd.Name,
                    PosX = cmd.PosX,
                    PosY = cmd.PosY,
                    AbsPosX = AbsPosX,
                    AbsPosY = AbsPosY
                };

                await OnUpdateUserPos(updateUserPos);

            });            

            Receive<Disconnect>(async cmd => {                
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received Disconnect message: {0}", jsonString);
                var disconnectUser = users.Values.Where(e=> e.ConnectionId == cmd.ConnectionId).FirstOrDefault();

                if(disconnectUser != null)
                {
                    if(users.ContainsKey(disconnectUser.Id))
                    {
                        users.Remove(disconnectUser.Id);

                        var leaveMsg = new LeaveRoom()
                        {
                            UserInfo = new UserInfo(){ Id =disconnectUser.Id }
                        
                        };
                        await OnLeaveRoom(leaveMsg);
                    }
                }
            });

            Receive<LeaveRoom>(async cmd => {                
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received LeaveRoom message: {0}", jsonString);

                if(users.ContainsKey(cmd.UserInfo.Id))
                {
                    users.Remove(cmd.UserInfo.Id);
                    await OnLeaveRoom(cmd);
                }
            });

            

            // Handle Tick message
            Receive<Tick>(_ => {
                log.Info("Tick event triggered");
                
                foreach (var botUpdateUserPos1 in botUpdateUserPosList)
                {
                    bool isMove = random.Next(5) == 0;

                    if(isMove)
                    {
                        MoveBot(botUpdateUserPos1);
                    }
                }
                
            });

        }

        private void MoveBot(UpdateUserPos botUpdateUserPos)
        {
            double moveX = random.NextDouble() * 40 - 20; // Move between -20 and 20
            double moveY = random.NextDouble() * 40 - 20; // Move between -20 and 20

            double newPosX = botUpdateUserPos.AbsPosX + moveX;
            double newPosY = botUpdateUserPos.AbsPosY + moveY;

            bool isYDirection = random.Next(4) == 0;

            bool isHelloMessage = random.Next(50) == 0;

            // Ensure bot stays within the screen boundaries (e.g., 0 to 800 for X and 0 to 600 for Y)
            if (isYDirection && newPosY >= 50 && newPosY <= 600)
            {
                botUpdateUserPos.PosX = 0;
                botUpdateUserPos.PosY = moveY;
                botUpdateUserPos.AbsPosY = newPosY;
                // Update bot position
                Self.Tell(botUpdateUserPos);
            }
            else if (newPosX >= 50 && newPosX <= 800)
            {
                botUpdateUserPos.PosX = moveX;
                botUpdateUserPos.PosY = 0;
                botUpdateUserPos.AbsPosX = newPosX;
                // Update bot position
                Self.Tell(botUpdateUserPos);
            }

            // Send random New Year greeting
            string[] greetings = new string[]
            {
            "소원을 들어주는 메타공간 새해 복 많이 받으세요~",
            "미래는 현재 우리가 무엇을 하는가에 달려 있다. - 마하트마 간디",
            "성공은 열심히 노력하며 기다리는 사람에게 찾아온다. - 토마스 에디슨",
            "당신이 할 수 있다고 믿든 할 수 없다고 믿든 믿는 대로 될 것이다. - 헨리 포드",
            "위대한 일은 작은 일들이 모여 이루어진다. - 빈센트 반 고흐",
            "꿈을 이루기 위한 첫 걸음은 깨어나는 것이다. - J.M. 파워",
            "가장 큰 영광은 결코 넘어지지 않는 데 있는 것이 아니라 넘어질 때마다 다시 일어서는 데 있다. - 공자",
            "삶을 변화시키는 유일한 방법은 행동이다. - 에리카 종",
            "위대한 행동을 하려면 꿈을 크게 가져야 한다. - 마리우스 푸즈니",
            "한계를 넘어서야 성장할 수 있다. - 알베르 카뮈",
            "성공으로 가는 비결은 실패의 두려움을 이겨내는 것이다. - 찰스 스윈돌",
            "미래를 계획하는 가장 좋은 방법은 현재를 최대한 활용하는 것이다. - 알란 케이",
            "작은 기회로부터 위대한 일이 시작된다. - 데모스테네스",
            "위험을 감수하지 않는다면 평범한 삶에 머물게 된다. - 짐 론",
            "천리 길도 한 걸음부터 시작된다. - 노자",
            "목표는 눈앞의 장애물을 넘어설 수 있는 용기를 준다. - 파블로 피카소",
            "성공이란 자신이 좋아하는 일을 하면서 살아가는 것이다. - 크리스 가드너",
            "하루하루를 최선을 다해 살아가라. 그것이 삶의 비결이다. - 스티브 잡스",
            "우리의 가장 큰 약점은 포기하는 데 있다. 성공하기 위한 가장 확실한 방법은 한 번만 더 시도하는 것이다. - 토마스 에디슨",
            "위대한 마음은 위대한 목표를 가지고 있다. - 랄프 왈도 에머슨",
            "삶의 진정한 성공은 마음속의 평화에서 비롯된다. - 달라이 라마",
            "현재를 살아가라. 미래는 스스로 준비될 것이다. - 알버트 아인슈타인",
            "오늘의 결단이 내일의 운명을 결정한다. - 윈스턴 처칠",
            "목표가 없는 인생은 방향을 잃은 배와 같다. - 토니 로빈스",
            "도전은 우리를 강하게 만들고, 역경은 우리를 단단하게 만든다. - 브루스 리",
            "실패는 성공으로 가는 길을 가르쳐 주는 교사이다. - 레오나르도 다 빈치",
            "변화는 고통스럽지만, 변화하지 않는 것은 파멸로 이끈다. - 프랭클린 D. 루스벨트",
            "불가능은 단지 의견일 뿐이다. - 모하메드 알리",
            "위대한 사람들은 자신이 가진 가능성을 믿는 사람들이다. - 월트 디즈니",
            "작은 노력들이 쌓여서 위대한 결과를 만든다. - 존 우든",
            "꿈을 꾸는 사람은 그 꿈을 실현할 능력이 있다. - 나폴레옹 힐",
            "목표가 명확하다면 길은 보인다. - 빌 게이츠",
            "노력은 결코 배신하지 않는다. - 김연아"
            };

            string randomGreeting = greetings[random.Next(greetings.Length)];

            if (isHelloMessage)
            {
                ChatMessage chatMessage = new ChatMessage()
                {
                    From = new UserInfo { Id = botUpdateUserPos.Id, Name = botUpdateUserPos.Name, Color = botUpdateUserPos.Color },
                    Message = randomGreeting
                };

                //AddChatMessageToHistory(chatMessage);

                Self.Tell(chatMessage);
            }
        }

        private void AddChatMessageToHistory(ChatMessage chatMessage)
        {
            chatHistory.Enqueue(chatMessage);
            while (chatHistory.Count > MaxChatHistoryCount)
            {
                chatHistory.TryDequeue(out _);
            }
        }

        private string GetChatHistoryAsJson()
        {
            return JsonSerializer.Serialize(chatHistory.ToList());
        }

        public async Task OnJoinRoom(RoomInfo roomInfo, UserInfo user, UpdateUserPos updateUserPos)
        {
            using(var scope = scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.All.SendAsync("OnJoinRoom", roomInfo, user, updateUserPos);
            }            
        }

        public async Task OnSyncRoom(UserInfo user, List<UpdateUserPos> updateUserPos )
        {
            using(var scope = scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.All.SendAsync("OnSyncRoom", user, updateUserPos);
            }
        }

        public async Task OnLeaveRoom(LeaveRoom leaveRoom)
        {
            using(var scope = scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.All.SendAsync("OnLeaveRoom", leaveRoom);
            }            
        }

        public async Task OnUpdateUserPos(UpdateUserPos updatePos)
        {
            using(var scope = scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.All.SendAsync("OnUpdateUserPos", updatePos);
            }            
        }

        public async Task OnChatMessage(ChatMessage chatMessage)
        {
            using(var scope = scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.All.SendAsync("OnChatMessage", chatMessage);
            }            
        }
    }
}
