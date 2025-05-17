using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Akka.Actor;
using Akka.Event;
using Akka.Util;

using BlazorChatApp.Client.Pages;
using BlazorChatApp.Shared;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

using OpenAI;
using OpenAI.Chat;

using MyChatMessage = BlazorChatApp.Shared.ChatMessage;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;


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

        private readonly ConcurrentQueue<MyChatMessage> chatHistory = new ConcurrentQueue<MyChatMessage>();
        
        private const int MaxChatHistoryCount = 50;

        private OpenAIClient openAIClient;


        private List<UserInfo> borUserInfos = new List<UserInfo>();
        private List<UpdateUserPos> botUpdateUserPosList = new List<UpdateUserPos>();

        private DateTime _lastFeedTime = DateTime.MinValue;


        Random random = new Random();

        // ChatCompleted 사용 예시 메서드
        private async Task<string> GetChatCompletionAsync(string userMessage)
        {
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant."),                
                new UserChatMessage(userMessage)
            };


            var response = await openAIClient.GetChatClient("gpt-3.5-turbo")
                .CompleteChatAsync(messages);
            
            return response.ToString();

        }
        private List<MyChatMessage> GetLastChatMessages(int count)
        {
            return chatHistory.Reverse().Take(count).Reverse().ToList();
        }

        public RoomActor(string _roomName, IServiceScopeFactory _scopeFactory)
        {
            // 환경변수에서 OpenAI API 키를 읽어옴
            var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(openAIApiKey))
                throw new InvalidOperationException("OPENAI_API_KEY 환경변수가 설정되어 있지 않습니다.");

            openAIClient = new OpenAIClient(openAIApiKey);

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

            List<string> roles = new List<string>
            {
                "프론트",
                "백엔드",
                "기획자",
                "인프라",
                "디자이너"
            };

            for (int i = 0; i < 5; i++)
            {
                string RandomColorN = string.Format("#{0:X6}", random.Next(0xFFFFFF));

                UserInfo userInfo = new UserInfo()
                {
                    Id = $"bot-{i}",
                    Name = $"Bot-{i}",
                    Color = RandomColorN,
                    Role = roles[i]
                };

                UpdateUserPos updateUserPos = new UpdateUserPos()
                {
                    Id = userInfo.Id,
                    Name = userInfo.Name,
                    PosX = random.Next(0, 600),
                    PosY = random.Next(300, 450),
                    AbsPosX = random.Next(0, 600),
                    AbsPosY = random.Next(300, 450),
                    Role = userInfo.Role,
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
                    Color=RandomColor,
                    Role = "user"
                };

                // Default Position
                double posx = random.Next(0,300);
                double posy = random.Next(300, 450);

                UpdateUserPos updateUserPos= new UpdateUserPos()
                { 
                    Id=cmd.UserInfo.Id,
                    Name=$"User-{userAutoNo}",
                    Role = cmd.UserInfo.Role,
                    PosX =posx,PosY=posy,
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
                    Color=RandomColor,
                    Role = cmd.UserInfo.Role
                };

                foreach (var botUserInfo in borUserInfos)
                {                    
                    await OnSyncRoom(botUserInfo, updateUserPosList);
                }

                await OnSyncRoom(userInfo, updateUserPosList);

            });

            Receive<ChatGptFeedRequest>(cmd =>
            {
                // 10초 이내면 피드백 무시
                if ((DateTime.UtcNow - _lastFeedTime).TotalSeconds < 10)
                    return;

                _lastFeedTime = DateTime.UtcNow;

                if (cmd.BotMessage.Contains("헤이데어"))
                    return;

                // 최근 채팅 메시지 10개 추출, 문맥연결용
                var lastMessages = GetLastChatMessages(10);

                // 최근 메시지를 문자열로 변환
                var historyText = string.Join("\n", lastMessages.Select(m => $"[{m.From?.Name ?? "User"}]: {m.Message}"));

                // System 프롬프트 생성
                var systemPrompt =
                    $"아래는 최근 대화 내역입니다. 이 문맥이 필요하면 참고하세요.\n" +
                    $"{historyText}\n" +
                    $"==================="+
                    $"{cmd.BotMessage}" +
                    "프론트,백엔드,기획자,인프라,디자이너중 다른직군의 관점에서 다른 피드견해를 주면됨 " +
                    "나는 짧게응답하는 봇이라 50자미만으로 응답. " +
                    "응답 text형태는 다음과같음\n\n[프론트] : 응답값\n\n" +
                    "피드할 내용이 딱히 없고 동일 직군피드백이라고하면 NA로만 응답";

                var messages = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(cmd.BotMessage)
                };

                // 비동기 작업을 PipeTo(Self)로 전달
                openAIClient.GetChatClient("gpt-4o")
                    .CompleteChatAsync(messages)
                    .ContinueWith(task =>
                    {
                        var response = task.Result;
                        UpdateUserPos updateUserPos = botUpdateUserPosList[0];

                        bool hasFeed= true;

                        if (response.Value.Content[0].Text.Contains("프론트"))
                            updateUserPos = botUpdateUserPosList[0];
                        else if (response.Value.Content[0].Text.Contains("백엔드"))
                            updateUserPos = botUpdateUserPosList[1];
                        else if (response.Value.Content[0].Text.Contains("기획자"))
                            updateUserPos = botUpdateUserPosList[2];
                        else if (response.Value.Content[0].Text.Contains("인프라"))
                            updateUserPos = botUpdateUserPosList[3];
                        else if (response.Value.Content[0].Text.Contains("디자이너"))
                            updateUserPos = botUpdateUserPosList[4];
                        else if (response.Value.Content[0].Text.Contains("NA"))
                            hasFeed = false;

                        var chatMessage = new MyChatMessage()
                        {
                            From = new UserInfo { Id = updateUserPos.Id, Name = updateUserPos.Name, Color = updateUserPos.Color },
                            Message = response.Value.Content[0].Text,
                            IsVisible = hasFeed
                        };
                        return chatMessage;

                    })
                    .PipeTo(Self);


            });

            Receive<ChatGptRequest> ( cmd =>
            {
                // 최근 채팅 메시지 10개 추출, 문맥연결용
                var lastMessages = GetLastChatMessages(10);

                // 최근 메시지를 문자열로 변환
                var historyText = string.Join("\n", lastMessages.Select(m => $"[{m.From?.Name ?? "User"}]: {m.Message}"));

                // System 프롬프트 생성
                var systemPrompt =
                    $"아래는 최근 대화 내역입니다. 이 문맥이 필요하면 참고하세요.\n" +
                    $"{historyText}\n" +
                    "프론트,백엔드,기획자,인프라,디자이너 중 답변에 적합한 직군 선택. " +
                    "적합하지 않으면 5개중 랜덤하게 선택, 나는 짧게응답하는 봇이라 50자미만으로 응답. " +
                    "응답 text형태는 다음과같음\n\n[프론트] : 응답값\n\n";


                var messages = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(cmd.UserMessage)
                };

                // 비동기 작업을 PipeTo(Self)로 전달
                openAIClient.GetChatClient("gpt-4o")
                    .CompleteChatAsync(messages)
                    .ContinueWith(task =>
                    {
                        var response = task.Result;
                        UpdateUserPos updateUserPos = botUpdateUserPosList[0];

                        if (response.Value.Content[0].Text.Contains("프론트"))
                            updateUserPos = botUpdateUserPosList[0];
                        else if (response.Value.Content[0].Text.Contains("백엔드"))
                            updateUserPos = botUpdateUserPosList[1];
                        else if (response.Value.Content[0].Text.Contains("기획자"))
                            updateUserPos = botUpdateUserPosList[2];
                        else if (response.Value.Content[0].Text.Contains("인프라"))
                            updateUserPos = botUpdateUserPosList[3];
                        else if (response.Value.Content[0].Text.Contains("디자이너"))
                            updateUserPos = botUpdateUserPosList[4];

                        var chatMessage = new MyChatMessage()
                        {
                            From = new UserInfo { Id = updateUserPos.Id, Name = updateUserPos.Name, Color = updateUserPos.Color },
                            Message = response.Value.Content[0].Text
                        };
                        return chatMessage;
                    })
                    .PipeTo(Self);

            });



            Receive<MyChatMessage>(async cmd => {                
                if(!cmd.IsVisible) return;

                //userAutoNo++;
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received ChatMessage message: {0}", jsonString);

                MyChatMessage chatMessage = new MyChatMessage()
                { 
                    From = cmd.From,
                    Message = cmd.Message
                };

                AddChatMessageToHistory(chatMessage);

                if (!cmd.From.Id.Contains("bot"))
                {
                    this.Self.Tell(new ChatGptRequest()
                    {
                        UserMessage = cmd.Message
                    });
                }
                else
                {
                    this.Self.Tell(new ChatGptFeedRequest()
                    {
                        BotMessage = cmd.Message
                    });
                }

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
                    Role = cmd.Role,
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

            bool isHelloMessage = random.Next(200) > 180;

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
            string[] heyTherePromos = new string[]
{
    "[헤이데어] 망설이는 고객, 우리가 붙잡아 드릴게요!",
    "[헤이데어] 고객 행동에 딱 맞춘 메시지, 이제는 기본입니다!",
    "[헤이데어] 이메일보다 10배 효과적인 온사이트 메시지!",
    "[헤이데어] 고객이 이탈하기 전, 먼저 말을 겁니다!",
    "[헤이데어] 방문자 행동에 따라 콘텐츠를 자동 추천해보세요!",
    "[헤이데어] 구매 전환율? 쑥쑥 올라갑니다!",
    "[헤이데어] 한눈에 보는 고객 인사이트, 대시보드로 끝!",
    "[헤이데어] 홍보와 CS를 동시에? 당연히 됩니다!",
    "[헤이데어] 인스타그램, 유튜브 콘텐츠도 바로 연결하세요!",
    "[헤이데어] 온사이트에서, 오프사이트까지. 전방위 메시지!",
    "[헤이데어] 프로모션 페이지 연결로 클릭률 UP!",
    "[헤이데어] 친구톡, 알림톡도 타겟팅 가능합니다!",
    "[헤이데어] 설문, 쿠폰, 이벤트… 메시지로 다 해보세요!",
    "[헤이데어] 페이지마다 다른 메시지로 고객 마음을 사로잡으세요!",
    "[헤이데어] 고객 관심사에 맞춘 버튼으로 클릭 유도!",
    "[헤이데어] 한 번의 설정으로 브랜드별 캠페인도 완성!",
    "[헤이데어] 데이터 기반 개인화 메시지로 구매 유도!",
    "[헤이데어] 홈 위젯으로 시선을 사로잡는 콘텐츠 큐레이션!",
    "[헤이데어] 고객이 좋아하는 콘텐츠를 정확히 보여주세요!",
    "[헤이데어] 고객이 머무는 모든 채널에서 말 걸어보세요!",
    "[헤이데어] 채널톡, 해피톡, 이메일까지 한번에 연결!",
    "[헤이데어] 콘텐츠 중심 마케팅, 우리가 완성합니다!",
    "[헤이데어] 고객을 위한 맞춤 커뮤니케이션, 지금 시작하세요!",
    "[헤이데어] 고객 경험이 달라지면, 매출도 달라집니다!",
    "[헤이데어] 놓치고 싶지 않은 고객? 우리가 잡습니다!",
    "[헤이데어] 이탈 전 행동을 포착하고, 맞춤 콘텐츠를 보여주세요!",
    "[헤이데어] 세일즈가 아닌 대화를 시작하세요.",
    "[헤이데어] CS + 마케팅 = All-in-One 솔루션!",
    "[헤이데어] 고객의 눈길을 끄는 단 한 줄, 여기서 시작됩니다!",
    "[헤이데어] 데이터 기반 마케팅, 어렵지 않아요!",
    "[헤이데어] 고객도, 운영자도 만족하는 마케팅 자동화!",
    "[헤이데어] 보고, 분석하고, 바로 액션하세요!",
    "[헤이데어] 이제는 메시지가 아닌 ‘경험’을 설계하세요",
    "[헤이데어] 웹사이트 방문자도 ‘고객’이 되게 하세요!",
    "[헤이데어] 성공하는 브랜드는 ‘대화’를 시작합니다",
    "[헤이데어] 무심코 스쳐 가던 고객을 멈추게 합니다!",
    "[헤이데어] 한 명의 고객도 허투루 보내지 마세요!",
    "[헤이데어] 맞춤 메시지 하나면 결과가 달라집니다!",
    "[헤이데어] 더 이상 복잡한 설정 없이, 바로 실행하세요!",
    "[헤이데어] 고객은 질문하고 싶어해요 – 먼저 말을 걸어주세요!",
    "[헤이데어] 고객 여정의 시작과 끝을 함께합니다 😊",
    "[헤이데어] 블룸AI는 루나소프트-엠비아이솔류션 합병 법인명입니다."
};

            string randomGreeting = heyTherePromos[random.Next(heyTherePromos.Length)];

            if (isHelloMessage && botUpdateUserPos.Id=="bot-2")
            {
                MyChatMessage chatMessage = new MyChatMessage()
                {
                    From = new UserInfo { Id = botUpdateUserPos.Id, Name = botUpdateUserPos.Name, Color = botUpdateUserPos.Color },
                    Message = randomGreeting
                };

                //AddChatMessageToHistory(chatMessage);

                Self.Tell(chatMessage);
            }
        }

        private void AddChatMessageToHistory(MyChatMessage chatMessage)
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

        public async Task OnChatMessage(MyChatMessage chatMessage)
        {
            using(var scope = scopeFactory.CreateScope())
            {
                var wsHub = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                await wsHub.Clients.All.SendAsync("OnChatMessage", chatMessage);
            }            
        }
    }
}
