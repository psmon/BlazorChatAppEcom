using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Components
{
    public interface IComponent
    {
        ValueTask Update(GameContext game);

        GameObject Owner { get; }
    }
}