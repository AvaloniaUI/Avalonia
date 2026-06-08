using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Wayland.Screens;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient.Rendering;
using NWayland;
using NWayland.Interop;
using NWayland.Protocols.FractionalScaleV1;
using NWayland.Protocols.LinuxDmabufV1;
using NWayland.Protocols.TextInputUnstableV3;
using NWayland.Protocols.Viewporter;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgForeignUnstableV2;
using NWayland.Protocols.XdgOutputUnstableV1;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Server.Transient;

class WaylandGlobals
{
    public WlRegistry Registry { get; }
    private Dictionary<string, (uint name, uint version)> _knownGlobals = new();
    public WaylandOutputsTracker Outputs { get; }
    public event Action<uint>? GlobalRemoved;
    public WlShm WlShm { get; }
    public List<WlShm.FormatEnum> ShmFormats { get; } = new();
    public WlCompositor WlCompositor { get; }
    public XdgWmBase XdgWmBase { get; }
    public WlDataDeviceManager? DataDeviceManager { get; }
    public ZwpLinuxDmabufV1? LinuxDmabuf { get; }
    public WpFractionalScaleManagerV1? FractionalScaleManager { get; }
    public WpViewporter? Viewporter { get; }
    public ZwpTextInputManagerV3? TextInputManagerV3 { get; }
    public ZxdgExporterV2? XdgExporter { get; }
    /// <summary>
    /// Bound when the compositor advertises <c>zxdg_output_manager_v1</c>.
    /// When present, screens use <c>zxdg_output_v1.logical_*</c> as the
    /// authoritative logical bounds; otherwise we derive logical bounds
    /// as <c>wl_output.mode / wl_output.scale</c>.
    /// </summary>
    public ZxdgOutputManagerV1? XdgOutputManager { get; }
    /// <summary>
    /// Bound only when the compositor advertises
    /// <c>zxdg_decoration_manager_v1</c> AND the platform option
    /// <see cref="WaylandPlatformOptions.ForceDrawnDecorations"/> is
    /// false. <c>null</c> means we must use client-side decorations for
    /// every toplevel (no SSD negotiation will be attempted).
    /// </summary>
    public ZxdgDecorationManagerV1? XdgDecorationManager { get; }

    public bool HasFractionalScaling => FractionalScaleManager != null && Viewporter != null;
    
    // Tracks all seat global names so we can route global_remove to InputDispatcher
    private readonly HashSet<uint> _seatGlobalNames = new();
    
    class RegistryListener(WaylandGlobals p) : NWayland.Protocols.Wayland.WlRegistry.Listener
    {
        protected override void Global(WlRegistry eventSender, uint name, string @interface, uint version)
        {
            p._knownGlobals[@interface] = (name, version);
            if (@interface == WlOutput.ProxyType.Interface.Name)
            {
                p.Outputs.AddGlobal(p, name, version);
            }
            else if (@interface == WlSeat.ProxyType.Interface.Name)
            {
                p.OnSeatGlobal(name, version);
            }
        }

        protected override void GlobalRemove(WlRegistry eventSender, uint name)
        {
            if (p._seatGlobalNames.Remove(name))
                p.InputDispatcher.OnSeatRemoved(name);
            p.GlobalRemoved?.Invoke(name);
        }
    }

    private const uint SeatMinVersion = 5;

    private void OnSeatGlobal(uint globalName, uint version)
    {
        var bindVersion = Math.Min(version, 9);
        if (bindVersion < SeatMinVersion)
            return;
        _seatGlobalNames.Add(globalName);
        InputDispatcher.OnSeatAdded(globalName, Registry, bindVersion);
    }

    T? Bind<T>(uint minVersion, uint maxVersion, IWlEventsListener? listener) where T : WlProxy, IWlProxyTypeDescriptorProvider
    {
        var descriptor = T.ProxyType;
        if (descriptor.Interface.Version < maxVersion)
            throw new AvaloniaWaylandException(
                $"{descriptor.Interface.Name} v{maxVersion} is not supported by current bindings");
        
        if (!_knownGlobals.TryGetValue(descriptor.Interface.Name, out var global))
            return null;
        if (global.version < minVersion)
            return null;
        return Registry.Bind<T>(global.name, Math.Min(global.version, maxVersion), listener);
    }

