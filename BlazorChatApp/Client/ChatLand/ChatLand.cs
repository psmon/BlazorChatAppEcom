using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Shared;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.ChatLand
{
    public class ChatLand : GameContext
    {
        private GameObject _chatLandGame;        


        private Canvas2DContext _context { get;set; }
        public DateTime LastRender { get;set; }

        public ChatLand()
        {
            LastRender = DateTime.Now;            
        }

        public static async ValueTask<ChatLand> Create(BECanvasComponent canvas, Dictionary<string,ElementReference> resource )
        {            
            var chatLandGame = new GameObject();
            chatLandGame.Components.Add(new Transform(chatLandGame)
            {
                Position = Vector2.Zero,
                Direction = Vector2.One,
            });

            chatLandGame.Components.Add(new ChatField(chatLandGame));

            var canvasContext = await canvas.CreateCanvas2DAsync();
            var game = new ChatLand 
            {   
                _context = canvasContext,
                _chatLandGame= chatLandGame
            };

            var chatField = chatLandGame.Components.Get<ChatField>();
            chatField.resource = resource;

            return game;
        }


        public void AddUser(string id, string name, double posx,double posy)
        {
            var chatField =_chatLandGame.Components.Get<ChatField>();
            chatField.AddUser(id, name, posx, posy);
        }

        public void RemoveUser(string id)
        {
            var chatField =_chatLandGame.Components.Get<ChatField>();
            chatField.RemoveUser(id);
        }

        public void UpdateUserPos(UpdateUserPos updateUserPos)
        {
            var chatField =_chatLandGame.Components.Get<ChatField>();
            chatField.UpdateUserPos(updateUserPos);            
        }

        public void ChatMessage(ChatMessage chatMessage)
        {
            var chatField =_chatLandGame.Components.Get<ChatField>();
            chatField.ChatMessage(chatMessage);
        }

        public StoreLink CollisionCheck(double x, double y)
        {
            var chatField =_chatLandGame.Components.Get<ChatField>();
            return chatField.CollisionCheck(x, y);
        }

        protected override async ValueTask Update()
        {
            await _chatLandGame.Update(this);
        }

        protected override async ValueTask Render()
        {
            await _context.BeginBatchAsync();
            await _context.ClearRectAsync(0, 0, Display.Size.Width, Display.Size.Height);

            var chatField =_chatLandGame.Components.Get<ChatField>();
            await chatField.Render(_context);

            await _context.EndBatchAsync();
        }
    }
}
