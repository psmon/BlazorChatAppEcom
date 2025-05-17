using System;
using System.Collections.Generic;
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
    public class ChatLand : SceneContext
    {        
        private string MyID {get;set; }

        private readonly SceneGraph _sceneGraph;

        private Dictionary<string,AnimationCollection> _animationCollection;

        private Dictionary<string,bool> _isInitUser = new Dictionary<string, bool>();


        public ChatField ChatField {get;set; }

        private Canvas2DContext _context { get;set; }
        public DateTime LastRender { get;set; }

        public ChatLand(Canvas2DContext context, Dictionary<string,AnimationCollection> animationCollection)
        {            
            _context = context;
            _animationCollection = animationCollection;
            _sceneGraph = new SceneGraph();            
            LastRender = DateTime.Now;            
        }

        public static async ValueTask<ChatLand> Create(BECanvasComponent canvas, Dictionary<string,ElementReference> resource, 
            Dictionary<string,AnimationCollection> animationCollection)
        {
            var canvasContext = await canvas.CreateCanvas2DAsync();
            
            var chatLandGame = new ChatLand(canvasContext, animationCollection);

            var chatObj = new SceneObject();

            var chatField = new ChatField(chatObj);

            chatObj.Components.Add(chatField);

            chatObj.Components.Get<ChatField>().resource = resource;

            chatLandGame._sceneGraph.Root.AddChild(chatObj);

            chatLandGame.ChatField = chatField;

            return chatLandGame;
        }


        public void AddUser(string id, string name, string role, double posx,double posy, bool isMe, Dictionary<string,ElementReference> resource)
        {
            if (_isInitUser.ContainsKey(id))
            {
                return;
            }

            if (isMe) MyID = id;

            var warrior = new SceneObject();
            AnimationCollection animationCollection;

            int userIdx = int.Parse(name.Split("-")[1]);

            string avartarName = "";

            int roundIdx = userIdx % 5;

            if(roundIdx==0)
            {
                avartarName = "santa2";
            }
            else if(roundIdx==1)
            {
                avartarName = "elf2";
            }
            else if(roundIdx==2)
            {
                avartarName = "elf1";
            }
            else if(roundIdx==3)
            {
                avartarName = "elf2";
            }
            else
            {
                avartarName = "santa2";
            }

            animationCollection = _animationCollection[avartarName];

            var animation = animationCollection.GetAnimation("Idle");            

            var sunTransform = new TransformComponent(warrior);            
            sunTransform.Local.Position.X=(float)posx;
            sunTransform.Local.Position.Y=(float)posy;
            warrior.Components.Add(sunTransform);

            warrior.Components.Add(new AnimatedSpriteRenderComponent(warrior)
            {
                Animation = animation
            });

            InitAnimationController(avartarName, animationCollection, warrior);


            string myRole = "user";

            if (id == "bot-0")
            {
                myRole = "프론트";
            }
            else if (id == "bot-1")
            {
                myRole = "백엔드";
            }
            else if (id == "bot-2")
            {
                myRole = "기획자";
            }
            else if (id == "bot-3")
            {
                myRole = "인프라";
            }
            else if (id == "bot-4")
            {
                myRole = "디자이너";
            }


            var character = new Character(animationCollection, warrior ,isMe , id, name, myRole);

            character.resource = resource;

            warrior.Components.Add(character);            

            warrior.HashId = id;            

            _sceneGraph.Root.AddChild(warrior);

            _isInitUser[id] = true;

        }

        public void RemoveUser(string id)
        {
            _sceneGraph.Root.RemoveById(id);
        }

        public void UpdateUserPos(UpdateUserPos updateUserPos)
        {
            var characterBrain = _sceneGraph.Root.FindById<Character>(updateUserPos.Id);

            Keys keys = Keys.Idle;

            if(updateUserPos.PosX < 0 )
            {
                keys = Keys.Left;
            }
            if(updateUserPos.PosX > 0 )
            {
                keys = Keys.Right;
            }
            if(updateUserPos.PosY < 0 )
            {
                keys = Keys.Up;
            }
            if(updateUserPos.PosY > 0 )
            {
                keys = Keys.Down;
            }

            characterBrain.OnMoveKey(updateUserPos);
        }

        public void ChatMessage(ChatMessage chatMessage)
        {
            var characterBrain = _sceneGraph.Root.FindById<Character>(chatMessage.From.Id);
            characterBrain.OnChatMessage(chatMessage.Message);
        }

        public StoreLink CollisionCheck(double x, double y)
        {
            return ChatField.CollisionCheck(x, y);
        }

        private void InitAnimationController(string avartarName , AnimationCollection animationCollection, SceneObject warrior)
        {
            var animationController = new AnimationController(warrior);
            animationController.SetFloat("speed", 0f);
            animationController.SetBool("attacking", false);
            animationController.SetBool("jumping", false);

            warrior.Components.Add(animationController);

            var idle = new AnimationState(animationCollection.GetAnimation(avartarName + "-Idle"));
            animationController.AddState(idle);

            var run = new AnimationState(animationCollection.GetAnimation(avartarName + "-Run"));
            animationController.AddState(run);

            idle.AddTransition(run,new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetFloat("speed") > .1f
            });


            run.AddTransition(idle, new Func<AnimationController, bool>[]
            {
                ctrl => ctrl.GetFloat("speed") < .1f
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

        private async ValueTask Render(SceneObject node)
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
