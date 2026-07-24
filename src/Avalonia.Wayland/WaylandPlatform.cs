using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
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

    public static void Initialize(WaylandPlatformOptions options) => TryInitialize(options)?.Throw();

    internal static ExceptionDispatchInfo? TryInitialize(WaylandPlatformOptions options)
    {
        // In some cases we aren't allowed to open multiple connections (e. g. WAYLAND_SOCKET env
        // is used), so we can't do separate usability checks and bail before doing any initialization.
        // Instead we do full startup sequence with WaylandWorker on compositor thread and wait for it
        // to get to  WaylandGlobals, which is the point where we can make an informed decision
        // about wl_display being usable.

        var connection = WaylandWorker.Probe(options, out var connectError);
        if (connection == null)
            return connectError?.SourceException is AvaloniaWaylandException
                ? connectError
                : ExceptionDispatchInfo.Capture(
                    new AvaloniaWaylandException("Unable to connect to Wayland display",
                        connectError?.SourceException));

        WaylandWorker worker;
        SnapshotScreensImpl screens;
        TaskCompletionSource<ExceptionDispatchInfo?> initTcs;
        try
        {
            // NOTE: This technically mutates the global state since it touches Dispatcher,
            // which designates the current thread as the UI one in static ctor, but it's generally safe to do
            var inputDispatchQueue = new AutomaticRawEventGrouperDispatchQueue();

            worker = new WaylandWorker(inputDispatchQueue);

            screens = new SnapshotScreensImpl();
            var screensProxy = new WaylandOutputsSinkProxy(screens, WaylandMarshallers.UIThread);

            initTcs = new TaskCompletionSource<ExceptionDispatchInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
            worker.Start(options, connection, screensProxy, initTcs);
        }
        catch (Exception e)
        {
            connection.Dispose();
            return ExceptionDispatchInfo.Capture(e);
        }

        if (initTcs.Task.GetAwaiter().GetResult() is { } initError)
            return initError;

        IDispatcherImpl dispatcherImpl = options.UseGLibMainLoop
            ? new WaylandGlibDispatcher(options.ExternalGLibMainLoopExceptionLogger)
            : new ManagedDispatcherImpl(null);
        Dispatcher.InitializeUIThreadDispatcher(dispatcherImpl);
        
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

        return null;
    }
}