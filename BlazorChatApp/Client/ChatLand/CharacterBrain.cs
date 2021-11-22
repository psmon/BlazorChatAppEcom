using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Assets;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Client.Core.Exceptions;
using BlazorChatApp.Shared;

namespace BlazorChatApp.Client.ChatLand
{
    public class CharacterBrain : BaseComponent
    {
        private readonly TransformComponent _transform;

        private Transform _goal_transform = Transform.Identity();        

        private readonly AnimationController _animationController;
        private readonly AnimatedSpriteRenderComponent _renderComponent;
        
        private const float MaxSpeed = 0.25f;

        private const float StopSpeed = 0.55f;        

        private bool _lastMirror =false;

        private bool _isMe = false;

        private Queue<UpdateUserPos> _queue;

        private const int KeySpeed = 10;

        private int _keyTime = KeySpeed;

        public CharacterBrain(AnimationCollection animationCollection, GameObject owner, bool isMe, string id) : base(owner)
        {
            _isMe = isMe;

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

        public override async ValueTask Update(GameContext game)
        {
            await UpdateByQueue(game);
        }

        public async ValueTask UpdateByKey(GameContext game)
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

        public async ValueTask UpdateByQueue(GameContext game)
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
    }
}