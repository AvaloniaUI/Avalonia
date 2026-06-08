using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Wayland.Clipboard;
using Avalonia.Wayland.Screens;
using Avalonia.Wayland.Server;
using Avalonia.X11;

namespace Avalonia.Wayland;

class WaylandPlatform
{

    public static void Initialize(WaylandPlatformOptions options)
    {
        var connection = WaylandWorker.Probe(options);
        if (connection == null)
            throw new AvaloniaWaylandException("Unable to connect to Wayland display");
        // TODO: wait for globals to be ready on the wayland thread and perform sanity checks too

        var inputDispatchQueue = new AutomaticRawEventGrouperDispatchQueue();
        Dispatcher.InitializeUIThreadDispatcher(new ManagedDispatcherImpl(null));
        
        var worker = new WaylandWorker(inputDispatchQueue);

        var screens = new SnapshotScreensImpl();
        var screensProxy = new WaylandOutputsSinkProxy(screens, WaylandMarshallers.UIThread);

        worker.Start(options, connection, screensProxy);



        var clipboardImpl = new WaylandClipboardImpl(worker);
        var clipboard = new Input.Platform.Clipboard(clipboardImpl);

        AvaloniaLocator.CurrentMutable

            .Bind<IWindowingPlatform>().ToConstant(new WaylandTopLevelFactory(worker.Client))
            .Bind<IRenderLoop>().ToConstant(worker.RenderLoop)
            .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Control))
            .Bind<KeyGestureFormatInfo>()
            .ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>() { }, meta: "Super"))
            .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
            .Bind<ICursorFactory>().ToConstant(new WaylandCursorFactory())
            .Bind<IClipboardImpl>().ToConstant(clipboardImpl)
            .Bind<IClipboard>().ToConstant(clipboard)
            .Bind<IPlatformDragSource>().ToConstant(new WaylandDragSource())
            .Bind<IPlatformSettings>().ToConstant(new DefaultPlatformSettings())
            .Bind<IMountedVolumeInfoProvider>().ToConstant(new LinuxMountedVolumeInfoProvider())
            .Bind<IPlatformIconLoader>().ToConstant(new X11IconLoader())
            .Bind<IScreenImpl>().ToConstant(screens);
    }
}

public class WaylandPlatformOptions
{
    public string? WlDisplayName { get; set; }

    /// <summary>
    /// An already-opened file descriptor for the Wayland display socket.
    /// When set, <see cref="WlDisplayName"/> is ignored and
    /// <c>wl_display_connect_to_fd</c> is used instead of <c>wl_display_connect</c>.
    /// Reconnects are automatically disabled in this mode because the fd
    /// is consumed by <c>libwayland</c> and cannot be reused.
    /// </summary>
    public int? DisplayFd { get; set; }

    public bool EnableTracing { get; set; }
    public bool? EnableReconnects { get; set; }

    /// <summary>
    /// Suppresses server-side decoration negotiation
    /// (<c>zxdg_decoration_manager_v1</c>): toplevels behave as if the
    /// compositor never advertised SSD support. Used primarily for
    /// testing the CSD path on compositors that would otherwise enforce
    /// server-side decorations (KWin, etc.). Equivalent in intent to
    /// <c>X11PlatformOptions.ForceDrawnDecorations</c>.
    /// </summary>
    [Experimental("AVALONIA_WAYLAND_FORCE_CSD"
#if NET10_0_OR_GREATER
        , Message = "Experimental, used mostly for testing"
#endif
        )]
    public bool ForceDrawnDecorations { get; set; }

#pragma warning disable AVALONIA_WAYLAND_FORCE_CSD
    internal bool ForceDrawnDecorationsInternal => ForceDrawnDecorations;
#pragma warning restore AVALONIA_WAYLAND_FORCE_CSD

    public IList<GlVersion> GlProfiles { get; set; } = new List<GlVersion>
    {
        new GlVersion(GlProfileType.OpenGL, 4, 0),
        new GlVersion(GlProfileType.OpenGL, 3, 2),
        new GlVersion(GlProfileType.OpenGL, 3, 0),
        new GlVersion(GlProfileType.OpenGLES, 3, 2),
        new GlVersion(GlProfileType.OpenGLES, 3, 0),
        new GlVersion(GlProfileType.OpenGLES, 2, 0)
    };
    public bool? UseDmabufSwapchain { get; set; }
}

public static class AvaloniaWaylandPlatformExtensions
{
    public static AppBuilder UseWayland(this AppBuilder builder)
    {
        builder
            .UseStandardRuntimePlatformSubsystem()
            .UseWindowingSubsystem(() =>
                WaylandPlatform.Initialize(AvaloniaLocator.Current.GetService<WaylandPlatformOptions>() ??
                                                 new WaylandPlatformOptions()));
        return builder;
    }

}