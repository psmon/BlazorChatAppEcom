using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Components;
using Newtonsoft.Json;
using TiledCS;


namespace BlazorChatApp.Client.ChatLand
{
    public class ChatMap : BaseComponent, IRenderable
    {
        public ChatMap(GameObject owner) : base(owner)
        { 
        }

        public override async ValueTask Update(GameContext game)
        { 

        }

        public ValueTask Render(GameContext game, Canvas2DContext context)
        {
            throw new NotImplementedException();
        }
    }
}
