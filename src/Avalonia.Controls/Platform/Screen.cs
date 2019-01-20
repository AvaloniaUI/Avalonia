namespace Avalonia.Platform
{
    public class Screen
    {
        public PixelRect Bounds { get; }

        public PixelRect WorkingArea { get; }

        public bool Primary { get; }
        
        public Screen(PixelRect bounds, PixelRect workingArea, bool primary)
        {
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.Primary = primary;
        } 
    }
}
