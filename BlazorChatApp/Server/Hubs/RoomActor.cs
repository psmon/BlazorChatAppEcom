﻿using System;
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
    public class RoomActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        public Dictionary<string,UpdateUserPos> users = new Dictionary<string,UpdateUserPos>();

        private string roomName;

        private int userAutoNo = 0;

        private readonly IServiceScopeFactory scopeFactory;
        

        Random random= new Random();

        public RoomActor(string _roomName, IServiceScopeFactory _scopeFactory)
        {
            scopeFactory = _scopeFactory;            

            roomName = _roomName;

            log.Info($"Create Room{roomName}");

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
                double posx = random.Next(300,600);
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

                await OnChatMessage(chatMessage);

            });

            Receive<UpdateUserPos>(async cmd => { 
                string jsonString = JsonSerializer.Serialize(cmd);
                log.Info("Received UpdateUserPos message: {0}", jsonString);

                double AbsPosX = users[cmd.Id].PosX+cmd.PosX;
                double AbsPosY= users[cmd.Id].PosY+cmd.PosY;                  

                if(users.ContainsKey(cmd.Id))
                {
                    users[cmd.Id].PosX=AbsPosX;
                    users[cmd.Id].PosY=AbsPosY;
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
