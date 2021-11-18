using System;
using System.Threading.Tasks;

using Akka.Actor;

using BlazorChatApp.Shared;

using Microsoft.AspNetCore.SignalR;

namespace BlazorChatApp.Server.Hubs
{
    public class ChatHub : Hub
    {
        private ActorSystem actorSystem;

        private ActorSelection roomActor;

        public ChatHub(ActorSystem _actorSystem)
        {
            actorSystem = _actorSystem;
            roomActor = actorSystem.ActorSelection("user/room1");
        }

        public override Task OnDisconnectedAsync(Exception e) 
        {
            roomActor.Tell(new Disconnect(){ ConnectionId = Context.ConnectionId });            
            return Task.CompletedTask;
        }

        
        // Client To Server

        public async Task JoInRoom(JoinRoom joinRoom)
        {
            joinRoom.ConnectionId = Context.ConnectionId;

            roomActor.Tell(joinRoom);
        }

        public async Task SyncRoom(SyncRoom syncRoom)
        {
            roomActor.Tell(syncRoom);
        }

        public async Task LeaveRoom(LeaveRoom leaveRoom)
        {
            roomActor.Tell(leaveRoom);
        }

        public async Task UpdateUserPos(UpdateUserPos updateUserPos)
        {
            roomActor.Tell(updateUserPos);
        }

        public async Task ChatMessage(ChatMessage chatMessage)
        {
            roomActor.Tell(chatMessage);
        }
    }
}
