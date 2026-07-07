using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Wayland.Embedding;
using Avalonia.Wayland.Embedding.Hosting;

namespace WaylandEmbedSample;

// The demo's main Avalonia window: a tab per embedding scenario plus the infinite-canvas tab. All GTK objects are
// created/used on this (the main / GLib-loop / Avalonia-UI) thread. GTK types are fully qualified (no `using Gtk;`).
internal sealed class DemoMainWindow : Window
{
    // Keep foreign exports alive while their dialogs are open (disposing revokes the handle).
    private readonly List<WaylandForeignExport> _exports = new();

    public DemoMainWindow()
    {
        Title = "Avalonia.Wayland.Embedding — GTK in Avalonia";
        Width = 900;
        Height = 640;

        var tabs = new TabControl();
        tabs.Items.Add(Tab("1 · Embed", BuildEmbedTab()));
        tabs.Items.Add(Tab("2 · Auto-host", BuildAutoHostTab()));
        tabs.Items.Add(Tab("3 · Export window", BuildExportWindowTab()));
        tabs.Items.Add(Tab("4 · Import toplevel", BuildImportToplevelTab()));
        tabs.Items.Add(Tab("5 · Content in GTK", BuildContentInGtkTab()));
        tabs.Items.Add(Tab("∞ · Infinite canvas", new InfiniteCanvasView()));
        Content = tabs;
    }

    private static TabItem Tab(string header, Control content) => new() { Header = header, Content = content };

    private static TextBlock Caption(string text) =>
        new() { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };

    // ── Scenario 1: a GTK toplevel renders INTO a pre-created Avalonia host control on this tab. ──
    private Control BuildEmbedTab()
    {
        // Frame-perfect: draw the embedded GTK buffer 1:1 (the resize flush keeps it in step with the host), so
        // resizing the window doesn't scale-distort a stale frame.
        var host = new WaylandSubcompositorControlHost { StretchContent = false };
        var frame = new Border
        {
            BorderBrush = Brushes.SlateGray,
            BorderThickness = new Thickness(1),
            Background = Brushes.Black,
            Child = host,
        };
        var embed = new Button { Content = "Embed a GTK window here" };
        embed.Click += (_, _) =>
        {
            var win = SampleGtk.WidgetWindow("embedded", 300, 200);
            GtkClientGlue.Embed(host, win); // shows the window, then embeds its toplevel into `host` before it maps
        };
        return new DockPanel
        {
            Children =
            {
                Stack(DockPanel.SetDock, Dock.Top,
                    Caption("Scenario 1 — a GTK toplevel is adopted by a specific Avalonia control (embed_toplevel). " +
                            "The control below is the embedding target."),
                    embed),
                frame, // fills the rest
            },
        };
    }

    // ── Scenario 2: a plain GTK toplevel auto-hosts into its own Avalonia window (rootless-compositor behavior). ──
    private Control BuildAutoHostTab()
    {
        var show = new Button { Content = "Show a standalone GTK window" };
        show.Click += (_, _) => SampleGtk.WidgetWindow("auto-hosted").ShowAll();
        return Panel(
            Caption("Scenario 2 — a GTK toplevel with no embedding token. Avalonia.Wayland.Embedding manufactures a " +
                    "real Avalonia Window to host it, mirroring title/min-max/close — acting as a rootless compositor."),
            show);
    }

    // ── Scenario 3: export THIS Avalonia window as a foreign handle; a GTK dialog parents itself to it. ──
    private Control BuildExportWindowTab()
    {
        var open = new Button { Content = "Open a GTK dialog parented to this window" };
        open.Click += (_, _) =>
        {
            var export = WaylandEmbeddingSubcompositor.ExportForeignXdgToplevel(this);
            _exports.Add(export);
            var dialog = SampleGtk.WidgetWindow("GTK dialog (child of Avalonia)", 280, 160);
            if (!GtkClientGlue.ParentToExportedWindow(dialog, export.Handle)) // set_transient_for_exported — before show
                Console.Error.WriteLine("Scenario 3: gdk_wayland_window_set_transient_for_exported failed.");
            dialog.ShowAll();
        };
        return Panel(
            Caption("Scenario 3 (xdg-foreign out) — this Avalonia window is published as a foreign handle; the GTK " +
                    "dialog imports it and set_parent_of's itself, so its auto-window is owned by this one."),
            open);
    }

