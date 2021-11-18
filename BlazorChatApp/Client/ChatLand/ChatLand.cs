﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Assets;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Shared;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.ChatLand
{
    public class ChatLand : GameContext
    {
        //private GameObject _chatLandGame;

        private readonly SceneGraph _sceneGraph;

        private AnimationCollection _animationCollection;

        private Canvas2DContext _context { get;set; }
        public DateTime LastRender { get;set; }

        public ChatLand(Canvas2DContext context, AnimationCollection animationCollection)
        {            
            _context = context;
            _animationCollection = animationCollection;
            _sceneGraph = new SceneGraph();            
            LastRender = DateTime.Now;            
        }

        public static async ValueTask<ChatLand> Create(BECanvasComponent canvas, Dictionary<string,ElementReference> resource, AnimationCollection animationCollection )
        {
            var canvasContext = await canvas.CreateCanvas2DAsync();
            
            var chatLandGame = new ChatLand(canvasContext, animationCollection);

            var chatField = new GameObject();

            chatField.Components.Add<ChatField>();
            chatField.Components.Get<ChatField>().resource = resource;

            chatLandGame._sceneGraph.Root.AddChild(chatField);


            var warrior = new GameObject();
            var animation = animationCollection.GetAnimation("Idle");
            chatLandGame.InitAnimationController(animationCollection, warrior);

            var sunTransform = new TransformComponent(warrior);
            sunTransform.Local.Position.X = canvas.Width / 2;
            sunTransform.Local.Position.Y = canvas.Height / 2;
            sunTransform.Local.Scale = new Vector2(1.5f);

            warrior.Components.Add(sunTransform);

            warrior.Components.Add(new CharacterBrain(animationCollection, warrior,true,"1234"));

            warrior.Components.Add(new AnimatedSpriteRenderComponent(warrior)
            {
                Animation = animation
            });

            chatLandGame._sceneGraph.Root.AddChild(warrior);


            return chatLandGame;
        }


        public void AddUser(string id, string name, double posx,double posy, bool isMe)
        {
            //_sceneGraph.Root.AddChild(warrior);

        }

        public void RemoveUser(string id)
        {
            //var chatField =_chatLandGame.Components.Get<ChatField>();
            //chatField.RemoveUser(id);
        }

        public void UpdateUserPos(UpdateUserPos updateUserPos)
        {
            //var characterBrain =_chatLandGame.Components.Get<CharacterBrain>();

            //chatField.UpdateUserPos(updateUserPos);

            //var chatField =_chatLandGame.Components.Get<ChatField>();
            //chatField.UpdateUserPos(updateUserPos);
        }

        public void ChatMessage(ChatMessage chatMessage)
        {
            //var chatField =_chatLandGame.Components.Get<ChatField>();
            //chatField.ChatMessage(chatMessage);
        }

        public StoreLink CollisionCheck(double x, double y)
        {
            //var chatField =_chatLandGame.Components.Get<ChatField>();
            //return chatField.CollisionCheck(x, y);
            return null;
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
            await _sceneGraph.Update(this);
        }

        protected override async ValueTask Render()
        {
            await _context.BeginBatchAsync();
            await _context.ClearRectAsync(0, 0, Display.Size.Width, Display.Size.Height);

            await Render(_sceneGraph.Root);
            
            await _context.EndBatchAsync();
        }

        private async ValueTask Render(GameObject node)
        {
            if (null == node)
                return;

            foreach(var component in node.Components)
                if (component is IRenderable renderable)
                    await renderable.Render(this, _context);

            foreach (var child in node.Children)
                await Render(child);
        }
    }
}
