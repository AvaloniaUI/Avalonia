using Avalonia;
using Avalonia.Wayland.Embedding;
using XEmbedSample; // HarfbuzzWorkaround (shared via <Compile Include>)

namespace WaylandEmbedSample;

// Reverse of XEmbedSample: Avalonia is the host, GTK3 is the embedded Wayland client. The demo window walks
// through all five embedding scenarios plus an "infinite canvas" of live GTK widgets. GTK and Avalonia share one
// GLib main loop (X11PlatformOptions.UseGLibMainLoop), so events flow between them without any manual pumping.
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Must run before GtkSharp loads its natives / HarfBuzz shapes text (see HarfbuzzWorkaround).
        HarfbuzzWorkaround.Apply();

        // Bring up the Avalonia X11 host on the GLib main loop GTK will run.
        AppBuilder.Configure<App>()
            .UseHarfBuzz()
            .UseSkia()
            .With(new X11PlatformOptions
            {
                UseGLibMainLoop = true,
                ExternalGLibMainLoopExceptionLogger = e => Console.Error.WriteLine(e),
            })
            .UseWayland()
            .With(new WaylandPlatformOptions()
            {
                WlDisplayName = "wayland-1",
                UseGLibMainLoop = true,
                ExternalGLibMainLoopExceptionLogger = e => Console.Error.WriteLine(e),
            })
            //.UseX11()
            .SetupWithoutStarting();

        if (args.Contains("--trace"))
            WaylandEmbeddingSubcompositor.ProtocolTrace += s => Console.Error.WriteLine("[wl] " + s);

        // Initialize GTK and point it at our in-process subcompositor (over WAYLAND_SOCKET). The compositor thread
        // is already running from its static ctor; this mints the client connection GTK connects back on.
        if (!GtkClientGlue.TryInitialize())
        {
            Console.Error.WriteLine("GTK could not be initialized.");
            return;
        }

        var main = new DemoMainWindow();
        main.Closed += (_, _) => Gtk.Application.Quit(); // closing the demo window stops the shared loop
        main.Show();

        // The GLib loop drives both GTK and Avalonia (UseGLibMainLoop).
        Gtk.Application.Run();
    }
}
