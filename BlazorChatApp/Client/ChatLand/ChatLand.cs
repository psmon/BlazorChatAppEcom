using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.ChatLand;
using BlazorChatApp.Client.Core;
using BlazorChatApp.Shared;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.ChatLand
{
    public class ChatLand : GameContext
    {
        ulong frameCnt = 0;
        private Dictionary<string,ElementReference> resource {get;set; }
        private Field BallField { get;set; }           
        private Canvas2DContext _context { get;set; }
        public DateTime LastRender { get;set; }

        public ChatLand()
        {
            LastRender = DateTime.Now;            
        }

        public static async ValueTask<ChatLand> Create(BECanvasComponent canvas, Dictionary<string,ElementReference> resource )
        {  
            var canvasContext = await canvas.CreateCanvas2DAsync();
            var game = new ChatLand 
            {   
                _context = canvasContext,
                BallField= new Field(canvasContext),
                resource = resource
            };

            game.BallField.resource = resource;

            return game;
        }

        public async ValueTask GameLoop(float timeStamp)
        {

        }



        public void AddUser(string id, string name, double posx,double posy)
        {
            BallField.AddUser(id, name, posx, posy);
        }

        public void RemoveUser(string id)
        {            
            BallField.RemoveUser(id);
        }

        public void UpdateUserPos(UpdateUserPos updateUserPos)
        {
            BallField.UpdateUserPos(updateUserPos);            
        }

        public void ChatMessage(ChatMessage chatMessage)
        {
            BallField.ChatMessage(chatMessage);
        }

        public StoreLink CollisionCheck(double x, double y)
        {
            return BallField.CollisionCheck(x, y);
        }

        protected override async ValueTask Update()
        {
            await BallField.Update();
        }

        protected override async ValueTask Render()
        {
            await BallField.Render();
        }
    }
}
