using System;
using BlazorChatApp.Client.Core.Components;

namespace BlazorChatApp.Client.Core.Exceptions
{
    public class ComponentNotFoundException<TC> : Exception where TC : IComponent
    {
        public ComponentNotFoundException() : base($"{typeof(TC).Name} not found on owner")
        {
        }
    }
}