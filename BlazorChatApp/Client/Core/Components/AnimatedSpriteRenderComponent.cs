using System.Threading.Tasks;
using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core.Assets;

namespace BlazorChatApp.Client.Core.Components
{
    public class AnimatedSpriteRenderComponent : BaseComponent, IRenderable
    {
        private readonly TransformComponent _transform;

        private int _currFramePosX = 0;
        private int _currFramePosY = 0;
        private int _currFrameIndex = 0;
        private long _lastUpdate = 0;
        
        private AnimationCollection.Animation _animation;

        public AnimatedSpriteRenderComponent(SceneObject owner) : base(owner)
        {
            _transform = owner.Components.Get<TransformComponent>();
        }


        public async ValueTask Render(SceneContext game, Canvas2DContext context)
        {
            if (null == Animation)
                return;

            if (game.GameTime.TotalMilliseconds - _lastUpdate > 1000f / Animation.Fps)
            {
                if (_currFrameIndex >= Animation.FramesCount)
                    _currFrameIndex = 0;

                _lastUpdate = game.GameTime.TotalMilliseconds;
                _currFramePosX = _currFrameIndex * Animation.FrameSize.Width;
                ++_currFrameIndex;
            }

            await context.SaveAsync();

            await context.TranslateAsync(_transform.World.Position.X + (MirrorVertically ? Animation.FrameSize.Width : 0f), _transform.World.Position.Y);

            await context.ScaleAsync(MirrorVertically ? -1f:1f, 1f);

            await context.DrawImageAsync(Animation.ImageRef,
                _currFramePosX, 0,
                Animation.FrameSize.Width, Animation.FrameSize.Height,
                0, 0,
                Animation.FrameSize.Width, Animation.FrameSize.Height);

            await context.RestoreAsync();
        }

        public AnimationCollection.Animation Animation
        {
            get => _animation;
            set
            {
                if (_animation == value)
                    return;
                _currFrameIndex = 0;
                _animation = value;
            }
        }


        public bool MirrorVertically { get; set; }
    }
}