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

        private readonly Queue<DateTime> _feedTimestamps = new Queue<DateTime>();
        private const int FeedWindowSeconds = 10;
        private const int FeedMaxCount = 3;

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
                "디자이너",
                "QA"
            };

            for (int i = 0; i < 6; i++)
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
                var now = DateTime.UtcNow;

                // 10초 이내 타임스탬프만 남기고 큐 정리
                while (_feedTimestamps.Count > 0 && (now - _feedTimestamps.Peek()).TotalSeconds > FeedWindowSeconds)
                    _feedTimestamps.Dequeue();

                if (_feedTimestamps.Count >= FeedMaxCount)
                    return; // 10초 이내 3회 초과 시 무시

                _feedTimestamps.Enqueue(now);

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
                    "프론트,백엔드,기획자,인프라,디자이너,QA 중 다른직군의 관점에서 다른 피드견해를 주면됨 단 하나의 직군만 응답해  " +
                    "나는 짧게응답하는 봇이라 50자미만으로 응답. IT기술,개발방법론 또는 회고의 내용일수도 있음 " +
                    "응답 text형태는 다음과같음\n\n[프론트] : 응답값\n\n" +
                    "피드할 내용이 딱히 없으면 NA 로만 응답";

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
                        else if (response.Value.Content[0].Text.Contains("QA"))
                            updateUserPos = botUpdateUserPosList[5];
                        else
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
                    "프론트,백엔드,기획자,인프라,디자이너,QA 중 답변에 적합한 직군 선택. " +
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
                        else if (response.Value.Content[0].Text.Contains("QA"))
                            updateUserPos = botUpdateUserPosList[5];

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

                if (cmd.Message == "NA")
                    return;

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
    "[헤이데어] 실수? 비난이 아닌 학습의 기회입니다!",
    "[헤이데어] 문제보다 해결에 집중해보세요!",
    "[헤이데어] 잘한 것도, 아쉬운 것도 기록해두세요!",
    "[헤이데어] “누가” 아닌 “어떻게”를 이야기해요!",
    "[헤이데어] 말할 수 있는 분위기, 그게 회고의 시작입니다!",
    "[헤이데어] 마음을 열면, 인사이트가 보입니다!",
    "[헤이데어] 느낌보다 데이터로 대화하세요!",
    "[헤이데어] “다음엔 안 그러자”보다 “다음엔 이렇게 하자!”",
    "[헤이데어] 작지만 실행 가능한 개선안을 도출해보세요!",
    "[헤이데어] 회고는 끝이 아닌, 다음을 여는 시작입니다!",
    "[헤이데어] 서로의 관점을 이해하려고 노력해보세요!",
    "[헤이데어] 회고는 개인 평가가 아닙니다 – 팀 성장의 시간!",
    "[헤이데어] 무례 없이 솔직하게 말해보세요!",
    "[헤이데어] 다르게 본다고 틀린 건 아닙니다!",
    "[헤이데어] 작은 변화가 큰 결과를 만듭니다!",
    "[헤이데어] 정답보다 ‘우리 팀에 맞는 방법’을 찾아요!",
    "[헤이데어] 좋은 회고는 질문에서 시작됩니다!",
    "[헤이데어] 경청은 최고의 리더십입니다!",
    "[헤이데어] 피드백은 날카롭지 않아도 효과적일 수 있어요!",
    "[헤이데어] 원인보다 의도를 먼저 이해해보세요!",
    "[헤이데어] 회고도 실험입니다 – 다양한 포맷을 시도해보세요!",
    "[헤이데어] 칭찬도 회고의 중요한 일부입니다!",
    "[헤이데어] 실수를 공유할수록 팀은 단단해집니다!",
    "[헤이데어] 감정은 숨기지 말고, 존중하며 표현하세요!",
    "[헤이데어] 문제를 말하는 사람이 해결의 열쇠입니다!",
    "[헤이데어] 개선은 작고 명확한 실천에서 시작됩니다!",
    "[헤이데어] 다음 스프린트에 반영 가능한 액션으로 정리해보세요!",
    "[헤이데어] 모두의 시간이니, 모두의 참여가 필요해요!",
    "[헤이데어] 비판은 줄이고, 제안은 늘려보세요!",
    "[헤이데어] 실패담도 팀의 자산입니다!",
    "[헤이데어] “왜 이렇게 했지?”보다 “왜 그랬을까?”",
    "[헤이데어] 지나간 일에 머물지 말고, 앞으로 나아가요!",
    "[헤이데어] 도전한 것을 칭찬하는 회고를 해보세요!",
    "[헤이데어] 침묵도 하나의 신호입니다 – 놓치지 마세요!",
    "[헤이데어] 너무 많지 않게, 집중할 포인트를 정하세요!",
    "[헤이데어] 분위기를 먼저 따뜻하게 만들어보세요!",
    "[헤이데어] 누구나 말할 수 있는 분위기를 만드는 것도 회고예요!",
    "[헤이데어] 회고에 늦은 건 있어도, 헛된 건 없어요!",
    "[헤이데어] 배움과 개선이 있는 한, 실패는 없습니다!",
    "[헤이데어] 이 회고가 우리 팀을 한 단계 더 성장시킵니다!"
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
