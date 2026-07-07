using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding;

namespace Avalonia.Wayland.Embedding.Tests;

/// <summary>
/// Test-side driver for the real-GTK integration tests. The actual GTK glue (init, wl_display/wl_surface
/// extraction, scenario helpers) lives in <see cref="GtkClientGlue"/> (the example glue project); this just adds
/// the test-only bits: the once-per-process dlopen workaround, a UI-thread init guard, and a combined GTK +
/// compositor + Avalonia manual pump (a real app runs GTK on the shared GLib main loop instead).
/// </summary>
internal static class GtkTestHarness
{
    [ModuleInitializer]
    internal static void ApplyDlopenWorkaround()
    {
        // GtkSharp dlopen's system libharfbuzz with RTLD_GLOBAL, which would corrupt libHarfBuzzSharp's symbols
        // and segfault. Apply the RTLD_DEEPBIND workaround at module load — before HarfBuzzSharp is used for text
        // shaping or GtkSharp loads its natives. (GtkClientGlue.TryInitialize assumes this was already applied.)
        try { XEmbedSample.HarfbuzzWorkaround.Apply(); }
        catch { /* best-effort; GTK tests skip themselves if init then fails */ }
    }

    /// <summary>Initialize GTK once on the Avalonia UI thread (delegates to the example glue). Returns false (and is
    /// skippable) if GTK is unavailable or init fails.</summary>
    public static bool EnsureInitialized()
    {
        Dispatcher.UIThread.VerifyAccess();
        return GtkClientGlue.TryInitialize();
    }

    /// <summary>GTK's wl_display* — pass to host.AttachClientSurface / contentHost.AttachToClientSurface.</summary>
    public static System.IntPtr GetWlDisplay() => GtkClientGlue.GetWlDisplay();

    /// <summary>The toolkit wl_surface* backing a realized GTK window — pass to host.AttachClientSurface.</summary>
    public static System.IntPtr GetWlSurface(Gtk.Window window) => GtkClientGlue.GetWlSurface(window);

    /// <summary>Pump GTK, the compositor, anand Avalonia together so a GTK toplevel completes its map handshake
    /// (configure → ack → draw → commit) and the resulting events reach the Avalonia UI.</summary>
    public static void PumpAll(int rounds = 40)
    {
        for (var i = 0; i < rounds; i++)
            PumpOnce();
    }

    /// <summary>Pump until <paramref name="condition"/> holds or <paramref name="maxRounds"/> is reached; returns
    /// whether it held. GTK draws its first buffer on its own GLib frame clock, so the number of rounds needed to
    /// complete a map handshake varies with machine load — a fixed <see cref="PumpAll"/> count is racy for that.
    /// Wait on the actual post-condition instead.</summary>
    public static bool PumpUntil(System.Func<bool> condition, int maxRounds = 400)
    {
        for (var i = 0; i < maxRounds && !condition(); i++)
            PumpOnce();
        return condition();
    }

    private static void PumpOnce()
    {
        GtkClientGlue.PumpGtk();    // GTK iteration + flush + compositor roundtrip (applies compositor→UI events)
        WaylandTestHarness.Pump(1); // Avalonia layout/render + frame-callback return
        // GDK throttles a window's buffer-producing paint to a ~16ms GLib timeout (vsync avoidance). Our pump uses
        // non-blocking gtk_main_iteration_do(FALSE), and a not-yet-due timeout isn't "pending", so a fast (warm-GTK)
        // pump loop can spin without ever dispatching it — the window then never draws its first buffer / maps. Give
        // GDK that wall-clock (a real app's main loop / gtk_main does this naturally). See GtkIntegrationTests.
        System.Threading.Thread.Sleep(1);
    }
}
