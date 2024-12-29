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
                    PosY = random.Next(400, 550),
                    AbsPosX = random.Next(0, 800),
                    AbsPosY = random.Next(400, 550),
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
                double posy = random.Next(400, 550);

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
            double moveX = random.NextDouble() * 80 - 40; // Move between -20 and 20
            double moveY = random.NextDouble() * 80 - 40; // Move between -20 and 20

            double newPosX = botUpdateUserPos.AbsPosX + moveX;
            double newPosY = botUpdateUserPos.AbsPosY + moveY;

            bool isYDirection = random.Next(4) == 0;

            bool isHelloMessage = random.Next(50) == 0;

            // Ensure bot stays within the screen boundaries (e.g., 0 to 800 for X and 0 to 600 for Y)
            if (isYDirection && newPosY >= 400 && newPosY <= 550)
            {
                botUpdateUserPos.PosX = 0;
                botUpdateUserPos.PosY = moveY;
                botUpdateUserPos.AbsPosY = newPosY;
                // Update bot position
                Self.Tell(botUpdateUserPos);
            }
            else if (newPosX >= 50 && newPosX <= 700)
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
    "2024년은 건강과 행복으로 가득한 한 해 되세요!",
    "새해에는 모든 일이 술술 풀리기를 기원합니다!",
    "사랑과 평화가 가득한 한 해 되시길 바랍니다~",
    "새해에는 이루고자 하는 모든 목표 달성하시길!",
    "가족과 함께 행복한 새해 맞이하세요!",
    "2024년은 더욱 큰 기회로 가득 차기를!",
    "새해에는 건강이 최고! 모두 건강하세요!",
    "새해 첫날부터 웃음 가득하시길 바랍니다!",
    "희망과 꿈이 이루어지는 한 해 되세요!",
    "새해 복 많이 받으세요! 올해도 잘 부탁드려요~",
    "더 큰 성공과 성취의 한 해 되시길 바랍니다!",
    "행운이 가득한 2024년 되세요!",
    "새로운 도전을 두려워하지 않는 한 해 되세요!",
    "가족과 함께하는 따뜻한 새해 맞이하세요!",
    "소망하시는 일 모두 이뤄지길 기원합니다!",
    "행복과 기쁨이 넘치는 한 해 되세요!",
    "새해는 늘 건강하고 행복하세요!",
    "모두가 사랑으로 가득 찬 한 해 되시길!",
    "새해 첫날부터 좋은 일만 가득하세요!",
    "새로운 시작을 축하하며 새해 복 많이 받으세요!",
    "늘 웃음 가득한 한 해 보내세요!",
    "사랑하는 사람들과 함께 행복한 2024년 되세요!",
    "희망과 사랑이 넘치는 새해 되시길 바랍니다!",
    "성공과 행운이 함께하는 2024년 되세요!",
    "건강과 행복을 기원합니다. 새해 복 많이 받으세요!",
    "새해에는 모든 어려움이 사라지기를 기원합니다.",
    "가족과 친구들이 행복한 한 해 되세요!",
    "2024년은 모두의 꿈이 이루어지는 해 되세요!",
    "희망과 기쁨으로 가득한 한 해 보내세요!",
    "더 멋진 기회와 도전이 함께하기를 기원합니다.",
    "사랑과 평화가 넘치는 새해 되시길 바랍니다.",
    "새해에는 건강도 돈도 행운도 함께 하세요!",
    "2024년에는 모든 일이 잘 풀리길 기원합니다!",
    "새해 복 많이 받으시고 건강하세요!",
    "새해 첫날, 웃음으로 시작하세요!",
    "올해는 정말 행복한 일들만 가득하시길!",
    "가족과 함께 평안한 새해 맞이하세요!",
    "2024년에는 꿈을 이룰 수 있는 한 해 되세요!",
    "건강이 제일! 새해에도 건강 잘 챙기세요!",
    "항상 행복하고 웃음 넘치는 한 해 되시길 바랍니다.",
    "새해에는 새로운 기회가 가득하기를 바랍니다.",
    "행운과 성공이 가득한 한 해 되세요!",
    "사랑하는 사람들과 함께 더 행복한 한 해 되세요.",
    "새해 복 많이 받으시고 모든 일이 순조롭길!",
    "2024년은 더 큰 행복을 만들어가는 해 되세요!",
    "늘 감사하는 마음으로 새해를 맞이하세요!",
    "소원하시는 모든 일이 이루어지는 한 해 되세요.",
    "희망으로 가득 찬 새해를 시작하세요!",
    "모든 이들이 행복하고 평화로운 한 해 되길!",
    "로또 1등 되게 해주세요!",
    "좋은 직장에서 승진하거나 원하는 일자리 얻으세요!",
    "사랑하는 사람과 더 깊은 관계로 발전하세요!",
    "여행을 마음껏 즐길 수 있는 새해가 되길!",
    "좋은 책을 많이 읽고 지혜가 쌓이는 한 해 되세요!",
    "건강한 다이어트와 운동으로 멋진 몸매를 만드세요!",
    "취미와 여가 시간을 더 많이 가지는 한 해 되길!",
    "경제적으로 더 풍족하고 안정된 한 해 되세요!",
    "자기 계발에 성공하고 한 단계 성장하세요!",
    "새 친구들과 함께 즐거운 시간을 보내세요!",
    "사랑하는 가족과의 소중한 추억 많이 만드세요!",
    "자연과 함께하는 시간을 늘리며 힐링하세요!",
    "평소 배우고 싶었던 것을 배우는 한 해 되세요!"
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
