using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Components;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client.ChatLand
{
    public class StoreLink
    {
        public string Name{get;set; }
        public string Link{get;set; }
        public double PosX{get;set; }
        public double PosY{get;set; }
    }

    public class ChatField : BaseComponent, IRenderable
    {
        public DateTime LastRender { get;set; }

        private readonly List<Snowflake> snowflakes = new List<Snowflake>();
        private readonly Random rand = new Random();

        public Dictionary<string,ElementReference> resource {get;set; }

        public readonly List<StoreLink> storeLinks= new List<StoreLink>();        
        public double Width { get; private set; } = 800;
        public double Height { get; private set; } = 600;

        private static readonly string[] FlowerPetalColors = new[]
{
    "#FFB6C1", // 연분홍
    "#FFC0CB", // 핑크
    "#FFD1DC", // 연한 핑크
    "#FFFACD", // 레몬
    "#E6E6FA", // 연보라
    "#D8BFD8", // 연보라2
    "#B0E0E6", // 연하늘
    "#F5DEB3", // 밀색
    "#FFF0F5", // 라벤더 블러쉬
    "#F08080", // 연한 빨강
    "#F4A460", // 샌드
    "#98FB98", // 연녹색
    "#FF69B4", // 핫핑크
    "#FFA07A", // 연한 주황
    "#FFD700"  // 노랑 (해바라기 느낌)
};

        public ChatField(SceneObject owner) : base(owner)
        {
            storeLinks.Add(new StoreLink()
            { 
                Name = "",
                PosX = 0,PosY=180,
                Link="AGROUPBEST"
            });

            
            // Initialize snowflakes
            for (int i = 0; i < 15; i++)
            {
                var PetalColor = FlowerPetalColors[rand.Next(FlowerPetalColors.Length)];

                snowflakes.Add(new Snowflake(
                    rand.NextDouble() * Width,
                    rand.NextDouble() * Height,
                    rand.NextDouble() * 2 + 1,
                    rand.NextDouble() * 3 + 1,
                    "#FFFFFF",
                    PetalColor
                ));
            }
        }

        public StoreLink CollisionCheck(double x, double y)
        {
            int inDistance = 30;
            Console.WriteLine($"x==>{x} y==>{y}");
            foreach(var storeLink in storeLinks)
            {
                var distance = Math.Sqrt((Math.Pow(storeLink.PosX - x, 2) + Math.Pow(storeLink.PosY - y, 2)));
                if (distance < inDistance)
                {
                    Console.WriteLine($"Link===>{storeLink.Link} Dist{distance}");
                    return storeLink;
                }
            }
            return null;
        }

        public void Resize(double width, double height) =>
            (Width, Height) = (width, height);


        public void SyncPos()
        {            
            //foreach (Ball ball in Balls)
                //ball.SyncPos();
        }

        private double RandomVelocity(Random rand, double min, double max)
        {
            double v = min + (max - min) * rand.NextDouble();
            if (rand.NextDouble() > .5)
                v *= -1;
            return v;
        }


        private string RandomColor(Random rand) => 
            string.Format("#{0:X6}", rand.Next(0xFFFFFF));



        public async override ValueTask Update(SceneContext game)
        {
            foreach (var snowflake in snowflakes)
            {
                snowflake.Update(Height);
            }
        }

        public async ValueTask Render(SceneContext game, Canvas2DContext context)
        {
            double fps = 1.0 / (DateTime.Now - LastRender).TotalSeconds;
            LastRender = DateTime.Now;

            await context.SetFillStyleAsync("#003366");
            await context.FillRectAsync(0, 0, Width, Height);
            await context.DrawImageAsync(resource["img-back"], 0, 0, Width, Height);

            await context.SetFontAsync("26px Segoe UI");
            await context.SetFillStyleAsync("#FFFFFF");


            await context.FillTextAsync("BlumnAI-헤이데어팀", 10, 30);
            await context.SetFontAsync("16px consolas");
            await context.FillTextAsync($"FPS: {fps:0.000}", 10, 50);
            await context.SetStrokeStyleAsync("#FFFFFF");

            await context.SetFontAsync("16px 바탕체");
            await context.SetFillStyleAsync("White");
            await context.SetStrokeStyleAsync("#DF0101");

            foreach(var store in storeLinks)
            {
                await context.FillTextAsync($"{store.Name}", store.PosX, store.PosY);
            }



            // Render flower-shaped snowflakes (타원 대신 원으로 꽃잎)
            foreach (var snowflake in snowflakes)
            {
                int petalCount = 5;
                double petalRadius = snowflake.Radius * 1.1;
                double petalDistance = snowflake.Radius * 1.2;
                double centerX = snowflake.X;
                double centerY = snowflake.Y;


                for (int i = 0; i < petalCount; i++)
                {
                    double angle = 2 * Math.PI * i / petalCount;
                    double petalCenterX = centerX + Math.Cos(angle) * petalDistance;
                    double petalCenterY = centerY + Math.Sin(angle) * petalDistance;

                    await context.BeginPathAsync();
                    await context.ArcAsync(petalCenterX, petalCenterY, petalRadius, 0, 2 * Math.PI, false);
                    await context.SetFillStyleAsync(snowflake.PetalColor);
                    await context.FillAsync();
                }

                // 꽃술(중앙) 그리기
                await context.BeginPathAsync();
                await context.ArcAsync(centerX, centerY, snowflake.Radius * 0.8, 0, 2 * Math.PI, false);
                await context.SetFillStyleAsync("#FFD700"); // 노란색
                await context.FillAsync();
            }


            await context.SetFillStyleAsync("White");
            await context.SetStrokeStyleAsync("#FFFFFF");

        }
    }
}
