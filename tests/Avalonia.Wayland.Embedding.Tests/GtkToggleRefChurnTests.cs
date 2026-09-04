using System;
using System.Linq;
using Avalonia.Wayland.Embedding.Hosting;

namespace Avalonia.Wayland.Embedding.Tests;

/// <summary>
/// Churns the auto-host path 100× (create a real GTK toplevel → wait for it to draw + auto-host → destroy → wait
/// gone). Two regressions live or die here:
/// <list type="bullet">
/// <item>The frame-clock pump timing: GDK throttles a window's first buffer-producing paint to a ~16ms GLib timeout,
/// and our manual pump must give it that wall-clock (see <see cref="GtkTestHarness"/>). A single window can pass by
/// luck; the 2nd+ window fails deterministically if the pump regresses, so the loop is the guard.</item>
/// <item>A GtkSharp toggle-reference bug on destroy (see the <c>Destroyed</c> workaround below).</item>
/// </list>
/// </summary>
public class GtkToggleRefChurnTests
{
    private const string LinuxOnly = "GTK embedding tests require Linux + GTK3";

    [AvaloniaFact]
    public void Auto_host_churn_100x()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        Assert.SkipUnless(GtkTestHarness.EnsureInitialized(), "GTK3 could not be initialized");

        for (var i = 0; i < 100; i++)
        {
            var title = $"gtk-churn-{i}";
            var win = new Gtk.Window(title);
            // Workaround for a GtkSharp bug, NOT correct usage: gtk_widget_destroy emits "destroy", whose GtkSharp
            // marshaller re-wraps the sender (GLib.Object.GetObject misses the dict-evicted wrapper) and adds a SECOND
            // g_object_add_toggle_ref → GLib aborts in toggle_refs_notify (n_toggle_refs must be 1). Disposing the
            // wrapper in the Destroyed handler removes the duplicate toggle ref in time.
            // https://github.com/GtkSharp/GtkSharp/issues/248#issuecomment-941704980
            win.Destroyed += (s, _) => ((Gtk.Window)s).Dispose();
            win.SetDefaultSize(240, 160);
            var box = new Gtk.Box(Gtk.Orientation.Vertical, 8) { Margin = 12 };
            box.Add(new Gtk.Label("hello from GTK"));
            box.Add(new Gtk.Button(new Gtk.Label("a GTK button")));
            win.Add(box);
            win.ShowAll();

            Assert.True(GtkTestHarness.PumpUntil(() => WaylandHosting.AutoWindows.Any(w => w.Title == title)),
                $"iteration {i}: GTK never auto-hosted its toplevel");

            win.Destroy();
            Assert.True(GtkTestHarness.PumpUntil(() => WaylandHosting.AutoWindows.All(w => w.Title != title)),
                $"iteration {i}: the auto-window survived the GTK toplevel being destroyed");
        }
    }
}
