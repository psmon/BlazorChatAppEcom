using System.Text.Json.Serialization;

namespace ServiceModel.Models.Search
{
    public class Paging
    {
        /// <summary>
        /// 시작페이지,Zero Base
        /// </summary>        
        public int page { get; set; }

        /// <summary>
        /// 한번에볼 페이지 제한
        /// </summary>        
        public int limit { get; set; }
    }

    public class Sort
    {
        /// <summary>
        /// 할인순 : desc,asc
        /// </summary>        
        public string discountRate { get; set; }

        /// <summary>
        /// 몰 인기순 : desc,asc
        /// </summary>        
        public string mallScore { get; set; }

        /// <summary>
        /// 상품가중치 : desc,asc
        /// </summary>        
        public string productScore { get; set; }

        /// <summary>
        /// 가격순 : desc,asc
        /// </summary> 
        public string price { get; set; }

        /// <summary>
        /// 인기순 : desc,asc
        /// </summary> 
        public string viewCnt { get; set; }

        /// <summary>
        /// 판매순 : desc,asc
        /// </summary> 
        public string saleCnt { get; set; }

        /// <summary>
        /// 최신상품순 : desc,asc
        /// </summary>
        public string updateDate { get; set; }

        /// <summary>
        /// 정렬 기획 옵션 : maxview,maxlike,latest,minprice,maxprice,maxdiscountrate,bestweek,bestmonth,newarrivals,showa
        /// </summary>
        public string option { get; set; }

    }

    public class Filters
    {
        /// <summary>
        /// 쇼핑몰명 : 멀티 필터지원
        /// </summary> 
        /// <example>핫핑,ㅇㅇ</example>
        public string mallName { get; set; }

        /// <summary>
        /// 쇼핑몰 ID : 멀티 필터지원
        /// </summary> 
        /// <example>hotping,</example>
        public string mallId { get; set; }

        /// <summary>
        /// 상품NO : 멀티 필터지원
        /// </summary> 
        /// <example>hotping1_27868,hotping1_27880</example>
        public string productNo { get; set; }

        /// <summary>
        /// 최소 할인률 : 이상(0가능)
        /// </summary> 
        public int minSaleRate { get; set; }

        /// <summary>
        /// 최대 할인률 : 이하(0불가)
        /// </summary> 
        public int maxSaleRate { get; set; }

        /// <summary>
        /// 최소가격 : 이상(0가능)
        /// </summary> 
        public int minPrice { get; set; }

        /// <summary>
        /// 최대가격 : 이하(0불가)
        /// </summary> 
        public int maxPrice { get; set; }

        /// <summary>
        /// 카테고리1 : 남성전체,여성전체
        /// </summary> 
        public string category1 { get; set; }

        /// <summary>
        /// 카테고리2 : 상의
        /// </summary> 
        public string category2 { get; set; }

        /// <summary>
        /// 카테고리3 : 티셔츠
        /// </summary> 
        public string category3 { get; set; }

        /// <summary>
        /// 카테고리4 : 긴팔티
        /// </summary> 
        public string category4 { get; set; }

        /// <summary>
        /// 통합 카테고리Code : 45678
        /// 멀티선택가능 : 45678,45672,45671
        /// </summary> 
        public string categoryCode { get; set; }

        /// <summary>
        /// 카테고리Code1 : 45678
        /// 멀티선택가능 : 45678,45672,45671
        /// </summary> 
        public string categoryCode1 { get; set; }

        /// <summary>
        /// 카테고리Code2 : 45678
        /// 멀티선택가능 : 45678,45672,45671
        /// </summary> 
        public string categoryCode2 { get; set; }

        /// <summary>
        /// 카테고리Code3 : 45678
        /// 멀티선택가능 : 45678,45672,45671
        /// </summary> 
        public string categoryCode3 { get; set; }

        /// <summary>
        /// 카테고리Code3 : 45678
        /// 멀티선택가능 : 45678,45672,45671
        /// </summary> 
        public string categoryCode4 { get; set; }

        /// <summary>
        /// 성별 : 남,여
        /// </summary> 
        public string sex { get; set; }

        /// <summary>
        /// 나이대 : 10,20,30,40
        /// </summary> 
        public string age { get; set; }

        /// <summary>
        /// 태깅 : 그런지컷,미니멀,울가디건 등
        /// </summary> 
        public string tag { get; set; }

        /// <summary>
        /// 색상필터 : 네이비,노랑
        /// </summary> 
        public string color { get; set; }

        /// <summary>
        /// 최근 등록상품 : 최근상품및 최신상품 업데이트된 몰조회가능합니다. : 4day or week or month
        /// </summary> 
        /// <example> 4day or week or month</example>
        public string period { get; set; }

        /// <summary>
        /// 기간검색 From
        /// </summary> 
        /// <example>2020-05-01T11:00:00</example>
        public string fromUpdateTime { get; set; }

        /// <summary>
        /// 기간검색 To
        /// </summary> 
        /// <example>2020-07-01T11:00:00</example>
        public string toUpdateTime { get; set; }

    }

    public class TackingUser
    {
        /// <summary>
        /// 플랫폼별 고유 앱식별값
        /// </summary> 
        public string fingerPrint { get; set; }

        public string platform { get; set; }    //IOS,ANDROID,MOWEB,WEB

        public string version { get; set; }     // 플랫폼 기준 자신의 버전
    }

    public class SearchFilter
    {
        public Paging paging { get; set; }

        public Sort sort { get; set; }

        public Filters filters { get; set; }

        /// <summary>
        /// 검색키워드 : 원피스
        /// </summary> 
        public string keyword { get; set; }

        /// <summary>
        /// 제외키워드 : 바지
        /// </summary> 
        public string exceptKeyword { get; set; }

        /// <summary>
        /// 검색Type: 0-스마트 검색(default), 1-어드민전용, 2-추천기반(멀티:,), 3-시즌핫템 VER1 , 4-시즌핫템 VER2 , 100-내부집계처리용도(리밋해제)
        /// </summary> 
        public int searchType { get; set; }

        /// <summary>
        /// 문서상태 : Default-쇼아전시,검색가능 / Showa-쇼아카테고리분류 / NA-어드민설정 비전시 / ShowaNA-미맵핑,미분류
        /// </summary>
        public string docState { get; set; }

        [JsonIgnore]
        public TackingUser tackingUser { get; set; }

    }
}
