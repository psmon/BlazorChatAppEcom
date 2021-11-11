using System;
using System.Collections.Generic;

namespace ServiceModel.Models.Search
{
    public class SearchResult
    {
        public List<SearchProduct> list { get; set; }

        public Summary summary { get; set; }

        public FilterHelper filterHelper { get; set; }

        public AutoKeywordResult autoKeywordResult { get; set; }

    }
}
