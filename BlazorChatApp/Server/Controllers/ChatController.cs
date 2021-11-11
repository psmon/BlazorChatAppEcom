using System.Threading.Tasks;

using BlazorChatApp.Server.Service;

using Microsoft.AspNetCore.Mvc;

using ServiceModel.Models.Search;

namespace BlazorChatApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController
    {
        SearchService searchService;
        public ChatController(SearchService serchService)
        {
            searchService = serchService;
        }

        [HttpGet]
        public async Task<SearchResult> Get(string keyword)
        {
            return await searchService.Search(keyword,null,null,null,null).ConfigureAwait(false);
        }
    }
}
