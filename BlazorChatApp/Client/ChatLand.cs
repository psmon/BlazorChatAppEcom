﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;

using BlazorChatApp.Client.Model;

using Microsoft.AspNetCore.Components;

namespace BlazorChatApp.Client
{
    public class ChatLand
    {
        ulong frameCnt = 0;
        public Dictionary<string,ElementReference> resource {get;set; }
        public Field BallField { get;set; }
        public BECanvasComponent _canvas { get;set; }        
        public Canvas2DContext _context { get;set; }
        public DateTime LastRender { get;set; }
        public ChatLand()
        {
        }

        public async ValueTask GameLoop(float timeStamp, int width, int height)
        {
            frameCnt++;

            if(frameCnt > int.MaxValue )
            {
                frameCnt = 0;
            }

            if(frameCnt%(60*5)==0)
            {
                BallField.SyncPos();
            }
            else
            {
                BallField.StepForward();
            }

            double fps = 1.0 / (DateTime.Now - LastRender).TotalSeconds;
            LastRender = DateTime.Now;

            await _context.BeginBatchAsync();
            await _context.ClearRectAsync(0, 0, BallField.Width, BallField.Height);
            await _context.SetFillStyleAsync("#003366");
            await _context.FillRectAsync(0, 0, BallField.Width, BallField.Height);
            await _context.DrawImageAsync(resource["img-back"], 0, 0,BallField.Width,BallField.Height);

            await _context.SetFontAsync("26px Segoe UI");
            await _context.SetFillStyleAsync("#FFFFFF");


            await _context.FillTextAsync("Blazor WebAssembly + HTML Canvas", 10, 30);
            await _context.SetFontAsync("16px consolas");
            await _context.FillTextAsync($"FPS: {fps:0.000}", 10, 50);
            await _context.SetStrokeStyleAsync("#FFFFFF");

            await _context.SetFontAsync("12px 바탕체");
            await _context.SetFillStyleAsync("Red");
            await _context.SetStrokeStyleAsync("#DF0101");

            foreach(var store in BallField.storeLinks)
            {
                await _context.FillTextAsync($"{store.Name}-{store.PosX},{store.PosY}", store.PosX, store.PosY);
            }

            await _context.SetFillStyleAsync("White");
            await _context.SetStrokeStyleAsync("#FFFFFF");
            foreach (var ball in BallField.Balls)
            {
                if (!string.IsNullOrEmpty(ball.ChatMessage))
                {
                    await _context.FillTextAsync($"{ball.Name} - {ball.ChatMessage}", ball.X -10, ball.Y -10);
                }
                else
                {
                    await _context.FillTextAsync($"{ball.Name} - {(int)ball.X},{(int)ball.Y}", ball.X -10, ball.Y -10);
                }


                await _context.DrawImageAsync(resource["img-char1"], ball.X,ball.Y,80,80);
                //await _context.BeginPathAsync();
                //await _context.ArcAsync(ball.X, ball.Y, ball.Radius, 0, 2 * Math.PI, false);
                //await _context.SetFillStyleAsync(ball.Color);
                //await _context.FillAsync();
                //await _context.StrokeAsync();
            }
            await _context.EndBatchAsync();
        }
    }
}