    T BindRequired<T>(uint minVersion, uint maxVersion,
        IWlEventsListener? listener) where T : WlProxy, IWlProxyTypeDescriptorProvider
    {
        var rv = Bind<T>(minVersion, maxVersion, listener);
        if (rv == null)
            throw new AvaloniaWaylandException(
                $"Required wayland global {T.ProxyType.Interface.Name} (>={minVersion}) not found");
        return rv;
    }

    class ShmListener(WaylandGlobals globals) : WlShm.Listener
    {
        protected override void Format(WlShm eventSender, WlShm.FormatEnum format)
        {
            globals.ShmFormats.Add(format);
            base.Format(eventSender, format);
        }
    }

    class XdgWmBaseListener : XdgWmBase.Listener
    {
        protected override void Ping(XdgWmBase eventSender, uint serial)
        {
            eventSender.Pong(serial);
        }
    }
    
    public WaylandGlobals(WaylandConnection connection, WaylandWorker worker, WaylandPlatformOptions platformOptions,
        WaylandOutputsSinkProxy? outputsSink)
    {
        Connection = connection;
        Worker = worker;
        InputDispatcher = new WaylandInputDispatcher(this);
        Outputs = new WaylandOutputsTracker(outputsSink);
        Registry = connection.Display.GetRegistry(new RegistryListener(this), connection.Queue);
        // Use a roundtrip to ensure we get all existing globals before our own event loop runs
        connection.Queue.Roundtrip();
        WlShm = BindRequired<WlShm>(1, 1, new ShmListener(this));
        WlCompositor = BindRequired<WlCompositor>(4, 6, null);
        XdgWmBase = BindRequired<XdgWmBase>(3, 4, new XdgWmBaseListener());
        CursorManager = new WaylandCursorManager(connection.Display, WlShm, WlCompositor);
        DataDeviceManager = Bind<WlDataDeviceManager>(3, 3, null);
        LinuxDmabuf = Bind<ZwpLinuxDmabufV1>(4, 4, null);
        FractionalScaleManager = Bind<WpFractionalScaleManagerV1>(1, 1, null);
        Viewporter = Bind<WpViewporter>(1, 1, null);
        TextInputManagerV3 = Bind<ZwpTextInputManagerV3>(1, 1, null);
        XdgExporter = Bind<ZxdgExporterV2>(1, 1, null);
        // Require v3 of zxdg_output_manager_v1 so wl_output.done acts as
        // the unified terminator (xdg_output.done is deprecated since
        // v3). Simplifies init-batch gating — same pragmatism as the
        // wl_output v2 minimum.
        XdgOutputManager = Bind<ZxdgOutputManagerV1>(3, 3, null);
        if (XdgOutputManager != null)
            Outputs.AttachXdgOutputManager(XdgOutputManager);
        // ForceDrawnDecorations is a test/debug option: skip binding the
        // SSD manager so toplevels behave as if the compositor never
        // advertised support, exercising the CSD code path.
        XdgDecorationManager = platformOptions.ForceDrawnDecorationsInternal
            ? null
            : Bind<ZxdgDecorationManagerV1>(1, 1, null);
        
        // Seats may have been announced before the data-device manager / text-input
        // manager were bound — InputDispatcher backfills now and constructs the
        // text-input-v3 facade if the manager is available.
        // Seats are bound reactively via RegistryListener and managed by InputDispatcher
        InputDispatcher.OnInitialGlobalsBound();

        // Collect events from bound globals
        connection.Queue.Roundtrip();

        // GPU swapchain selection. Default is WSI (wl_egl_window +
        // eglSwapBuffers) — required for NVIDIA and most resilient to driver
        // quirks. Opt in to the dmabuf path (we own the allocator) by setting
        // UseDmabufSwapchain = true.
        var useDmabuf = platformOptions.UseDmabufSwapchain ?? false;
        WaylandPlatformGraphics.IWaylandGraphics? gpu = null;
        if (useDmabuf && LinuxDmabuf != null)
            gpu = WaylandEglDmaBufPlatformGraphics.TryCreate(connection, this, platformOptions.GlProfiles);
        else
            gpu = WaylandEglWsiPlatformGraphics.TryCreate(connection, platformOptions.GlProfiles);

        worker.PlatformGraphics.Initialize(gpu);

        // TODO: sanity checks
    }

    public WaylandConnection Connection { get; }
    public WaylandWorker Worker { get; }
    public WaylandInputDispatcher InputDispatcher { get; }
    public WaylandCursorManager CursorManager { get; }

    public void Dispose()
    {
        InputDispatcher.Dispose();
        Worker.PlatformGraphics.Reset();
    }
}