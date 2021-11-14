﻿using System;
using System.Collections.Generic;
using System.Linq;

using BlazorChatApp.Shared;

namespace BlazorChatApp.Client.ChatLand
{
    public class StoreLink
    {
        public string Name{get;set; }
        public string Link{get;set; }
        public double PosX{get;set; }
        public double PosY{get;set; }
    }

    public class Field
    {
        public readonly List<StoreLink> storeLinks= new List<StoreLink>();
        public readonly List<Ball> Balls = new List<Ball>();
        public double Width { get; private set; } = 800;
        public double Height { get; private set; } = 600;

        public Field()
        {
            storeLinks.Add(new StoreLink()
            { 
                Name = "A몰",
                PosX = 145,PosY=128,
                Link="AGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "B몰",
                PosX = 105,PosY=258,
                Link="BGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "C몰",
                PosX = 75,PosY=378,
                Link="CGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "D몰",
                PosX = 150,PosY=378,
                Link="DGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "E몰",
                PosX = 330,PosY=378,
                Link="EGROUPBEST"
            });

            storeLinks.Add(new StoreLink()
            { 
                Name = "F몰",
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

        public void StepForward()
        {            
            foreach (Ball ball in Balls)
                ball.StepForward();
        }

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

        public void AddUser(string id, string name, double posx,double posy)
        {
            double minSpeed = 1.2;
            double maxSpeed = .5;
            double radius = 10;
            Random rand = new Random();

            var user = Balls.FindAll(x => x.Id == id);
            if(user.Count==0)
            {
                Balls.Add(
                    new Ball(id,name,
                        x: posx,
                        y: posy,
                        xVel: minSpeed,
                        yVel: minSpeed,
                        radius: radius,
                        color: RandomColor(rand)
                    )
                );
            }
        }

        public void RemoveUser(string id)
        {
            Balls.RemoveAll(b => b.Id == id);
        }

        public void UpdateUserPos(UpdateUserPos updateUserPos)
        {
            var ball = Balls.FirstOrDefault(f=>f.Id.Equals(updateUserPos.Id));
            if(ball!=null)
            {
                ball.MoveForward(updateUserPos.AbsPosX,updateUserPos.AbsPosY);
            }
        }

        public void ChatMessage(ChatMessage chatMessage)
        {
            var ball = Balls.FirstOrDefault(f=>f.Id.Equals(chatMessage.From.Id));
            if(ball!=null)
            {
                ball.AddChatMessage(chatMessage);
            }
        }

    }
}