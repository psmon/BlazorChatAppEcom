using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceModel.Models.Search
{
    public enum AUTOKEYWORD_TYPE
    {
        ALL = 0,
        GOODS = 1,
        Relation = 2,
        TAG = 3,
        MALL = 4,
        NONE = 100
    }

    public class AutoKeyword
    {
        public int no { get; set; }

        public int score { get; set; }              //자동완성 우선 노출 가중치

        public int autoType { get; set; }           //자동완성 Type

        public string input { get; set; }           //입력 키워드

        public string title { get; set; }

        public string text { get; set; }

        public string link { get; set; }

        public string imageUrl { get; set; }

        public int num1 { get; set; }

        public int num2 { get; set; }

    }

    public class AutoKeywordResult
    {
        public List<AutoKeyword> relation { get; set; }

        public List<AutoKeyword> mall { get; set; }
    }
}
