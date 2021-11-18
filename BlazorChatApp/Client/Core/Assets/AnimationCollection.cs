using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.Core.Assets
{
    
    public class AnimationCollection : IAsset
    {
        private readonly IDictionary<string, Animation> _animations;

        public AnimationCollection(string name)
        {
            this.Name = name;

            _animations = new Dictionary<string, Animation>();
        }

        public string Name { get; }

        public Animation GetAnimation(string name) => string.IsNullOrWhiteSpace(name) || !_animations.ContainsKey(name) ? null : _animations[name];

        private void AddAnimation(Animation animation)
        {
            if (animation == null) 
                throw new ArgumentNullException(nameof(animation));
            if(_animations.ContainsKey(animation.Name))
                throw new ArgumentException($"there is already an animation with the same name: {animation.Name}");

            _animations.Add(animation.Name, animation);
        }

        public class Animation
        {
            public Animation(string name, int fps, Size frameSize, int framesCount,
                ElementReference imageRef, string imageData, Size imageSize,
                AnimationCollection animations)
            {
                Name = name;
                Fps = fps;
                FrameSize = frameSize;
                FramesCount = framesCount;
                ImageRef = imageRef;
                ImageData = imageData;
                ImageSize = imageSize;

                this.FramesCount = this.ImageSize.Width / this.FrameSize.Width;
             
                animations.AddAnimation(this);
            }
            public string Name { get; }
            public int Fps { get; }
            public int FramesCount { get; }
            public Size FrameSize { get; }
            public Size ImageSize { get; }
            public ElementReference ImageRef { get; set; }
            public string ImageData { get; }
        }
    }
}