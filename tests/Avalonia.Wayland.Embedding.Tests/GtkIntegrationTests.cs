using System;
using System.Linq;
using Avalonia.Headless;
using Avalonia.Wayland.Embedding.Hosting;

namespace Avalonia.Wayland.Embedding.Tests;

/// <summary>
/// Integration tests that run a REAL GTK3 client against the in-process subcompositor (over WAYLAND_SOCKET).
/// GTK is a process-wide singleton (it can't re-init), so the scenarios run sequentially in one test to keep the
/// shared GTK display + connection deterministic. These exercise the actual toolkit glue path that
/// <see cref="WaylandTestClient"/> only simulates with the NWayland client bindings.
/// </summary>
public class GtkIntegrationTests
{
    private const string LinuxOnly = "GTK embedding tests require Linux + GTK3";

    [AvaloniaFact]
    public void Real_gtk_client_maps_renders_and_embeds()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        Assert.SkipUnless(GtkTestHarness.EnsureInitialized(), "GTK3 could not be initialized");

        // ── Scenario 2: a plain GTK toplevel auto-hosts into an Avalonia window, rendering real pixels ──
        var win = new Gtk.Window("gtk-render");
        win.SetDefaultSize(240, 160);
        var box = new Gtk.Box(Gtk.Orientation.Vertical, 8) { Margin = 12 };
        box.Add(new Gtk.Label("hello from GTK"));
        box.Add(new Gtk.Button(new Gtk.Label("a GTK button")));
        win.Add(box);
        win.ShowAll();
        // GTK draws its first buffer (→ map → auto-window) on its own GLib frame clock, which can take a varying
        // number of pump rounds under load — wait on the post-condition rather than a fixed PumpAll count.
        Assert.True(GtkTestHarness.PumpUntil(() => WaylandHosting.AutoWindows.Any(w => w.Title == "gtk-render")),
            "GTK never auto-hosted its toplevel");

        var auto = WaylandHosting.AutoWindows.Single(w => w.Title == "gtk-render");
        var autoHost = Assert.IsType<WaylandSubcompositorControlHost>(auto.Content);
        Assert.NotEmpty(autoHost.Bitmaps.Values); // GTK rendered real pixels into our compositor

        win.Destroy();
        Assert.True(GtkTestHarness.PumpUntil(() => WaylandHosting.AutoWindows.All(w => w.Title != "gtk-render")),
            "the auto-window survived the GTK toplevel being destroyed"); // closed → auto-window gone

        // ── Scenario 1: embed a GTK toplevel into a pre-created Avalonia host via AttachClientSurface ──
        var host = new WaylandSubcompositorControlHost();
        var window = new Avalonia.Controls.Window { Width = 320, Height = 220, Content = host };
        window.Show();
        WaylandTestHarness.Pump();

        var gtk = new Gtk.Window("gtk-embed");
        gtk.SetDefaultSize(140, 100);
        gtk.Add(new Gtk.Label("embedded GTK"));

        // Use the SAME glue path the sample uses (GtkClientGlue.Embed): it shows the window so gdk creates the
        // xdg_toplevel role, then sends embed_toplevel over GTK's OWN connection before gdk draws+commits (maps).
        // The glue's private event queue keeps gdk's default-queue events from dispatching during the embed, so it
        // can't map first. This also regression-covers the "must ShowAll before embed" ordering the glue encodes.
        GtkClientGlue.Embed(host, gtk);
        // gdk now draws → commits → maps EMBEDDED into the host; wait on the embed map completing.
        Assert.True(GtkTestHarness.PumpUntil(() => host.IsEmbeddedSurfaceMapped),
            "the GTK toplevel did not embed into the host");
        Assert.NotEmpty(host.Bitmaps.Values);  // real GTK pixels rendered into the embedded host
        Assert.DoesNotContain(WaylandHosting.AutoWindows, w => w.Title == "gtk-embed"); // embedded, not auto-hosted

        gtk.Destroy();
        window.Close();
        GtkTestHarness.PumpAll();
    }
}
