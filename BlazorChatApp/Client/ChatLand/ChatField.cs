using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Core;
using BlazorChatApp.Client.Core.Components;
using BlazorChatApp.Shared;

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

        public Dictionary<string,ElementReference> resource {get;set; }

        public readonly List<StoreLink> storeLinks= new List<StoreLink>();        
        public double Width { get; private set; } = 800;
        public double Height { get; private set; } = 600;

        public ChatField(GameObject owner) : base(owner)
        {
            storeLinks.Add(new StoreLink()
            { 
                Name = "쇼아몰",
                PosX = 145,PosY=128,
                Link="AGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "루나몰",
                PosX = 105,PosY=258,
                Link="BGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "루나톡센터",
                PosX = 75,PosY=378,
                Link="CGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "스마트+몰",
                PosX = 150,PosY=378,
                Link="DGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "루나A몰",
                PosX = 330,PosY=378,
                Link="EGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "루나B몰",
                PosX = 330,PosY=478,
                Link="FGROUPBEST"
            });

        }

        public StoreLink CollisionCheck(double x, double y)
        {
            int inDistance = 30;
            Console.WriteLine($"x==>{x} y==>{y}");
            foreach(var storeLink in storeLinks)
            {
                var distance = Math.Sqrt((Math.Pow(storeLink.PosX-50 - x, 2) + Math.Pow(storeLink.PosY-80 - y, 2)));
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



        public async override ValueTask Update(GameContext game)
        {
            //await StepForward();
        }

        public async ValueTask Render(GameContext game, Canvas2DContext context)
        {
            double fps = 1.0 / (DateTime.Now - LastRender).TotalSeconds;
            LastRender = DateTime.Now;

            await context.SetFillStyleAsync("#003366");
            await context.FillRectAsync(0, 0, Width, Height);
            await context.DrawImageAsync(resource["img-back"], 0, 0, Width, Height);

            await context.SetFontAsync("26px Segoe UI");
            await context.SetFillStyleAsync("#FFFFFF");


            await context.FillTextAsync("Meta EShop", 10, 30);
            await context.SetFontAsync("16px consolas");
            await context.FillTextAsync($"FPS: {fps:0.000}", 10, 50);
            await context.SetStrokeStyleAsync("#FFFFFF");

            await context.SetFontAsync("16px 바탕체");
            await context.SetFillStyleAsync("Red");
            await context.SetStrokeStyleAsync("#DF0101");

            foreach(var store in storeLinks)
            {
                await context.FillTextAsync($"{store.Name}", store.PosX, store.PosY);
            }

            await context.SetFillStyleAsync("White");
            await context.SetStrokeStyleAsync("#FFFFFF");

        }
    }
}
