namespace Avalonia.Platform
{
    public class Screen
    {
        public double PixelDenisty { get; }

        public PixelRect Bounds { get; }

        public PixelRect WorkingArea { get; }

        public bool Primary { get; }
        
        public Screen(double pixelDensity, PixelRect bounds, PixelRect workingArea, bool primary)
        {
            this.PixelDenisty = pixelDensity;
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.Primary = primary;
        } 
    }
}
