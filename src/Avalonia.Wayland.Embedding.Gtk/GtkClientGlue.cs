using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Wayland.Embedding.Hosting;
using Gdk;
// GtkSharp's root namespaces collide with this project's `...Embedding.Gtk` namespace, so bind them explicitly at
// compilation-unit scope (where `Gtk`/`Gdk` resolve to the global GtkSharp namespaces, not this one).
using GtkApplication = global::Gtk.Application;
using GtkWindow = global::Gtk.Window;
using GdkGlobal = global::Gdk.Global;
using GdkDisplay = global::Gdk.Display;

// NB: the namespace is the embedding root, NOT `Avalonia.Wayland.Embedding.Gtk` (the assembly name). A `.Gtk`
// namespace nested under `Avalonia.Wayland.Embedding` would shadow GtkSharp's global `Gtk` namespace for any
// consumer that itself lives under `Avalonia.Wayland.Embedding.*` (e.g. the test project), breaking their plain
// `Gtk.Window` references. Sitting in the root namespace alongside WaylandEmbeddingSubcompositor sidesteps that.
namespace Avalonia.Wayland.Embedding;

/// <summary>
/// Example-only glue showing how a GtkSharp/GTK3 app drives the in-process subcompositor. GTK connects to us over
/// <c>WAYLAND_SOCKET</c> (the client fd from <see cref="WaylandEmbeddingSubcompositor.CreateConnection"/>), so no
/// display is needed. This is NOT shipped on NuGet — a real consumer writes equivalent glue for its own toolkit
/// against the public <c>Avalonia.Wayland.Embedding</c> API. UI/GTK-thread-affined (GTK is a process singleton).
/// </summary>
/// <remarks>
/// If your app also uses Avalonia's HarfBuzz text shaping, apply the GtkSharp/HarfBuzzSharp dlopen workaround
/// (RTLD_DEEPBIND) BEFORE calling <see cref="TryInitialize"/> — GtkSharp loads system libharfbuzz with RTLD_GLOBAL,
/// which otherwise corrupts libHarfBuzzSharp's symbols. The sample does this in <c>Main</c>.
/// </remarks>
public static class GtkClientGlue
{
    [DllImport("libc", CharSet = CharSet.Ansi)]
    private static extern int setenv(string name, string value, int overwrite);

    // GTK-specific extraction: the wl_display* of GTK's connection, and a realized GdkWindow's wl_surface*.
    [DllImport("libgdk-3.so.0")]
    private static extern IntPtr gdk_wayland_display_get_wl_display(IntPtr gdkDisplay);

    [DllImport("libgdk-3.so.0")]
    private static extern IntPtr gdk_wayland_window_get_wl_surface(IntPtr gdkWindow);

    // GTK-side xdg-foreign helpers (scenarios 3 & 4): GTK wraps zxdg_importer/exporter behind these.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GdkWaylandWindowExported(IntPtr window, IntPtr handle, IntPtr userData);

