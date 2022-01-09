using System;
using System.Threading.Tasks;

using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Components;


namespace BlazorChatApp.Client.ChatLand
{
    public class ChatMap : BaseComponent, IRenderable
    {
        public ChatMap(SceneObject owner) : base(owner)
        { 
        }

        public override async ValueTask Update(SceneContext game)
        { 

        }

        public ValueTask Render(SceneContext game, Canvas2DContext context)
        {
            throw new NotImplementedException();
        }
    }
}
