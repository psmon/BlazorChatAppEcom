using System.Numerics;
using System.Threading.Tasks;
using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Animations;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Client.Core.Exceptions;

namespace BlazorChatApp.Client.ChatLand
{
    public class CharacterBrain : BaseComponent
    {
        private readonly Transform _transform;
        private readonly AnimationController _animationController;
        private readonly AnimatedSpriteRenderComponent _renderComponent;
        private const float MaxSpeed = 0.25f;

        private bool _lastMirror =false;
        private bool _isMe = false;        

        public CharacterBrain(AnimationCollection animationCollection, GameObject owner, bool isMe, string id) : base(owner, id)
        {
            _isMe = isMe;

            _transform = owner.Components.Get<Transform>() ??
                         throw new ComponentNotFoundException<Transform>();

            _renderComponent = owner.Components.Get<AnimatedSpriteRenderComponent>() ??
                               throw new ComponentNotFoundException<AnimatedSpriteRenderComponent>();

            _animationController = owner.Components.Get<AnimationController>() ??
                                   throw new ComponentNotFoundException<AnimationController>();
        }

        public override async ValueTask Update(GameContext game)
        {
            if(!_isMe) return;

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
                _transform.Direction = Vector2.UnitX;
                _renderComponent.MirrorVertically = false;
                _lastMirror = false;
                speed = MaxSpeed;
            }

            if (left.State == ButtonState.States.Down)
            {
                _transform.Direction = -Vector2.UnitX;
                _renderComponent.MirrorVertically = true;
                _lastMirror = true;
                speed = MaxSpeed;
            }

            if (ups.State == ButtonState.States.Down)
            {
                _transform.Direction = -Vector2.UnitY;
                _renderComponent.MirrorVertically = _lastMirror;
                speed = MaxSpeed;
            }

            if (downs.State == ButtonState.States.Down)
            {
                _transform.Direction = Vector2.UnitY;
                _renderComponent.MirrorVertically = _lastMirror;
                speed = MaxSpeed;
            }

            var acc = _transform.Direction * speed * game.GameTime.ElapsedTime;
            _transform.Position += acc;

            _animationController.SetBool("attacking", isAttacking);
            //_animationController.SetBool("jumping", isJumping);
            _animationController.SetFloat("speed", speed);
        }
    }
}