using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Animations;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Shared;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.ChatLand
{
    public class ChatLand : GameContext
    {
        private GameObject _chatLandGame;        

        private AnimationCollection _animationCollection;

        private Canvas2DContext _context { get;set; }
        public DateTime LastRender { get;set; }

        public ChatLand()
        {
            LastRender = DateTime.Now;            
        }

        public static async ValueTask<ChatLand> Create(BECanvasComponent canvas, Dictionary<string,ElementReference> resource, AnimationCollection animationCollection )
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
                _chatLandGame= chatLandGame,
                _animationCollection = animationCollection
            };

            var chatField = chatLandGame.Components.Get<ChatField>();
            chatField.resource = resource;

            return game;
        }


        public void AddUser(string id, string name, double posx,double posy, bool isMe)
        {
            //var chatField =_chatLandGame.Components.Get<ChatField>();
            //chatField.AddUser(id, name, posx, posy);

            var warrior = _chatLandGame;

            var animation = _animationCollection.GetAnimation("Idle");

            warrior.Components.Add(new Transform(warrior)
            {
                Position = new Vector2((float)posx, (float)posy),
                Direction = Vector2.Zero,
                Size = animation.FrameSize
            });

            warrior.Components.Add(new AnimatedSpriteRenderComponent(warrior)
            {
                Animation = animation
            });

            InitAnimationController(_animationCollection, warrior);

            warrior.Components.Add(new CharacterBrain(_animationCollection, warrior, isMe, id));

        }

        public void RemoveUser(string id)
        {
            var chatField =_chatLandGame.Components.Get<ChatField>();
            chatField.RemoveUser(id);
        }

        public void UpdateUserPos(UpdateUserPos updateUserPos)
        {
            var characterBrain =_chatLandGame.Components.Get<CharacterBrain>();

            //chatField.UpdateUserPos(updateUserPos);

            //var chatField =_chatLandGame.Components.Get<ChatField>();
            //chatField.UpdateUserPos(updateUserPos);
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

        private void InitAnimationController(AnimationCollection animationCollection, GameObject warrior)
        {
            var animationController = new AnimationController(warrior);
            animationController.SetFloat("speed", 0f);
            animationController.SetBool("attacking", false);
            animationController.SetBool("jumping", false);

            warrior.Components.Add(animationController);

            var idle = new AnimationState(animationCollection.GetAnimation("Idle"));
            animationController.AddState(idle);

            var run = new AnimationState(animationCollection.GetAnimation("Run"));
            animationController.AddState(run);

            var jump = new AnimationState(animationCollection.GetAnimation("Jump"));
            animationController.AddState(jump);

            var attack = new AnimationState(animationCollection.GetAnimation("Attack1"));
            animationController.AddState(attack);

            idle.AddTransition(run,new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetFloat("speed") > .1f
            });
            idle.AddTransition(attack, new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetBool("attacking")
            });
            idle.AddTransition(jump, new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetBool("jumping")
            });

            run.AddTransition(idle, new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetFloat("speed") < .1f
            });
            run.AddTransition(attack, new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetBool("attacking")
            });

            attack.AddTransition(idle, new Func<AnimationController, bool>[]
            {
                ctrl => !ctrl.GetBool("attacking")
            });

            jump.AddTransition(idle, new Func<AnimationController, bool>[]
            {
                ctrl => !ctrl.GetBool("jumping")
            });
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

            var spriteRenderer = _chatLandGame.Components.Get<AnimatedSpriteRenderComponent>();
            await spriteRenderer.Render(this, _context);
            
            await _context.EndBatchAsync();
        }
    }
}
