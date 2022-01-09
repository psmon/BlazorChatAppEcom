using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Client.Core.Exceptions;

namespace BlazorChatApp.Client.Core
{
    public class SceneObject 
    {
        private static int _lastId = 0;

        private readonly IList<SceneObject> _children;

        public string HashId { get;set; }

        public SceneObject()
        {
            this.Id = ++_lastId;

            _children = new List<SceneObject>();

            this.Components = new ComponentsCollection(this);
        }

        public int Id { get; }

        public ComponentsCollection Components { get; }

        public IEnumerable<SceneObject> Children => _children;
        public SceneObject Parent { get; private set; }

        public void AddChild(SceneObject child)
        {
            if (!this.Equals(child.Parent))
                child.Parent?._children.Remove(child);

            child.Parent = this;
            _children.Add(child);
        }

        public T FindById<T>(string id) where T : class, IComponent
        {
            foreach (var child in _children)
            {
                if(child.HashId == id)
                {
                    return child.Components.Get<T>();                    
                }
            }
            return default(T);
        }

        public void RemoveById(string id)
        {
            foreach (var child in _children)
            {
                if(child.HashId == id)
                {
                    _children.Remove(child);
                    return;
                }
            }
        }

        public async ValueTask Update(SceneContext game)
        {
            foreach (var component in this.Components)
                await component.Update(game);

            foreach (var child in _children)
                await child.Update(game);
        }

        public override int GetHashCode() => this.Id;

        public override bool Equals(object obj) => obj is SceneObject node && this.Id.Equals(node.Id);

        public override string ToString() => $"GameObject {this.Id}";
    }
}
