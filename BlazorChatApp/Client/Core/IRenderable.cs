using System.Threading.Tasks;
using Blazor.Extensions.Canvas.Canvas2D;

namespace BlazorChatApp.Client.Core
{
    public interface IRenderable
    {
        ValueTask Render(SceneContext game, Canvas2DContext context);
    }
}