namespace Avalonia.Platform
{
    public class Screen
    {
        public double PixelDensity { get; }

        public PixelRect Bounds { get; }

        public PixelRect WorkingArea { get; }

        public bool Primary { get; }
        
        public Screen(double pixelDensity, PixelRect bounds, PixelRect workingArea, bool primary)
        {
            this.PixelDensity = pixelDensity;
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.Primary = primary;
        } 
    }
}
