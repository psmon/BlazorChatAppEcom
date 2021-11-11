using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceModel.Models.Search
{
    public class SearchRecommandGroup
    {
        public bool isMatch { get; set; }

        public string matchType { get; set; }

        public string matchGroup { get; set; }
    }
}
