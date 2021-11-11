using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ServiceModel.Models.Search
{
    public class SortFilter
    {
        [JsonPropertyName("sort.option")]
        public string option { get; set; }
    }

    public class SortItem
    {
        public string title { get; set; }

        public SortFilter filter { get; set; }
    }

}
