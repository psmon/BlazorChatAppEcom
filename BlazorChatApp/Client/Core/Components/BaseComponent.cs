using System;
using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Components
{
    public abstract class BaseComponent : IComponent
    {
        protected BaseComponent(SceneObject owner)
        {
            this.Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public virtual async ValueTask Update(SceneContext game)
        {
        }

        public SceneObject Owner { get; }
    }
}