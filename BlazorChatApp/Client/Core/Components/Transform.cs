﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Components
{
    public class Transform
    {
        public Vector2 Position;

        public Vector2 Scale;

        public Vector2 Direction;

        public float Rotation;

        public void Clone(ref Transform source)
        {
            this.Position = source.Position;
            this.Scale = source.Scale;
            this.Direction = source.Direction;
            this.Rotation = source.Rotation;
        }

        public static Transform Identity() => new Transform()
        {
            Position = Vector2.Zero,
            Scale = Vector2.One,
            Direction = Vector2.UnitX,
            Rotation = 0f
        };
    }
}
