using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core
{
    public class SceneGraph
    {
        public SceneGraph()
        {
            Root = new SceneObject();
        }

        public async ValueTask Update(SceneContext game)
        {
            if (null == Root)
                return;
            await Root.Update(game);
        }
        
        public SceneObject Root { get; }
    }
}