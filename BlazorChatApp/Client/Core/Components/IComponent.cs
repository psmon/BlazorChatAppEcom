using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Components
{
    public interface IComponent
    {
        ValueTask Update(SceneContext game);

        SceneObject Owner { get; }
    }
}