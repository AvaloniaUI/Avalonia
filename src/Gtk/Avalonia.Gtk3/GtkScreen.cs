using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    public class GtkScreen : Screen
    {
        private readonly int screenId;
        
        public GtkScreen(Rect bounds, Rect workingArea, bool primary, int screenId) : base(bounds, workingArea, primary)
        {
            this.screenId = screenId;
        }

        public override int GetHashCode()
        {
            return screenId;
        }

        public override bool Equals(object obj)
        {
            return (obj is GtkScreen screen) ? this.screenId == screen.screenId : base.Equals(obj);
        }
    }
}