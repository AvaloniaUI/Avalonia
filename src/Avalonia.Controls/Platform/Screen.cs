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
        
        public override bool Equals(object obj)
        {
            return obj is Screen screen && screen.Bounds == Bounds && screen.WorkingArea == WorkingArea && screen.Primary == Primary;
        }
    }
}