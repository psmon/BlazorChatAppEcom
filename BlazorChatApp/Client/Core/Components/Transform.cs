using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Components
{
    public class Transform : BaseComponent
    {
        public Transform(GameObject owner) : base(owner)
        {
        }

        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Direction { get; set; } = Vector2.One;
    }
}
