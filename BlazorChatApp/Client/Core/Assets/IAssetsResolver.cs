using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Assets
{
    public interface IAssetsResolver
    {
        ValueTask<TA> Load<TA>(string path) where TA : IAsset;
        TA Get<TA>(string name) where TA : class, IAsset;
    }
}