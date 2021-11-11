using System;
using System.Threading.Tasks;

using Flurl;
using Flurl.Http;
using ServiceModel.Models.Search;

namespace BlazorChatApp.Server.Service
{
    public partial class SearchService
    {
        protected string BaseUrl;

        protected string BaseUrlRecommend;

        protected string BaseUrlSummary;

        public SearchService()
        {
            //https://search-api.showa.kr/help/index.html
            BaseUrl = "https://search-api.showa.kr/api/Search/search";

            BaseUrlRecommend = "https://search-api.showa.kr/api/v1/Recommend/mainView";

            BaseUrlSummary = "https://search-api.showa.kr/api/Search/search-dsl";

        }

        public async Task<SearchResult> Search(string keyword, string mallId, string categoryCode, string categoryCode2 , string sortOption)
        {
            Console.WriteLine($"try search {keyword}");

            Url searchQuery;

            if (keyword !=null && (keyword.Contains("GROUPBEST") || keyword.Contains("SHOWABESTGROUP")))
            {
                searchQuery = BaseUrlRecommend
                    .SetQueryParam("matchGroup", keyword);
            }
            else
            {
                searchQuery = BaseUrl
                    .SetQueryParam("paging.page", 0)
                    .SetQueryParam("paging.limit", 100);

                if (keyword != null)
                {
                    searchQuery = searchQuery.SetQueryParam("keyword", keyword);
                }

                if (mallId != null)
                {
                    searchQuery = searchQuery.SetQueryParam("filters.mallId", mallId);
                }

                if (categoryCode != null)
                {
                    searchQuery = searchQuery.SetQueryParam("filters.categoryCode1", categoryCode);
                }

                if (categoryCode2 != null)
                {
                    searchQuery = searchQuery.SetQueryParam("filters.categoryCode2", categoryCode2);
                }

                if(sortOption != null)
                {
                    searchQuery = searchQuery.SetQueryParam("sort.option", sortOption);
                }

            }

            SearchResult result = await searchQuery.GetJsonAsync<SearchResult>();

            return result;
        }

    }
}
