using System;
using System.Collections.Generic;
using System.Linq;

using BlazorChatApp.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlazorChatApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShopItemController : ControllerBase
    {
        private static readonly ShopItem[] Summaries = new ShopItem[]
        {
            new ShopItem { nameKr="프루 양털 누빔 패딩 (3color)", mallName="크러시제이",
                imageUrl1 = "https://crushj.com/web/product/medium/202111/4e4dc386337b07425c4614d1569fbd31.gif" },
            new ShopItem { nameKr="믹스꽈 크롭꽈배기니트", mallName="큐니걸스",
                imageUrl1 = "https://qng.co.kr/web/product/medium/202111/5416a8f87665c3e2f47e5afff35c8393.gif" },
            new ShopItem { nameKr="데이즈 누빔 셔츠 자켓", mallName="메이드제이",
                imageUrl1 = "https://madejay.com/web/product/medium/202111/faccfc1e2ca92ea058e1a83666fefbcd.gif" },
            new ShopItem { nameKr="오피셜 솔리드 슬림핏 하프 울 코트", mallName="오드",
                imageUrl1 = "http://www.ode.co.kr/shopimages/odeshop/018004000307.jpg?1637194729" },
            new ShopItem { nameKr="소프트하프퍼자켓", mallName="시크릿라벨",
                imageUrl1 = "http://www.secretlabel.co.kr/shopimages/label55/046003001704.jpg?1637134024" },
        };

        private readonly ILogger<ShopItemController> _logger;

        public ShopItemController(ILogger<ShopItemController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<ShopItem> Get()
        {
            var rng = new Random();
            return Enumerable.Range(0, 3).Select(index => new ShopItem
            {
                nameKr = Summaries[index].nameKr,
                urlPc= Summaries[index].urlPc,
                mallName= Summaries[index].mallName,
                imageUrl1 = Summaries[index].imageUrl1
            })
            .ToArray();
            }
    }
}
