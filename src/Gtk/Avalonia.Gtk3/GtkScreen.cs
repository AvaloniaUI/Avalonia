using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    public class GtkScreen : Screen
    {
        private readonly int _screenId;
        
        public GtkScreen(Rect bounds, Rect workingArea, bool primary, int screenId) : base(bounds, workingArea, primary)
        {
            this._screenId = screenId;
        }

        public override int GetHashCode()
        {
            return _screenId;
        }

        public override bool Equals(object obj)
        {
            return (obj is GtkScreen screen) ? this._screenId == screen._screenId : base.Equals(obj);
        }
    }
}