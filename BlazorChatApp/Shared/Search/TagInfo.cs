using System.Collections.Generic;

namespace ServiceModel.Models.Search
{
    public class TagInfo
    {
        public List<string> list { get; set; } = new List<string>();

        public int index { get; set; } = 20;
    }
}
