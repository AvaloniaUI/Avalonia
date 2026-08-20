namespace WaylandEmbedSample;

// Little GTK3 windows full of real GTK widgets, reused across the demo scenarios. GTK types are fully qualified
// (no `using Gtk;`) so Avalonia's Window/Button/Label aren't shadowed in the rest of the sample.
internal static class SampleGtk
{
    public static Gtk.Window WidgetWindow(string title, int width = 260, int height = 180)
    {
        var win = new Gtk.Window(title);
        win.SetDefaultSize(width, height);
        var box = new Gtk.Box(Gtk.Orientation.Vertical, 8) { Margin = 12 };
        box.Add(new Gtk.Label($"GTK widgets — {title}"));
        box.Add(new Gtk.Entry { PlaceholderText = "a GTK entry" });
        box.Add(new Gtk.Button(new Gtk.Label("a GTK button")));
        box.Add(new Gtk.CheckButton("a GTK checkbox"));
        var scale = new Gtk.Scale(Gtk.Orientation.Horizontal, new Gtk.Adjustment(40, 0, 100, 1, 10, 0));
        box.Add(scale);
        win.Add(box);
        return win;
    }

    // A compact widget window for the infinite-canvas tiles (one per embedded toplevel).
    public static Gtk.Window CanvasTile(int index, int width, int height)
    {
        var win = new Gtk.Window($"tile-{index}");
        win.SetDefaultSize(width, height);
        var box = new Gtk.Box(Gtk.Orientation.Vertical, 6) { Margin = 8 };
        box.Add(new Gtk.Label($"GTK tile #{index}"));
        box.Add(new Gtk.Button(new Gtk.Label("press me")));
        box.Add(new Gtk.Entry { PlaceholderText = "type here" });
        win.Add(box);
        return win;
    }
}
