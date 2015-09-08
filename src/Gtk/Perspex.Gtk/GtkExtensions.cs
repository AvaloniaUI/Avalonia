





namespace Perspex.Gtk
{
    using Gtk = global::Gtk;

    public static class GtkExtensions
    {
        public static Rect ToPerspex(this Gdk.Rectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }
}
