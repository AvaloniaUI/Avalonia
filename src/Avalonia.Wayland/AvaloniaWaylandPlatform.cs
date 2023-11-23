using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Wayland;
using NWayland.Protocols.FractionalScaleV1;
using NWayland.Protocols.Plasma.Blur;
using NWayland.Protocols.PointerGesturesUnstableV1;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Viewporter;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgActivationV1;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland
{
    internal class AvaloniaWaylandPlatform : IWindowingPlatform, IDisposable, XdgWmBase.IEvents
    {
        public AvaloniaWaylandPlatform(WaylandPlatformOptions options)
        {
            Options = options;
            WlDisplay = WlDisplay.Connect();
            var registry = WlDisplay.GetRegistry();
            WlRegistryHandler = new WlRegistryHandler(registry);
            WlDisplay.Roundtrip();

            WlCompositor = WlRegistryHandler.BindRequiredInterface(WlCompositor.BindFactory, WlCompositor.InterfaceName, WlCompositor.InterfaceVersion);
            WlSeat = WlRegistryHandler.BindRequiredInterface(WlSeat.BindFactory, WlSeat.InterfaceName, WlSeat.InterfaceVersion);
            WlShm = WlRegistryHandler.BindRequiredInterface(WlShm.BindFactory, WlShm.InterfaceName, WlShm.InterfaceVersion);
            WlDataDeviceManager = WlRegistryHandler.BindRequiredInterface(WlDataDeviceManager.BindFactory, WlDataDeviceManager.InterfaceName, WlDataDeviceManager.InterfaceVersion);
            XdgWmBase = WlRegistryHandler.BindRequiredInterface(XdgWmBase.BindFactory, XdgWmBase.InterfaceName, XdgWmBase.InterfaceVersion);
            XdgActivation = WlRegistryHandler.BindRequiredInterface(XdgActivationV1.BindFactory, XdgActivationV1.InterfaceName, XdgActivationV1.InterfaceVersion);
            WpViewporter = WlRegistryHandler.Bind(WpViewporter.BindFactory, WpViewporter.InterfaceName, WpViewporter.InterfaceVersion);
            WpFractionalScaleManager = WlRegistryHandler.Bind(WpFractionalScaleManagerV1.BindFactory, WpFractionalScaleManagerV1.InterfaceName, WpFractionalScaleManagerV1.InterfaceVersion);
            ZxdgDecorationManager = WlRegistryHandler.Bind(ZxdgDecorationManagerV1.BindFactory, ZxdgDecorationManagerV1.InterfaceName, ZxdgDecorationManagerV1.InterfaceVersion);
            ZxdgExporter = WlRegistryHandler.Bind(ZxdgExporterV2.BindFactory, ZxdgExporterV2.InterfaceName, ZxdgExporterV2.InterfaceVersion);
            ZwpPointerGestures = WlRegistryHandler.Bind(ZwpPointerGesturesV1.BindFactory, ZwpPointerGesturesV1.InterfaceName, ZwpPointerGesturesV1.InterfaceVersion);
            ZwpTextInputManagerV3 = WlRegistryHandler.Bind(ZwpTextInputManagerV3.BindFactory, ZwpTextInputManagerV3.InterfaceName, ZwpTextInputManagerV3.InterfaceVersion);
            KdeKwinBlurManager = WlRegistryHandler.Bind(OrgKdeKwinBlurManager.BindFactory, OrgKdeKwinBlurManager.InterfaceName, OrgKdeKwinBlurManager.InterfaceVersion);

            XdgWmBase.Events = this;

            WlScreens = new WlScreens(this);
            WlInputDevice = new WlInputDevice(this);
            WlDataHandler = new WlDataHandler(this);
            WlRawEventGrouper = new WlRawEventGrouper();

            AvaloniaLocator.CurrentMutable
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IDispatcherImpl>().ToConstant(new WlPlatformThreading(this))
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Control))
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<ICursorFactory>().ToConstant(new WlCursorFactory(this))
                .Bind<IClipboard>().ToConstant(WlDataHandler)
                .Bind<IPlatformDragSource>().ToConstant(WlDataHandler)
                .Bind<IPlatformSettings>().ToSingleton<DBusPlatformSettings>()
                .Bind<IPlatformIconLoader>().ToConstant(new WlIconLoader())
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new LinuxMountedVolumeInfoProvider());

            WlDisplay.Roundtrip();

            DBusHelper.TryInitialize();

            var graphics = InitializeGraphics(options);
            if (graphics is not null)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(graphics);

            Compositor = new Compositor(graphics);
        }

        internal WaylandPlatformOptions Options { get; }

        internal Compositor Compositor { get; }

        internal WlDisplay WlDisplay { get; }

        internal WlRegistryHandler WlRegistryHandler { get; }

        internal WlCompositor WlCompositor { get; }

        internal WlSeat WlSeat { get; }

        internal WlShm WlShm { get; }

        internal WlDataDeviceManager WlDataDeviceManager { get; }

        internal XdgWmBase XdgWmBase { get; }

        internal XdgActivationV1 XdgActivation { get; }

        internal WpViewporter? WpViewporter { get; }

        internal WpFractionalScaleManagerV1? WpFractionalScaleManager { get; }

        internal ZxdgDecorationManagerV1? ZxdgDecorationManager { get; }

        internal ZxdgExporterV2? ZxdgExporter { get; }

        internal ZwpPointerGesturesV1? ZwpPointerGestures { get; }

        internal ZwpTextInputManagerV3? ZwpTextInputManagerV3 { get; }

        internal OrgKdeKwinBlurManager? KdeKwinBlurManager { get; }

        internal WlScreens WlScreens { get; }

        internal WlDataHandler WlDataHandler { get; }

        internal WlInputDevice WlInputDevice { get; }

        internal WlRawEventGrouper WlRawEventGrouper { get; }

        public IWindowImpl CreateWindow() => new WlToplevel(this);

        public IWindowImpl CreateEmbeddableWindow() => throw new NotSupportedException();

        public ITrayIconImpl? CreateTrayIcon()
        {
            var dbusTrayIcon = new DBusTrayIconImpl();
            if (!dbusTrayIcon.IsActive)
                return null;
            dbusTrayIcon.IconConverterDelegate = static impl => impl is WlIconData wlIconData ? wlIconData.Data : Array.Empty<uint>();
            return dbusTrayIcon;
        }

        public void OnPing(XdgWmBase eventSender, uint serial) => XdgWmBase.Pong(serial);

        public void Dispose()
        {
            KdeKwinBlurManager?.Dispose();
            ZxdgDecorationManager?.Dispose();
            ZxdgExporter?.Dispose();
            ZwpTextInputManagerV3?.Dispose();
            ZwpPointerGestures?.Dispose();
            WlDataDeviceManager.Dispose();
            WlRawEventGrouper.Dispose();
            WlDataHandler.Dispose();
            WlInputDevice.Dispose();
            WlScreens.Dispose();
            WlSeat.Dispose();
            WlShm.Dispose();
            XdgActivation.Dispose();
            XdgWmBase.Dispose();
            WlCompositor.Dispose();
            WlRegistryHandler.Dispose();
            WlDisplay.Dispose();
        }


        private IPlatformGraphics? InitializeGraphics(WaylandPlatformOptions opts)
        {
            if (opts.RenderingMode is null || opts.RenderingMode.Count == 0)
                throw new InvalidOperationException($"{nameof(WaylandPlatformOptions)}.{nameof(WaylandPlatformOptions.RenderingMode)} must not be null or empty.");

            foreach (var renderingMode in opts.RenderingMode)
            {
                switch (renderingMode)
                {
                    case WaylandRenderingMode.Software:
                        return null;
                    case WaylandRenderingMode.Egl:
                        const int EGL_PLATFORM_WAYLAND_KHR = 0x31D8;
                        var platformGraphics = EglPlatformGraphics.TryCreate(() => new EglDisplay(new EglDisplayCreationOptions
                        {
                            Egl = new EglInterface(),
                            PlatformType = EGL_PLATFORM_WAYLAND_KHR,
                            PlatformDisplay = WlDisplay.Handle,
                            SupportsContextSharing = true,
                            SupportsMultipleContexts = true
                        }));

                        if (platformGraphics is not null)
                            return platformGraphics;
                        break;
                }
            }

            throw new InvalidOperationException($"{nameof(WaylandPlatformOptions)}.{nameof(WaylandPlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
        }
    }
}

