namespace Avalonia.Triangle.Models
{
    public class DotObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public DotObject(Dot dot)
        {
            X = (int)dot.X;
            Y =(int) dot.Y;
        }
    }
}