    [DllImport("libgdk-3.so.0")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool gdk_wayland_window_export_handle(IntPtr window, GdkWaylandWindowExported callback, IntPtr userData, IntPtr destroyFunc);

    [DllImport("libgdk-3.so.0", CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool gdk_wayland_window_set_transient_for_exported(IntPtr window, string parentHandle);

    // Synchronous resize flush: drive an embedded widget's GdkFrameClock directly (paint/after-paint) so it repaints
    // in-line, WITHOUT a GLib main-loop turn (no Avalonia dispatcher re-entrancy, and a nested toolkit loop can't
    // freeze us). The clock handle comes via the P/Invoke (GtkSharp's managed Gtk.Widget.FrameClock materializes a
    // wrapper the finalizer thread would unref cross-thread); GtkSharp exposes no managed arbitrary-signal emit.
    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_widget_get_frame_clock(IntPtr widget);

    [DllImport("libgobject-2.0.so.0", CharSet = CharSet.Ansi)]
    private static extern void g_signal_emit_by_name(IntPtr instance, string detailedSignal);

    // The wayland object id of a wl_surface proxy — the same id the compositor sees server-side, so the resize flush
    // can match our windows to the hosts that resized purely by id, with no client round-trip.
    [DllImport("libwayland-client.so.0")]
    private static extern uint wl_proxy_get_id(IntPtr proxy);

    // gdk holds the export callback until destroy_func runs; keep a managed ref so it isn't collected meanwhile.
    private static readonly List<GdkWaylandWindowExported> s_exportCallbacks = new();

    private static bool s_initialized;
    private static bool s_failed;
    // The per-wl_display embedder connection (bound + associated at init); answers resize-flush queries with no round-trip.
    private static WaylandEmbedderConnection? s_connection;
    // Frame clocks the resize flush sized this frame; their paint/after-paint is deferred to RepaintEnqueuedWindows
    // (the before-commit hook) so we never paint mid-layout. Keyed by frame-clock pointer (unique per window).
    private static readonly HashSet<IntPtr> s_pendingRepaintClocks = new();

    /// <summary>Initialize GTK once for this process and point it at our compositor (over WAYLAND_SOCKET). Returns
    /// false if GTK is unavailable or init fails. Apply the HarfBuzz dlopen workaround first (see remarks).</summary>
    public static bool TryInitialize()
    {
        if (s_initialized)
            return true;
        if (s_failed)
            return false;
        try
        {
            var (clientFd, _) = WaylandEmbeddingSubcompositor.CreateConnection();
            // libwayland's wl_display_connect(NULL) reads WAYLAND_SOCKET from libc's environ — must use libc setenv,
            // NOT Environment.SetEnvironmentVariable (which doesn't write the C runtime's environ).
            setenv("WAYLAND_SOCKET", clientFd.ToString(), 1);
            GdkGlobal.AllowedBackends = "wayland"; // force the GDK Wayland backend → talks to us, not a real display
            GtkApplication.Init();
            // Patch the GdkFrameClock class vtable now (before any real window exists) so every clock is tracked from
            // birth: a throwaway toplevel gives us a sample clock whose class FrameClockHelper pins + patches.
            using (var probe = new GtkWindow(global::Gtk.WindowType.Toplevel))
            {
                probe.Realize();
                var probeClock = gtk_widget_get_frame_clock(probe.Handle);
                if (probeClock != IntPtr.Zero)
                    FrameClockHelper.Initialize(probeClock);
            }
            // Resize flush (mid-layout): size the GTK windows and enqueue them. Repaint (just before the Avalonia
            // commit, after layout): emit their paint/after-paint. Splitting the two keeps GTK off the layout pass.
            WaylandEmbeddingSubcompositor.AddClientFramePumpCallback(DriveEmbeddedFrames);
            WaylandEmbeddingSubcompositor.AddBeforeCommitCallback(RepaintEnqueuedWindows);
            // Bind the embedder now — GTK has finished init (its own registry/globals are settled) and no render is
            // running. The bind/associate lives on a PRIVATE event queue, so it can't dispatch GDK's default-queue
            // events out from under it.
            EnsureConnection();
            s_initialized = true;
            return true;
        }
        catch
        {
            s_failed = true;
            return false;
        }
    }

    /// <summary>GTK's <c>wl_display*</c> — the connection backing all of GTK's surfaces.</summary>
    public static IntPtr GetWlDisplay() => gdk_wayland_display_get_wl_display(GdkDisplay.Default.Handle);

    /// <summary>The <c>wl_surface*</c> backing a GTK window. Realizes the window so the surface exists; the window
    /// stays UNMAPPED until shown, which is exactly the state scenario-1 embedding wants.</summary>
    public static IntPtr GetWlSurface(GtkWindow window)
    {
        window.Realize();
        return gdk_wayland_window_get_wl_surface(window.Window.Handle);
    }

    /// <summary>Scenario 1: embed a GTK toplevel into a pre-created Avalonia host control. Shows the window first so
    /// gdk creates its <c>xdg_toplevel</c> role (the embed targets that role), then sends embed_toplevel — the
    /// private event queue keeps gdk from drawing+mapping before the embed is processed, so it maps INTO the host.</summary>
    public static void Embed(WaylandSubcompositorControlHost host, GtkWindow window)
    {
        EnsureConnection();
        ShowAndFlush(window); // creates the wl_surface + xdg_toplevel role (role-setup commit, no buffer → unmapped)
        host.AttachClientSurface(s_connection!, GetWlSurface(window));
    }

    // Bind the embedder + associate this connection. Done once at glue init (after GtkApplication.Init has settled
    // GTK's own registry) over a PRIVATE event queue, so the bind/associate round-trips on GTK's wl_display can't
    // dispatch GDK's default-queue events. Idempotent + guarded on a live display, so the embed paths can still call
    // it defensively.
    private static void EnsureConnection()
    {
        if (s_connection is not null || GdkDisplay.Default is null)
            return;
        s_connection = WaylandEmbeddingSubcompositor.CreateEmbedderConnection(GetWlDisplay());
    }

    /// <summary>Scenario 5: place Avalonia content inside a GTK window (tags the window's own surface). Shows the
    /// window first so its <c>xdg_toplevel</c> role exists; the mark is recorded and resolves when the window maps.</summary>
    public static void PlaceContentInside(WaylandSubcompositorAvaloniaContentHost content, GtkWindow window)
    {
        EnsureConnection();
        ShowAndFlush(window);
        content.AttachToClientSurface(s_connection!, GetWlSurface(window));
    }

    /// <summary>
    /// Client-frame pump (registered with the subcompositor): synchronously drive the GdkFrameClock of every GTK
    /// toplevel that maps to a host which RESIZED this flush, so it relayouts + repaints at the new size in-line and
    /// on this call stack — no GLib main-loop turn, so it can't re-enter Avalonia's dispatcher or be wedged by a
    /// nested toolkit loop. We enumerate ALL GTK toplevels (not just ones embedded through this glue — a window can
    /// be created natively) and ask the subcompositor which of their surfaces resized; only those are driven.
    /// </summary>
    private static void DriveEmbeddedFrames()
    {
        if (s_connection is null || !WaylandEmbeddingSubcompositor.HasPendingResizes || GdkDisplay.Default is null)
            return;

        var windows = GtkWindow.ListToplevels();
        if (windows.Length == 0)
            return;

        var surfaceIds = new uint[windows.Length];
        for (var i = 0; i < windows.Length; i++)
        {
            var surface = windows[i].Window is { } gdkWindow
                ? gdk_wayland_window_get_wl_surface(gdkWindow.Handle)
                : IntPtr.Zero;
            surfaceIds[i] = surface == IntPtr.Zero ? 0 : wl_proxy_get_id(surface);
        }

        var resized = s_connection.QueryResizedSurfaces(surfaceIds);

        // GTK can't read the fresh xdg_toplevel.configure mid-flush (its wayland GSource holds the read lock while our
        // higher-priority source runs), so for the in-flush frame we size each resized window's widget tree by hand
        // (Resize + an immediate SizeAllocate at the new bounds) and enqueue it. The compositor still sends a matching
        // configure, which GTK acks (same size, a no-op) once it resumes — so its own state stays coherent. Painting is
        // deferred to RepaintEnqueuedWindows (the before-commit hook) so the toolkit never paints during this layout pass.
        for (var i = 0; i < windows.Length; i++)
        {
            if (resized[i] is not { } size)
                continue;
            if (windows[i].Window is not { } gdkWindow)
                continue;
            var clock = gtk_widget_get_frame_clock(windows[i].Handle);
            if (clock == IntPtr.Zero)
                continue;

            windows[i].Window.Resize(size.Width, size.Height);
            windows[i].SizeAllocate(new Rectangle(0, 0, size.Width, size.Height));
            s_pendingRepaintClocks.Add(clock);
        }
    }

    // Registered to run right before MediaContext commits the Avalonia compositors (after layout): paint the windows
    // the resize flush sized + enqueued. Painting here, not in the flush, keeps the toolkit off the layout pass; the
    // fresh buffer is flushed and processed so it lands in THIS commit.
    private static void RepaintEnqueuedWindows()
    {
        if (s_pendingRepaintClocks.Count == 0)
            return;
        foreach (var clock in s_pendingRepaintClocks)
        {
            g_signal_emit_by_name(clock, "paint");
            // after-paint freezes the clock to await a wl_surface.frame callback we deliver out-of-band; the scope
            // undoes only the redundant freeze so the clock stays paintable for the next forced frame.
            using (FrameClockHelper.EnterManualAfterPaint(clock))
                g_signal_emit_by_name(clock, "after-paint");
        }
        s_pendingRepaintClocks.Clear();

        // Flush GTK's freshly-committed buffers (write only — NEVER a wl_display roundtrip from a render: it deadlocks
        // on the toolkit's read lock), then a WORKER roundtrip so our compositor processes them and hands the new
        // bitmap to the host before the commit below captures it.
        GdkDisplay.Default?.Flush();
        WaylandEmbeddingSubcompositor.Roundtrip();
    }

    // Show the window (gdk creates the xdg_toplevel role + queues a role-setup commit) and flush those requests to
    // the compositor, so the toplevel role is live there before we embed/mark over the toolkit's connection.
    private static void ShowAndFlush(GtkWindow window)
    {
        window.ShowAll();
        GdkDisplay.Default?.Flush();
    }

    /// <summary>Scenario 3: parent a GTK dialog to an Avalonia Window exported via
    /// <see cref="WaylandEmbeddingSubcompositor.ExportForeignXdgToplevel"/>. Call BEFORE the dialog is shown (gdk
    /// applies the transient-for on map). Returns false if gdk couldn't set it (e.g. importer unavailable).</summary>
    public static bool ParentToExportedWindow(GtkWindow dialog, string avaloniaWindowHandle)
    {
        dialog.Realize();
        return gdk_wayland_window_set_transient_for_exported(dialog.Window.Handle, avaloniaWindowHandle);
    }

    /// <summary>Scenario 4: export a GTK toplevel's handle so Avalonia can resolve it via
    /// <see cref="WaylandEmbeddingSubcompositor.ImportForeignXdgToplevel"/>. The handle arrives asynchronously
    /// (after the next GTK/compositor round-trip) on <paramref name="onHandle"/>. The window must already be SHOWN
    /// (gdk exports the live xdg_toplevel). Returns false if gdk couldn't start the export.</summary>
    public static bool ExportWindowHandle(GtkWindow window, Action<string> onHandle)
    {
        window.Realize();
        GdkWaylandWindowExported callback = (_, handlePtr, _) =>
        {
            var handle = Marshal.PtrToStringUTF8(handlePtr);
            if (!string.IsNullOrEmpty(handle))
                onHandle(handle!);
        };
        s_exportCallbacks.Add(callback); // keep alive until gdk is done with it
        return gdk_wayland_window_export_handle(window.Window.Handle, callback, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>Process GTK's pending events and flush its requests onto the socket, then let the compositor consume
    /// them. For manual-step drivers (tests, smoke runs); a real app runs GTK on the shared GLib main loop instead.</summary>
    public static void PumpGtk()
    {
        while (GtkApplication.EventsPending())
            GtkApplication.RunIteration(false);
        GdkDisplay.Default?.Flush();
        WaylandEmbeddingSubcompositor.Roundtrip();
    }
}
