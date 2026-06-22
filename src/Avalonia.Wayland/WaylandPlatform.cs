using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
        // TODO: wait for globals to be ready on the wayland thread and perform sanity checks too.
        // This is needed for UsePlatformDetect() in Avalonia.Desktop: if we detect a compositor we
        // can't support we should fail here so the caller can fall back to X11. Later we'll also want
        // feature-detection flags (e.g. xdg-toplevel-drag-v1 for docks) so apps can request features
        // that force an X11 fallback when the compositor lacks them.

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
            .Bind<ICursorFactory>().ToConstant(new WaylandCursorFactory(worker.Client))
            .Bind<IClipboardImpl>().ToConstant(clipboardImpl)
            .Bind<IClipboard>().ToConstant(clipboard)
            .Bind<IPlatformDragSource>().ToConstant(new WaylandDragSource())
            .Bind<IPlatformSettings>().ToSingleton<DBusPlatformSettings>()
            .Bind<IMountedVolumeInfoProvider>().ToConstant(new LinuxMountedVolumeInfoProvider())
            .Bind<IPlatformIconLoader>().ToConstant(new X11IconLoader())
            .Bind<IScreenImpl>().ToConstant(screens);
    }
}