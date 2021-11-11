using System;

namespace ServiceModel.Models.Search
{
    public class SearchProduct
    {
        public int no { get; set; }

        public string docId { get; set; }

        public string docState { get; set; }

        public string goodsNo { get; set; }

        public string productNo { get; set; }

        public string nameKr { get; set; }

        public string nameEn { get; set; }

        public string mallName { get; set; }

        public string categoryCode { get; set; }

        public string category1 { get; set; }

        public string categoryCode1 { get; set; }

        public string category2 { get; set; }

        public string categoryCode2 { get; set; }

        public string category3 { get; set; }

        public string categoryCode3 { get; set; }

        public string category4 { get; set; }

        public string categoryCode4 { get; set; }

        public string categoryNameFromMall { get; set; }

        public string sex { get; set; }

        public string age { get; set; }

        public int price { get; set; }

        public int discountPrice { get; set; }

        public int discountRate { get; set; }

        public int mallScore { get; set; }

        public string mallIcon { get; set; }

        public string mallId { get; set; }

        public int productScore { get; set; }

        public int weekScore { get; set; }

        public int monthScore { get; set; }

        public int newarrivalScore { get; set; }

        public int latestScore { get; set; }

        public int mdOrder { get; set; }

        public int viewCnt { get; set; }

        public int saleCnt { get; set; }

        public int likeCnt { get; set; }
      
        public string terms { get; set; }

        public string tags { get; set; }

        public string showaTag1 { get; set; }

        public string showaTag2 { get; set; }

        public string colors { get; set; }

        public bool isLike { get; set; }

        public bool isBest { get; set; }

        public bool isNew { get; set; }

        public string fullTerms { get; set; }

        public string urlPc { get; set; }

        public string urlMobile { get; set; }

        public string imageUrl1 { get; set; }

        public string imageUrl2 { get; set; }

        public bool isAvailable { get; set; } = true;

        public ImageInfo imageInfo { get; set; }

        public DateTime updateTime { get; set; }

        public DateTime createTime { get; set; }
    }
}