namespace Avalonia
{
    public static class AvaloniaWaylandPlatformExtensions
    {
        public static AppBuilder UseWayland(this AppBuilder builder) =>
            builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(static () =>
                {
                    var options = AvaloniaLocator.Current.GetService<WaylandPlatformOptions>() ?? new WaylandPlatformOptions();
                    var platform = new AvaloniaWaylandPlatform(options);
                    AvaloniaLocator.CurrentMutable.BindToSelf(platform);
                });
    }

    public class WaylandPlatformOptions
    {
        /// <summary>
        /// Gets or sets Avalonia rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="WaylandRenderingMode.Egl"/>, <see cref="WaylandRenderingMode.Software"/>.
        /// </summary>
        /// <remarks>
        /// If application should work on as wide range of devices as possible, at least add <see cref="WaylandRenderingMode.Software"/> as a fallback value.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<WaylandRenderingMode> RenderingMode { get; set; } = new[]
        {
            WaylandRenderingMode.Egl,
            WaylandRenderingMode.Software
        };

        /// <summary>
        /// The app ID identifies the general class of applications to which the surface belongs. <br/>
        /// The compositor can use this to group multiple surfaces together, or to determine how to launch a new application. <br/>
        /// As a best practice, it is suggested to select app ID's that match the basename of the application's .desktop file. For example, "org.freedesktop.FooViewer" where the .desktop file is "org.freedesktop.FooViewer.desktop".
        /// </summary>
        public string? AppId { get; set; }

        /// <summary>
        /// Determines whether to use IME.
        /// IME would be enabled by default if the current user input language is one of the following: Mandarin, Japanese, Vietnamese or Korean.
        /// </summary>
        /// <remarks>
        /// Input method editor is a component that enables users to generate characters not natively available 
        /// on their input devices by using sequences of characters or mouse operations that are natively available on their input devices.
        /// </remarks>
        public bool? EnableIme { get; set; }
    }
}
