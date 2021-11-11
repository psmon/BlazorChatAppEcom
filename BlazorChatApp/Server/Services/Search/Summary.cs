using System.Collections.Generic;

namespace ServiceModel.Models.Search
{
    public class FilterValue
    {
        public string fieldName { get; set; }

        public string filterValue { get; set; }

        public string filterText { get; set; }

        public double? value { get; set; }
    }

    public class ElkPage
    {
        public int total { get; set; }

        public int size { get; set; }

        public int page { get; set; }

        public int pageSize { get; set; }

        public bool hasNext { get; set; }
    }

    public class Summary
    {
        public ElkPage paging { get; set; } = new ElkPage();

        public TagInfo tags { get; set; }        

        public List<FilterValue> mall { get; set; } = new List<FilterValue>();

        public List<FilterValue> category1 { get; set; } = new List<FilterValue>();

        public List<FilterValue> category2 { get; set; } = new List<FilterValue>();

        public List<FilterValue> category3 { get; set; } = new List<FilterValue>();

    }
}
