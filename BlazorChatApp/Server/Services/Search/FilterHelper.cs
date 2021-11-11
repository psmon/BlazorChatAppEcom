using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ServiceModel.Models.Search
{
    public class RangeSaleRateFilter
    {
        [JsonPropertyName("filters.minSaleRate")]
        public int minSaleRate { get; set; }

        [JsonPropertyName("filters.maxSaleRate")]
        public int maxSaleRate { get; set; }
    }

    public class RangePriceFilter
    {
        [JsonPropertyName("filters.minPrice")]
        public int minPrice { get; set; }

        [JsonPropertyName("filters.maxPrice")]
        public int maxPrice { get; set; }
    }

    public class FilterRangeItem
    {
        public string title { get; set; }

        public object filter { get; set; }
    }

    public class FilterHelper
    {   
        public List<SortItem> sort { get; set; }

        public FilterRangeItem price { get; set; } = new FilterRangeItem();

        public List<FilterRangeItem> saleRate { get; set; }
    }
}
