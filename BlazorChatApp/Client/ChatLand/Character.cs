using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Assets;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Client.Core.Exceptions;
using BlazorChatApp.Shared;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.ChatLand
{
    public class Character : BaseComponent, IRenderable
    {
        private readonly TransformComponent _transform;
        public string Name{ get;set; }

        public string Role { get; set; }
        public string ChatMessage{ get;set; }

        public Dictionary<string,ElementReference> resource {get;set; }

        private int ChatViewTime { get;set; } = 60*10;

        private Transform _goal_transform = Transform.Identity();        

        private readonly AnimationController _animationController;
        private readonly AnimatedSpriteRenderComponent _renderComponent;
        
        private const float MaxSpeed = 0.25f;           

        private bool _lastMirror =false;        

        private Queue<UpdateUserPos> _queue;

        private const int KeySpeed = 10;

        private bool _isMine;

        public Character(AnimationCollection animationCollection, SceneObject owner, bool isMine, string id, string name, string role) : base(owner)
        {
            Name = name;

            Role = role;

            _isMine = isMine;

            _queue= new Queue<UpdateUserPos>();
            
            _transform = owner.Components.Get<TransformComponent>() ??
                         throw new ComponentNotFoundException<TransformComponent>();

            _goal_transform.Position = _transform.Local.Position;

            _renderComponent = owner.Components.Get<AnimatedSpriteRenderComponent>() ??
                               throw new ComponentNotFoundException<AnimatedSpriteRenderComponent>();

            _animationController = owner.Components.Get<AnimationController>() ??
                                   throw new ComponentNotFoundException<AnimationController>();
        }

        public void OnMoveKey(UpdateUserPos updateUserPos)
        {
            _queue.Enqueue(updateUserPos);
        }

        public void OnChatMessage(string message)
        {
            ChatViewTime = 60*10;
            ChatMessage = message;
        }

        public override async ValueTask Update(SceneContext game)
        {
            await UpdateByQueue(game);
        }

        public async ValueTask UpdateByKey(SceneContext game)
        {
            var right = InputSystem.Instance.GetKeyState(Keys.Right);
            var left = InputSystem.Instance.GetKeyState(Keys.Left);

            var ups = InputSystem.Instance.GetKeyState(Keys.Up);
            var downs = InputSystem.Instance.GetKeyState(Keys.Down);

            var space = InputSystem.Instance.GetKeyState(Keys.Space);
            var up = InputSystem.Instance.GetKeyState(Keys.Up);

            var isAttacking = (space.State == ButtonState.States.Down);
            var isJumping = (up.State == ButtonState.States.Down);

            var speed = 0f;

            if (right.State == ButtonState.States.Down)
            {
                _transform.Local.Direction = Vector2.UnitX;
                _renderComponent.MirrorVertically = false;
                _lastMirror = false;
                speed = MaxSpeed;
            }

            if (left.State == ButtonState.States.Down)
            {
                _transform.Local.Direction = -Vector2.UnitX;
                _renderComponent.MirrorVertically = true;
                _lastMirror = true;
                speed = MaxSpeed;
            }

            if (ups.State == ButtonState.States.Down)
            {
                _transform.Local.Direction = -Vector2.UnitY;
                _renderComponent.MirrorVertically = _lastMirror;
                speed = MaxSpeed;
            }

            if (downs.State == ButtonState.States.Down)
            {
                _transform.Local.Direction = Vector2.UnitY;
                _renderComponent.MirrorVertically = _lastMirror;
                speed = MaxSpeed;
            }

            var acc = _transform.Local.Direction * speed * game.GameTime.ElapsedMilliseconds;
            _transform.Local.Position += acc;

            _animationController.SetBool("attacking", isAttacking);
            //_animationController.SetBool("jumping", isJumping);
            _animationController.SetFloat("speed", speed);
        }

        public async ValueTask UpdateByQueue(SceneContext game)
        {
            UpdateUserPos updateUserPos;            

            _queue.TryDequeue(out updateUserPos);

            float moveX , moveY;

            if(updateUserPos != null)
            {
                moveX = (float)updateUserPos.PosX;
                moveY = (float)updateUserPos.PosY;

                _goal_transform.Position.X = (float)updateUserPos.AbsPosX;
                _goal_transform.Position.Y = (float)updateUserPos.AbsPosY;

                if(moveX > 0 ){
                    _goal_transform.Direction = Vector2.UnitX;
                }
                if(moveX < 0 ){
                    _goal_transform.Direction = -Vector2.UnitX;
                }
                if(moveY < 0 ){
                    _goal_transform.Direction = -Vector2.UnitY;
                }
                if(moveY > 0 ){
                    _goal_transform.Direction = Vector2.UnitY;
                }
            }

            var speed = 0f;

            float diffX = _goal_transform.Position.X - _transform.Local.Position.X;
            float diffY = _goal_transform.Position.Y - _transform.Local.Position.Y;

            if (diffX > 0 && _goal_transform.Direction == Vector2.UnitX)
            {
                _transform.Local.Direction = Vector2.UnitX;
                _renderComponent.MirrorVertically = false;
                _lastMirror = false;
                speed = MaxSpeed;
            }

            if (diffX < 0 && _goal_transform.Direction == -Vector2.UnitX)
            {
                _transform.Local.Direction = -Vector2.UnitX;
                _renderComponent.MirrorVertically = true;
                _lastMirror = true;
                speed = MaxSpeed;
            }

            if ( diffY < 0 && _goal_transform.Direction == -Vector2.UnitY)
            {
                _transform.Local.Direction = -Vector2.UnitY;
                _renderComponent.MirrorVertically = _lastMirror;
                speed = MaxSpeed;
            }

            if ( diffY > 0 && _goal_transform.Direction == Vector2.UnitY)
            {
                _transform.Local.Direction = Vector2.UnitY;
                _renderComponent.MirrorVertically = _lastMirror;
                speed = MaxSpeed;
            }

            var acc = _transform.Local.Direction * speed * game.GameTime.ElapsedMilliseconds;
            _transform.Local.Position += acc;

            _animationController.SetFloat("speed", speed);
        }

        public async ValueTask Render(SceneContext game, Canvas2DContext context)
        {
            string NameText = Name;

            if(!string.IsNullOrEmpty(ChatMessage))
            {
                NameText = NameText + "-" + ChatMessage;
                ChatViewTime--;
            }

            if(ChatViewTime<0)
            {
                ChatMessage = string.Empty;
            }

            await context.SaveAsync();

            //닉네임
            await context.SetFontAsync("14px 바탕체");            
            await context.SetFillStyleAsync("Blue");
            await context.FillTextAsync(Name, 
                _transform.Local.Position.X+10, _transform.Local.Position.Y + 75);

            //Role
            await context.SetFontAsync("14px 바탕체");
            await context.SetFillStyleAsync("White");
            await context.FillTextAsync(Role,
                _transform.Local.Position.X + 10, _transform.Local.Position.Y + 90);

            if (!string.IsNullOrEmpty(ChatMessage))
            {
                //채팅 Box
                //int dynamicWith = 50 + ((ChatMessage.Length -3)*15);
                int dynamicWidth = 50 + ((ChatMessage.Length - 3) * 15);
                int dynamicHeight = 50;

                //await context.DrawImageAsync(resource["img-chatbox"], _transform.Local.Position.X + 20, _transform.Local.Position.Y - 40, dynamicWith, 50);

                // 캐릭터의 중심점 머리 위로 위치 조정
                float boxX = _transform.Local.Position.X - dynamicWidth / 2 + 30;
                float boxY = _transform.Local.Position.Y - 60; // 캐릭터 머리 위로 조정

                float tailWidth = 10;
                float tailHeight = 10;
                int radius = 10;

                // 말풍선 배경
                await context.BeginPathAsync();
                await context.MoveToAsync(boxX + radius, boxY);
                await context.ArcToAsync(boxX + dynamicWidth, boxY, boxX + dynamicWidth, boxY + radius, radius);
                await context.ArcToAsync(boxX + dynamicWidth, boxY + dynamicHeight, boxX + dynamicWidth - radius, boxY + dynamicHeight, radius);
                await context.LineToAsync(boxX + dynamicWidth / 2 + tailWidth, boxY + dynamicHeight);
                await context.LineToAsync(boxX + dynamicWidth / 2, boxY + dynamicHeight + tailHeight);
                await context.LineToAsync(boxX + dynamicWidth / 2 - tailWidth, boxY + dynamicHeight);
                await context.ArcToAsync(boxX, boxY + dynamicHeight, boxX, boxY + dynamicHeight - radius, radius);
                await context.ArcToAsync(boxX, boxY, boxX + radius, boxY, radius);
                await context.ClosePathAsync();

                if(NameText.Contains("Bot"))
                {
                    await context.SetFillStyleAsync("#4CAF50");                    
                }
                else
                {
                    await context.SetFillStyleAsync("#03a9f4");
                }

                await context.FillAsync();

                // 말풍선 테두리
                await context.SetStrokeStyleAsync("White");
                await context.StrokeAsync();


                //채팅 메시징
                await context.SetFontAsync("bold 14px 바탕체");
                await context.SetFillStyleAsync("White");
                await context.FillTextAsync(ChatMessage,
                    boxX + 3, boxY + 20);
            }

            if(_isMine)
            {
            }

            await context.RestoreAsync();                        
        }
    }
}