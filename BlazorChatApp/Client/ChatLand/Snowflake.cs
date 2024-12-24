namespace BlazorChatApp.Client.ChatLand
{
    public class Snowflake
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Radius { get; set; }
        public string Color { get; set; }

        public Snowflake(double x, double y, double speed, double radius, string color)
        {
            X = x;
            Y = y;
            Speed = speed;
            Radius = radius;
            Color = color;
        }

        public void Update(double height)
        {
            Y += Speed;
            if (Y > height)
            {
                Y = 0;
            }
        }
    }
}
