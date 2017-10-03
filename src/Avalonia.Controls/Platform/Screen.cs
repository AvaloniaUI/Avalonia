namespace Avalonia.Platform
{
    public class Screen
    {
        public Rect Bounds { get; }

        public Rect WorkingArea { get; }

        public bool Primary { get; }
        
        public Screen(Rect bounds, Rect workingArea, bool primary)
        {
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.Primary = primary;
        } 
    }
}