    // ── Scenario 4: a GTK toplevel exports its handle; an Avalonia dialog is parented to its host window. ──
    private Control BuildImportToplevelTab()
    {
        var open = new Button { Content = "Show a GTK window + an Avalonia dialog parented to it" };
        open.Click += (_, _) =>
        {
            var gtk = SampleGtk.WidgetWindow("GTK parent");
            gtk.ShowAll(); // auto-hosts (scenario 2) → a host control exists to resolve
            // Export after the loop has turned once, so the window has mapped and gdk has bound the exporter global.
            GLib.Idle.Add(() =>
            {
                var ok = GtkClientGlue.ExportWindowHandle(gtk, handle =>
                {
                    // Runs on the GLib/UI thread when GTK gets its zxdg_exported_v2.handle.
                    var host = WaylandEmbeddingSubcompositor.ImportForeignXdgToplevel(handle);
                    var owner = host is null ? null : TopLevel.GetTopLevel(host) as Window;
                    var dialog = new Window
                    {
                        Title = "Avalonia dialog (child of GTK)",
                        Width = 300,
                        Height = 160,
                        Content = Panel(Caption("This Avalonia dialog is owned by the GTK window via xdg-foreign.")),
                    };
                    if (owner is not null)
                        dialog.Show(owner);
                    else
                        dialog.Show();
                });
                if (!ok)
                    Console.Error.WriteLine("Scenario 4: gdk_wayland_window_export_handle failed.");
                return false; // one-shot idle
            });
        };
        return Panel(
            Caption("Scenario 4 (xdg-foreign in) — the GTK toplevel exports a handle; Avalonia resolves it to the " +
                    "hosting control, walks up to the owning Window, and parents an Avalonia dialog to it."),
            open);
    }

    // ── Scenario 5: Avalonia content is overlaid INSIDE an embedded GTK window. ──
    private Control BuildContentInGtkTab()
    {
        var open = new Button { Content = "Show a GTK window with Avalonia content inside it" };
        open.Click += (_, _) =>
        {
            var content = new WaylandSubcompositorAvaloniaContentHost
            {
                Content = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x20, 0x60, 0xC0)),
                    Child = new TextBlock
                    {
                        Text = "Avalonia content\ninside a GTK window",
                        Foreground = Brushes.White,
                        Margin = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                    },
                },
            };
            var gtk = SampleGtk.WidgetWindow("GTK host for Avalonia content", 360, 240);
            GtkClientGlue.PlaceContentInside(content, gtk); // shows the window + marks the surface (resolves on map)
            content.UpdateContentRect(new Rect(20, 80, 200, 120)); // glue places + clips the overlay
            // Track the GTK window's size so the overlay rect stays sensible as it resizes.
            gtk.SizeAllocated += (_, args) =>
                content.UpdateContentRect(new Rect(20, 80, System.Math.Max(0, args.Allocation.Width - 40), 120));
        };
        return Panel(
            Caption("Scenario 5 — an Avalonia control is composited on top of the embedded GTK window's pixels at a " +
                    "glue-driven rect (mark_content_surface). Input hit-tests the Avalonia content first."),
            open);
    }

    private static StackPanel Panel(params Control[] children)
    {
        var sp = new StackPanel { Spacing = 8, Margin = new Thickness(12) };
        foreach (var c in children)
            sp.Children.Add(c);
        return sp;
    }

    // A top-docked StackPanel (caption + button) for the embed/content tabs whose host fills the remainder.
    private static StackPanel Stack(System.Action<Control, Dock> setDock, Dock dock, params Control[] children)
    {
        var sp = new StackPanel { Spacing = 8, Margin = new Thickness(12) };
        foreach (var c in children)
            sp.Children.Add(c);
        setDock(sp, dock);
        return sp;
    }
}
