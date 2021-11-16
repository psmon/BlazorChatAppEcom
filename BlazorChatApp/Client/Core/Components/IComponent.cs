using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Components
{
    public interface IComponent
    {
        ValueTask Update(GameContext game);

        public GameObject Owner { get; }

        public string Id { get;set; }
    }
}
