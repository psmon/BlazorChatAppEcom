using System.Threading.Tasks;

namespace BlazorChatApp.Client.Core.Assets.Loaders
{
    public interface IAssetLoader<TA> where TA : IAsset
    {
        ValueTask<TA> Load(string path);
    }
}