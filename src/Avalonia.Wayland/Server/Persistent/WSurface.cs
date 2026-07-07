using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Threading;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient;
using Avalonia.Wayland.Server.Transient.Rendering;
using NWayland.Protocols.FractionalScaleV1;
using NWayland.Protocols.Viewporter;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Server.Persistent;

class WSurface : IPersistentWaylandObject, IWSurface, IWaylandFramebufferSurface
{
    protected WaylandWorker Worker { get; }
    protected WaylandConnection? Connection { get; private set; }
    public WaylandGlobals? Globals { get; private set; }
    public WlSurface? WlSurface { get; private set; }
    protected WpFractionalScaleV1? FractionalScale { get; private set; }
    protected WpViewport? Viewport { get; private set; }
    protected int? LastPreferredBufferScale { get; private set; }
    protected double? PreferredFractionalScale { get; private set; }
    protected List<WaylandOutputsTracker.Output> Outputs  { get; } = new();
    private WlCallback? _frameCallback;
    private double _currentScale = 1;
    private const double ScaleEpsilon = 1e-6;
    private readonly List<IDisposable> _activeRenderTargets = new();

    public bool HasFractionalScaling => FractionalScale != null && Viewport != null;
    public WlDisplay? CurrentDisplay => Connection?.Display;
    internal double CurrentScale => _currentScale;

    public WSurface(WaylandWorker worker)
    {
        Worker = worker;
        worker.PostOob(() => worker.RegisterPersistentObject(this));
    }

    /// <summary>
    /// Destroys this surface and unregisters it from the worker.
    /// Called on the Wayland thread when the UI thread disposes the window.
    /// </summary>
    public void Disconnect()
    {
        Worker.UnregisterPersistentObject(this);
    }

    /// <summary>
    /// Whether this surface currently has an active text input client.
    /// Set via message from the UI thread, read on the Wayland thread to gate compose processing.
    /// </summary>
    internal bool HasTextInputClient { get; private set; }

    private WaylandTextInputV3EventsProxy? _textInputSink;

    /// <summary>
    /// Session token last received via <see cref="SetTextInputActive"/>.
    /// Stamped on every worker→UI IME callback so the UI broker can drop
    /// callbacks belonging to a previous client (see
    /// <c>WaylandTextInputMethod._clientEpoch</c>).
    /// </summary>
    internal int TextInputSessionToken { get; private set; }

    private WaylandTextInputV3? TextInputV3 => Globals?.InputDispatcher?.TextInputV3;

    /// <summary>
    /// Cursor currently requested for this surface, or <c>null</c> for the default arrow. Lives on
    /// the worker thread — updated via <see cref="SetCursor"/>, consumed by the
    /// <see cref="WaylandInputDispatcher"/> when refreshing the pointer cursor.
    /// </summary>
    internal WaylandCursor? CurrentCursor { get; private set; }

    public virtual void SetCursor(IWaylandCursor? cursor)
    {
        // The proxy auto-unwraps to the real worker-side object before reaching us.
        CurrentCursor = (WaylandCursor?)cursor;
        Globals?.InputDispatcher?.NotifyCursorChanged(this);
    }

    public virtual void RegisterTextInputSink(WaylandTextInputV3EventsProxy sink)
    {
        _textInputSink = sink;
        TextInputV3?.RegisterSurface(this, sink);
    }

    public virtual void SetTextInputActive(bool hasClient, bool supportsPreedit, bool supportsSurroundingText, int sessionToken)
    {
        HasTextInputClient = hasClient;
        TextInputSessionToken = sessionToken;
        if (_textInputSink is { } sink && TextInputV3 is { } v3)
        {
            // RegisterSurface is idempotent and safe to re-issue here in case
            // the V3 facade only became available after the broker registered.
            v3.RegisterSurface(this, sink);
            v3.AbortComposition(this);
            v3.SetActive(this, hasClient, supportsPreedit, supportsSurroundingText);
        }
    }

    public virtual void AbortTextInputComposition() => TextInputV3?.AbortComposition(this);

    public virtual void SetTextInputCursorRect(Rect rect) => TextInputV3?.SetCursorRect(this, rect);

    public virtual void SetTextInputOptions(TextInputOptions options) => TextInputV3?.SetOptions(this, options);

    public virtual void SetTextInputSurroundingText(string text, int cursorChar, int anchorChar) =>
        TextInputV3?.SetSurroundingText(this, text, cursorChar, anchorChar);

    public virtual void ResetTextInput() => TextInputV3?.Reset(this);

    public virtual void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        Connection = connection;
        Globals = globals;
        WlSurface = Globals.WlCompositor.CreateSurface(new Listener(this));

        if (globals.HasFractionalScaling)
        {
            FractionalScale = globals.FractionalScaleManager!.GetFractionalScale(
                WlSurface, new FractionalScaleListener(this), connection.Queue);
            Viewport = globals.Viewporter!.GetViewport(WlSurface);
        }
    }

    private IPlatformRenderSurface[]? _renderSurfaces;
    private object? _renderSurfacesGraphicsKey;

    /// <summary>
    /// Render surfaces matching the currently active platform graphics
    /// backend. Re-resolved when the backend changes (e.g. after a
    /// disconnect/reconnect re-binds globals and possibly initializes a
    /// different EGL platform). The framebuffer surface is always
    /// included as a software fallback.
    /// </summary>
    public IPlatformRenderSurface[] RenderSurfaces
    {
        get
        {
            var graphics = Worker.PlatformGraphics.CurrentGraphics;
            if (_renderSurfaces != null && ReferenceEquals(_renderSurfacesGraphicsKey, graphics))
                return _renderSurfaces;

            _renderSurfacesGraphicsKey = graphics;
            _renderSurfaces = graphics != null
                ? [graphics.CreateRenderSurface(this), new WaylandFramebuffer(this)]
                : [new WaylandFramebuffer(this)];
            return _renderSurfaces;
        }
    }

    public bool IsFrameCallbackPending => _frameCallback != null;

    public virtual PlatformRenderTargetState State =>
        (Globals != null && WlSurface != null && !IsFrameCallbackPending) ? PlatformRenderTargetState.Ready : default;

    public void RegisterRenderTarget(IDisposable renderTarget) => _activeRenderTargets.Add(renderTarget);
    public void UnregisterRenderTarget(IDisposable renderTarget) => _activeRenderTargets.Remove(renderTarget);

    // Toplevels/popups attach one buffer per frame inside the throttled render loop, which flushes
    // naturally, so no eager roundtrip is needed (it would stall the render loop).
    public bool EnforceBufferCreationRoundtrip => false;

    protected virtual void OnOutputsChanged()
    {
        RecomputeScale();
    }

    /// <summary>
    /// Recomputes the desired scale. Priority: fractional preferred_scale (when fractional path
    /// is active) > preferred_buffer_scale (wl_surface v6+) > max scale of currently-entered outputs.
    /// Called on the Wayland thread.
    /// </summary>
    private void RecomputeScale()
    {
        double newScale;
        if (PreferredFractionalScale is { } fractional)
        {
            newScale = fractional;
        }
        else if (LastPreferredBufferScale is { } preferred)
        {
            newScale = preferred;
        }
        else
        {
            newScale = 1;
            foreach (var o in Outputs)
            {
                if (o.Scale > newScale)
                    newScale = o.Scale;
            }
        }

        if (Math.Abs(newScale - _currentScale) > ScaleEpsilon)
        {
            _currentScale = newScale;
            OnScaleChanged(newScale);
        }
    }

    /// <summary>
    /// Called on the Wayland thread when the computed render scale changes.
    /// </summary>
    protected virtual void OnScaleChanged(double scale)
    {
    }

    /// <summary>
    /// Invoked by the platform surface before attaching a buffer and
    /// calling wl_surface::commit. EGL/Vulkan drivers want to make commits
    /// themselves for some reason, so we have this kind of callback logic.
    ///
    /// Stages double-buffered per-frame state — frame callback,
    /// ack_configure, set_window_geometry, viewport / buffer scale, min/max
    /// constraints — into the next commit so they apply atomically with
    /// the new buffer.
    /// </summary>
    public virtual void OnBeforeNewBufferAttached(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
        if (HasFractionalScaling)
        {
            var lw = Math.Max(1, (int)Math.Round(sceneInfo.LogicalSize.Width));
            var lh = Math.Max(1, (int)Math.Round(sceneInfo.LogicalSize.Height));
            Viewport!.SetDestination(lw, lh);
        }
        else
            WlSurface!.SetBufferScale(Math.Max(1, (int)Math.Ceiling(sceneInfo.Scaling)));

        _frameCallback = WlSurface!.Frame(new FrameListener(this));
    }

    class FrameListener(WSurface p) : WlCallback.Listener
    {
        protected override void Done(WlCallback eventSender, uint callbackData)
        {
            p.Worker.WakeupRenderLoop();
            eventSender.Dispose();
            p._frameCallback = null;
        }
    }

    class FractionalScaleListener(WSurface p) : WpFractionalScaleV1.Listener
    {
        protected override void PreferredScale(WpFractionalScaleV1 eventSender, uint scale)
        {
            // Per protocol: scale is preferred_scale * 120. Reject 0 (would be a compositor
            // bug — value must be > 0 per the protocol) but otherwise accept whatever the
            // compositor sent us, including sub-1.0 values.
            if (scale == 0)
                return;
            p.PreferredFractionalScale = scale / 120.0;
            p.RecomputeScale();
        }
    }

    class Listener(WSurface p) : WlSurface.Listener
    {
        protected override void PreferredBufferScale(WlSurface eventSender, int factor)
        {
            p.LastPreferredBufferScale = factor;
            p.RecomputeScale();
        }

        protected override void Enter(WlSurface eventSender, WlOutput? output)
        {
            var o = p.Globals?.Outputs.Outputs.FirstOrDefault(o => o.WlOutput == output);
            if (o != null)
                p.Outputs.Add(o);
            p.OnOutputsChanged();
        }

        protected override void Leave(WlSurface eventSender, WlOutput? output)
        {
            var o = p.Globals?.Outputs.Outputs.FirstOrDefault(o => o.WlOutput == output);
            if (o != null)
                p.Outputs.Remove(o);
            p.OnOutputsChanged();
        }
    }

    public virtual void OnDisconnected()
    {
        Outputs.Clear();
        LastPreferredBufferScale = null;
        PreferredFractionalScale = null;
        _frameCallback?.Dispose();
        _frameCallback = null;
        // Dispose render targets before destroying the wl_surface so EGL
        // resources (images, DMA-BUF buffers) are torn down while the
        // underlying wl_surface is still alive.
        foreach (var rt in _activeRenderTargets.ToList())
            rt.Dispose();
        // Invalidate cached render surface array so reconnect builds fresh ones.
        _renderSurfaces = null;
        _renderSurfacesGraphicsKey = null;
        // Per fractional-scale-v1 / viewporter protocol: child objects must be destroyed
        // before their parent wl_surface to avoid protocol errors.
        if (Viewport != null)
        {
            Viewport.Destroy();
            Viewport.Dispose();
            Viewport = null;
        }
        if (FractionalScale != null)
        {
            FractionalScale.Destroy();
            FractionalScale.Dispose();
            FractionalScale = null;
        }
        WlSurface?.Destroy();
        WlSurface = null;
        Globals = null;
        Connection = null;
    }
}

class WXdgShellSurface : WSurface, IWXdgShellSurface
{
    public XdgSurface? XdgSurface { get; private set; }
    private uint? _pendingAckSerial;
    private bool _initialConfigureAcknowledged;
    // True once we've attached a buffer + acked the initial configure
    // following the most recent OnConnected — i.e. the surface is now
    // *mapped* in xdg-shell terms. xdg_surface.GetPopup against an
    // unmapped parent is a protocol error, so child popups must wait
    // for this transition before attaching.
    private bool _mapped;

    /// <summary>
    /// Children popups whose parents weren't yet mapped when the popup
    /// went through OnConnected (or when the child itself reconnected
    /// before the parent finished mapping). Drained when this surface
    /// transitions to mapped (in <see cref="OnBeforeNewBufferAttached"/>).
    /// Cleared on disconnect — every child's OnDisconnected runs
    /// independently, so on reconnect they will re-register if still pending.
    /// </summary>
    private readonly List<WXdgPopup> _pendingChildPopups = new();

    /// <summary>True iff this surface is currently mapped (xdg-shell sense).</summary>
    internal bool IsMapped => _mapped;

    internal void RegisterPendingChildPopup(WXdgPopup popup) => _pendingChildPopups.Add(popup);
    internal void UnregisterPendingChildPopup(WXdgPopup popup) => _pendingChildPopups.Remove(popup);

    /// <summary>
    /// The UI-thread event sink associated with this surface.
    /// </summary>
    internal WSurfaceEventSinkProxy EventSink { get; }

    private Thickness? _shadowExtents;

    internal Thickness ShadowExtents => _shadowExtents ?? default;

    /// <summary>
    /// The window-geometry rectangle (left, top, width, height in
    /// surface-local logical pixels, *excluding* shadow margins) that we
    /// last sent to the compositor via <c>xdg_surface.SetWindowGeometry</c>.
    /// This is the authoritative parent geometry seen by the compositor;
    /// child popups read it for buffer→geometry coordinate translation
    /// and anchor-rect clamping. <c>null</c> until the first buffer is
    /// attached on the current connection.
    /// </summary>
    internal (int Left, int Top, int Width, int Height)? LastWindowGeometry { get; private set; }

    public WXdgShellSurface(WaylandWorker worker, WSurfaceEventSinkProxy eventSink) : base(worker)
    {
        EventSink = eventSink;
    }

    public void SetShadowExtents(Thickness extents)
    {
        _shadowExtents = extents;
    }

    protected override void OnScaleChanged(double scale) => EventSink.OnScaleChanged(scale);

    protected override void OnOutputsChanged()
    {
        base.OnOutputsChanged();
        // Snapshot the per-surface entered-outputs identity list and post
        // it to the UI thread so IScreenImpl.ScreenFromTopLevel can map
        // the window onto a Screen. Identity matches WaylandOutputSnapshot.Id.
        var ids = Outputs.Count == 0
            ? (IReadOnlyList<object>)Array.Empty<object>()
            : Outputs.Select(o => o.Id).ToList();
        EventSink.OnSurfaceOutputsChanged(ids);
    }

    public override void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        base.OnConnected(connection, globals);
        WlSurface!.Tags[typeof(WXdgShellSurface)] = this;
        _pendingAckSerial = null;
        _initialConfigureAcknowledged = false;
        _mapped = false;

        XdgSurface = globals.XdgWmBase.GetXdgSurface(WlSurface!, new XdgSurfaceListener(this));

        // NOTE: pending child popups are NOT finalised here — the parent
        // isn't *mapped* yet (no buffer attached, no initial configure
        // acked). xdg_surface.GetPopup against an unmapped parent is a
        // protocol error. Children are drained from OnBeforeNewBufferAttached
        // when the mapped transition completes.
    }

    public override void OnDisconnected()
    {
        Globals?.InputDispatcher?.TextInputV3?.UnregisterSurface(this);
        WlSurface?.Tags.Remove(typeof(WXdgShellSurface));
        XdgSurface?.Destroy();
        XdgSurface = null;
        _pendingAckSerial = null;
        _initialConfigureAcknowledged = false;
        _mapped = false;
        _pendingChildPopups.Clear();
        LastWindowGeometry = null;
        EventSink.OnSurfaceOutputsChanged(Array.Empty<object>());
        base.OnDisconnected();
    }

    /// <summary>
    /// Called when the complete configure batch is sealed by xdg_surface.configure(serial).
    /// Subclasses should call the event sink and then call base.
    /// </summary>
    protected virtual void OnConfigureBatchComplete(uint serial)
    {
        Worker.WakeupRenderLoop();
    }

    /// <summary>
    /// Called from UI thread (via Post) to set the serial that should be acked on next commit.
    /// </summary>
    public void SetPendingAckSerial(uint serial)
    {
        _initialConfigureAcknowledged = true;
        _pendingAckSerial = serial;
    }

    public override PlatformRenderTargetState State =>
        _initialConfigureAcknowledged
            ? base.State
            : PlatformRenderTargetState.NotReadyWillWakeupRenderLoop;

    public override void OnBeforeNewBufferAttached(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
        if (_pendingAckSerial.HasValue)
        {
            XdgSurface!.AckConfigure(_pendingAckSerial.Value);
            _pendingAckSerial = null;
        }

        MaybeEmitWindowGeometry(sceneInfo.Size, sceneInfo.Scaling, sceneInfo.LogicalSize);

        base.OnBeforeNewBufferAttached(sceneInfo);

        // First buffer attach + configure-ack since the most recent
        // (re-)connect → we are now mapped. Drain any popups that were
        // queued while we were unmapped; they can now safely call
        // xdg_surface.GetPopup against us.
        if (!_mapped)
        {
            _mapped = true;
            if (_pendingChildPopups.Count > 0)
            {
                var snapshot = _pendingChildPopups.ToArray();
                _pendingChildPopups.Clear();
                foreach (var p in snapshot)
                    p.TryAttachToParent();
            }
        }
    }

    /// <summary>
    /// Computes and emits the surface's window geometry (the rectangle
    /// inside the buffer that the compositor should treat as the visible
    /// window, excluding decoration-area shadows). Default implementation
    /// applies the cached <see cref="_shadowExtents"/> against the
    /// post-scale logical size and calls <c>xdg_surface.SetWindowGeometry</c>.
    ///
    /// Skipped entirely until the surface receives its first
    /// <see cref="SetShadowExtents"/> call — the compositor then treats
    /// the whole buffer as the visible window (which is what we want for
    /// surfaces that don't draw shadows, e.g. popups). Once extents have
    /// been set we keep emitting geometry even if extents go back to all
    /// zero (e.g. when a window is maximized): the compositor still needs
    /// the explicit "geometry == buffer" call to discard the previous
    /// shadow margins.
    ///
    /// Skipping until first set also dodges the fractional-scale rounding
    /// artefact where 125% scaling can't align the geometry rect to the
    /// pixel grid without clipping content.
    /// </summary>
    protected virtual void MaybeEmitWindowGeometry(PixelSize bufferPixelSize, double bufferScale, Size logicalSize)
    {
        if (_shadowExtents is not { } shadowExtents)
            return;

        // Compute surface-local geometry excluding shadows.
        // Geometry must be >= 1x1 and lie within the buffer's logical size (protocol requirement).
        // On the fractional-scale path the renderer passes the authoritative logical size from
        // the latest configure (via composition's RenderTargetSceneInfo); for the integer-scale
        // path we fall back to ceiling the buffer pixel size by the integer scale.
        int surfaceWidth, surfaceHeight;
        if (HasFractionalScaling && logicalSize.Width > 0 && logicalSize.Height > 0)
        {
            surfaceWidth = Math.Max(1, (int)Math.Round(logicalSize.Width));
            surfaceHeight = Math.Max(1, (int)Math.Round(logicalSize.Height));
        }
        else
        {
            var intScale = Math.Max(1, (int)Math.Ceiling(bufferScale));
            surfaceWidth = (bufferPixelSize.Width + intScale - 1) / intScale;
            surfaceHeight = (bufferPixelSize.Height + intScale - 1) / intScale;
        }

        var left = Math.Clamp((int)shadowExtents.Left, 0, surfaceWidth - 1);
        var top = Math.Clamp((int)shadowExtents.Top, 0, surfaceHeight - 1);
        var right = Math.Clamp(
            (int)Math.Ceiling(surfaceWidth - shadowExtents.Right), left + 1, surfaceWidth);
        var bottom = Math.Clamp(
            (int)Math.Ceiling(surfaceHeight - shadowExtents.Bottom), top + 1, surfaceHeight);

        XdgSurface!.SetWindowGeometry(left, top, right - left, bottom - top);
        LastWindowGeometry = (left, top, right - left, bottom - top);
    }

    internal class XdgSurfaceListener(WXdgShellSurface p) : XdgSurface.Listener
    {
        protected override void Configure(XdgSurface eventSender, uint serial) =>
            p.OnConfigureBatchComplete(serial);
    }
}

class WXdgTopLevel : WXdgShellSurface, IWXdgTopLevel
{
    protected TaskCompletionSource<XdgConfigureBatch> BasicInitCompletedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private XdgToplevel? _xdgTopLevel;
    public Task<XdgConfigureBatch> BasicInitCompleted => BasicInitCompletedTcs.Task;
    private XdgConfigureBatch _pendingBatch = new();

    // Active xdg-foreign exports for this surface (worker thread only).
    // Cleared in OnDisconnected — the proxies belong to the dying connection.
    private readonly List<XdgToplevelExport> _activeExports = new();

    // Typed event sink for xdg_toplevel-specific events. Stored separately
    // from the base IWSurfaceEventSink reference so OnConfigure/OnClose can
    // be dispatched without an interface re-cast on every emit.
    private readonly WXdgTopLevelEventSinkProxy _topLevelEventSink;

    private Size? _minSize;
    private Size? _maxSize;
    private string? _title;

    private ZxdgToplevelDecorationV1? _decoration;
    // Disable SSD support completely and don't allow re-enabling it because we can't
    // TODO: Wait for V2 version of the protocol to gain more adoption and implement it on our side
    private bool _csdSticky;

    public WXdgTopLevel(WaylandWorker worker, WXdgTopLevelEventSinkProxy eventSink) : base(worker, eventSink)
    {
        _topLevelEventSink = eventSink;
    }

    public override void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        base.OnConnected(connection, globals);
        if (Globals!.Outputs.Outputs.Count == 0)
            throw new AvaloniaWaylandException("Expected at least one wl_output at this point");
        _pendingBatch = new();
        _xdgTopLevel = XdgSurface!.GetToplevel(new TopLevelListener(this));
        if (!_csdSticky && Globals.XdgDecorationManager is { } decoMgr)
        {
            _decoration = decoMgr.GetToplevelDecoration(
                _xdgTopLevel,
                new DecorationListener(this),
                connection.Queue);
            _decoration.SetMode(ZxdgToplevelDecorationV1.ModeEnum.ServerSide);
        }

        // Re-apply cached title on reconnect.
        if (_title != null)
            _xdgTopLevel.SetTitle(_title);

        // Re-apply cached min/max if they were ever set on a previous
        // (now-dead) connection. The OnConnected commit below will
        // promote the queued double-buffered state.
        if (_minSize.HasValue || _maxSize.HasValue)
            EmitMinMaxSize();
        WlSurface!.Commit();
    }

    private Size CalculateMaxSize()
    {
        // Prefer compositor-provided bounds
        if (_pendingBatch.Bounds is { Width: > 0, Height: > 0 } bounds)
            return new Size(bounds.Width, bounds.Height);
        
        // Fallback: compute from output geometry
        Size? maxSize = null;  
        var outputs = Outputs.Count > 0 ? Outputs : Globals!.Outputs.Outputs;
        foreach (var o in outputs)
        {
            // Use the same logical-size derivation as the screen snapshot
            // (xdg_output.logical_size when available with mutter-bogus
            // workaround, else mode/scale). DPR == 1 here because o.LogicalSize
            // is already in compositor logical pixels.
            var size = o.LogicalSize.ToSize(1);
            if (maxSize == null)
                maxSize = size;
            else
                maxSize = new(Math.Min(maxSize.Value.Width, size.Width),
                    Math.Min(maxSize.Value.Height, size.Height));
        }

        return maxSize ?? new Size(800, 600);
    }

    public void SetMaximized() => _xdgTopLevel?.SetMaximized();
    public void UnsetMaximized() => _xdgTopLevel?.UnsetMaximized();
    public void SetFullscreen() => _xdgTopLevel?.SetFullscreen(null!);
    public void UnsetFullscreen() => _xdgTopLevel?.UnsetFullscreen();
    public void SetMinimized() => _xdgTopLevel?.SetMinimized();

    public void SetTitle(string? title)
    {
        _title = title;
        _xdgTopLevel?.SetTitle(title ?? string.Empty);
    }

    public void SetMinMaxSize(Size? minSize, Size? maxSize)
    {
        if (minSize == _minSize && maxSize == _maxSize)
            return;
        _minSize = minSize;
        _maxSize = maxSize;
        // No-op if we aren't currently connected — _xdgTopLevel is null
        // and _wlSurface is null on disconnect; OnConnected replays the
        // cached values when a connection is (re)established.
        if (_xdgTopLevel == null)
            return;
        EmitMinMaxSize();
        // Push immediately so an idle window with no pending buffer still
        // sees the new constraints take effect. A commit without an
        // attach is well-defined: it promotes the double-buffered values
        // without redrawing. Future buffer attaches will redundantly
        // re-emit the same values, which the compositor accepts.
        WlSurface!.Commit();
    }

    private void EmitMinMaxSize()
    {
        if (_xdgTopLevel == null)
            return;

        // null => "no constraint" => send 0 (per xdg-shell spec).
        var (minW, minH) = _minSize is { } min
            ? (Math.Max(1, (int)Math.Ceiling(min.Width)),
               Math.Max(1, (int)Math.Ceiling(min.Height)))
            : (0, 0);
        var (maxW, maxH) = _maxSize is { } max
            ? (Math.Max(1, (int)Math.Floor(max.Width)),
               Math.Max(1, (int)Math.Floor(max.Height)))
            : (0, 0);

        // Spec: "Requesting a minimum size to be larger than the maximum
        // size of a surface is illegal and will result in an invalid_size
        // error." Clamp the minimum down to the maximum (≥0 maintained);
        // 0 on max means "unconstrained" and lets any min through.
        if (maxW > 0 && minW > maxW) minW = maxW;
        if (maxH > 0 && minH > maxH) minH = maxH;

        _xdgTopLevel.SetMinSize(minW, minH);
        _xdgTopLevel.SetMaxSize(maxW, maxH);
    }

    public void SetParent(IWXdgTopLevel? parent)
    {
        _xdgTopLevel?.SetParent(((WXdgTopLevel?)parent)?._xdgTopLevel);
    }

    public void Move(object? platformCookie)
    {
        if (_xdgTopLevel != null
            && platformCookie is WaylandInputEventCookie cookie
            && CurrentDisplay is { } display
            && cookie.TryConsume(display, out var seat, out var serial))
            _xdgTopLevel.Move(seat, serial);
    }

    public void Resize(object? platformCookie, XdgToplevel.ResizeEdgeEnum edge)
    {
        if (_xdgTopLevel != null
            && platformCookie is WaylandInputEventCookie cookie
            && CurrentDisplay is { } display
            && cookie.TryConsume(display, out var seat, out var serial))
            _xdgTopLevel.Resize(seat, serial, edge);
    }
    
    protected override void OnConfigureBatchComplete(uint serial)
    {
        var batch = _pendingBatch;
        batch.Serial = serial;
        batch.MaxSize = CalculateMaxSize();
        _pendingBatch = new();
        
        _topLevelEventSink.OnConfigure(batch, DispatcherPriority.AsyncRenderTargetResize);
        BasicInitCompletedTcs.TrySetResult(batch);
        
        base.OnConfigureBatchComplete(serial);
    }

    private class DecorationListener(WXdgTopLevel p) : ZxdgToplevelDecorationV1.Listener
    {
        protected override void Configure(ZxdgToplevelDecorationV1 eventSender, ZxdgToplevelDecorationV1.ModeEnum mode)
        {
            var translated = mode switch
            {
                ZxdgToplevelDecorationV1.ModeEnum.ServerSide => DecorationMode.ServerSide,
                _ => DecorationMode.ClientSide,
            };
            if (p.BasicInitCompletedTcs.Task.IsCompleted)
                p._topLevelEventSink.OnDecorationModeChanged(translated, DispatcherPriority.AsyncRenderTargetResize);
            else
                p._pendingBatch.InitialDecorationMode = translated;
        }
    }

    public void DestroyDecoration()
    {
        _csdSticky = true;
        _decoration?.Destroy();
        _decoration?.Dispose();
        _decoration = null;
    }

    internal class TopLevelListener(WXdgTopLevel p) : XdgToplevel.Listener
    {
        protected override void ConfigureBounds(XdgToplevel eventSender, int width, int height) => 
            p._pendingBatch.Bounds = new(width, height);
        
        protected override void Configure(XdgToplevel eventSender, int width, int height, ReadOnlySpan<byte> states)
        {
            p._pendingBatch.Size = new(width, height);
            p._pendingBatch.States = XdgConfigureBatch.ParseStates(states);
        }

        protected override void Close(XdgToplevel eventSender) =>
            p._topLevelEventSink.OnClose();
    }

    public override void OnDisconnected()
    {
        EventSink.OnKeyboardLeave();
        foreach (var ex in _activeExports.ToList())
            ex.Dispose();
        _activeExports.Clear();
        _decoration?.Destroy();
        _decoration = null;
        _xdgTopLevel?.Destroy();
        _xdgTopLevel = null;
        _pendingBatch = new();
        base.OnDisconnected();
    }

    /// <summary>
    /// Worker-thread: exports this toplevel via xdg-foreign-unstable-v2, returning a
    /// handle object whose Task resolves once the  compositor has emitted the handle event.
    /// Returns <c>null</c> if the exporter  global is not bound on the current connection
    /// or this toplevel isn't currently mapped.
    /// </summary>
    public IWaylandXdgTopLevelExport? ExportToplevel()
    {
        if (Globals?.XdgExporter is not { } exporter || WlSurface == null || _xdgTopLevel == null
            || Connection == null)
            return null;
        var export = new XdgToplevelExport(this, Worker, exporter, WlSurface, Connection.Queue);
        _activeExports.Add(export);
        return export.GetUiThreadHandle();
    }

    internal void RemoveExport(XdgToplevelExport export) => _activeExports.Remove(export);
}

/// <summary>
/// Worker-side xdg_popup surface. The UI thread keeps a
/// <c>WXdgPopupProxy</c> handle and feeds positioner parameters via
/// <see cref="UpdatePositioner"/>; the worker rebuilds the protocol-level
/// <c>xdg_positioner</c> and calls <c>xdg_surface.GetPopup</c> on every
/// (re-)connect (so the popup survives compositor reconnects). Popup
/// configure events are accumulated in <see cref="_pendingBatch"/> and
/// flushed to <see cref="IWXdgPopupEventSink.OnPopupConfigure"/> when the
/// wrapping <c>xdg_surface.configure(serial)</c> seals the batch.
/// </summary>
class WXdgPopup : WXdgShellSurface, IWXdgPopup
{
    private readonly WXdgPopupEventSinkProxy _popupEventSink;
    private readonly WXdgShellSurface _parent;

    private XdgPopup? _xdgPopup;
    private XdgPopupConfigureBatch _pendingBatch = new();
    private XdgPopupPositionerParams? _positioner;
    // 0 is reserved (means "no token"); start at 1 and bump on every reposition.
    private uint _nextRepositionToken = 1;

    public WXdgPopup(WaylandWorker worker, WXdgPopupEventSinkProxy eventSink, WXdgShellSurface parent)
        : base(worker, eventSink)
    {
        _popupEventSink = eventSink;
        _parent = parent;
    }
    
    public void UpdatePositioner(XdgPopupPositionerParams positioner)
    {
        var hadPrevious = _positioner.HasValue;
        _positioner = positioner;

        if (!hadPrevious)
        {
            // First positioner — attempt to attach now (may still defer
            // if the parent isn't mapped or we're not yet connected;
            // OnConnected / parent's drain will retry).
            TryAttachToParent();
            return;
        }

        if (_xdgPopup == null || Connection == null)
            return;

        // Already mapped — issue a reposition. The compositor will reply
        // with a fresh configure + repositioned(token) sequence; the
        // pending batch is reset so the next OnPopupConfigure carries the
        // post-reposition geometry.
        var p = BuildPositioner(positioner);
        if (p == null)
            return;
        try
        {
            _pendingBatch = new();
            _xdgPopup.Reposition(p, _nextRepositionToken++);
        }
        finally
        {
            p.Destroy();
            p.Dispose();
        }
    }

    public override void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        // Always create wl_surface + xdg_surface (so any pending children of
        // _ours_ can attach), even if our own popup creation is deferred
        // because the parent isn't ready yet.
        base.OnConnected(connection, globals);

        TryAttachToParent();
    }

    /// <summary>
    /// Called either from our own <see cref="OnConnected"/>, or from the
    /// parent's <see cref="WXdgShellSurface.OnBeforeNewBufferAttached"/> when
    /// finalising pending child popups whose creation was deferred because
    /// the parent wasn't mapped yet. xdg_surface.GetPopup against an
    /// unmapped parent is a protocol error, so we wait for the parent's
    /// first frame.
    /// </summary>
    internal void TryAttachToParent()
    {
        if (_xdgPopup != null) return;                         // already attached
        if (XdgSurface == null) return;                       // we're not connected
        if (_positioner is not { } pos) return;                // UI hasn't supplied positioner yet
        if (!_parent.IsMapped || _parent.XdgSurface is not { } parentXdgSurface)
        {
            // Parent not yet mapped — register; parent will call us back
            // when its first buffer commits.
            _parent.RegisterPendingChildPopup(this);
            return;
        }

        var positioner = BuildPositioner(pos);
        if (positioner == null)
            return;
        try
        {
            _pendingBatch = new();
            _xdgPopup = XdgSurface.GetPopup(parentXdgSurface, positioner, new PopupListener(this));
        }
        finally
        {
            positioner.Destroy();
            positioner.Dispose();
        }
        WlSurface!.Commit();
    }

    /// <summary>
    /// Translates the cached <see cref="XdgPopupPositionerParams"/> into a
    /// fresh <c>xdg_positioner</c> protocol object. The caller owns the
    /// returned object and must <c>Destroy</c>+<c>Dispose</c> it after use.
    /// Performs the buffer→geometry origin shift on the anchor rect (using
    /// the parent's most recent <c>SetWindowGeometry</c> as the
    /// authoritative source) and clamps it into the parent's window-geometry
    /// rectangle as required by the xdg_positioner spec.
    /// </summary>
    private XdgPositioner? BuildPositioner(XdgPopupPositionerParams p)
    {
        if (Globals == null)
            return null;

        var positioner = Globals.XdgWmBase.CreatePositioner();

        // Size: protocol requires positive integers.
        var width = Math.Max(1, (int)Math.Ceiling(p.Size.Width));
        var height = Math.Max(1, (int)Math.Ceiling(p.Size.Height));
        positioner.SetSize(width, height);

        // The anchor rect is given in the parent's *buffer-relative* logical
        // frame (true top-left, includes shadow). Wayland's xdg_positioner
        // wants coords in the parent's *window-geometry* frame (excludes
        // shadow). Use the parent's most recent SetWindowGeometry as the
        // single source of truth — it's what the compositor sees, and using
        // it avoids any drift between our shadow-extent state and the
        // geometry actually committed.
        //
        // If the parent hasn't reported geometry yet, that's a contract
        // violation (we shouldn't be building a positioner before the
        // parent is mapped) — fall back to assuming origin (0,0) and skip
        // clamping rather than crashing.
        var parentGeom = _parent.LastWindowGeometry;
        var originX = parentGeom?.Left ?? 0;
        var originY = parentGeom?.Top ?? 0;

        var anchorX = (int)Math.Round(p.AnchorRect.X) - originX;
        var anchorY = (int)Math.Round(p.AnchorRect.Y) - originY;
        var anchorW = Math.Max(0, (int)Math.Round(p.AnchorRect.Width));
        var anchorH = Math.Max(0, (int)Math.Round(p.AnchorRect.Height));

        // Clamp into the parent's window geometry. The protocol forbids the
        // anchor rect from extending outside the parent's geometry; rather
        // than failing the call (which would protocol-error the connection)
        // we clip it. UI-side may produce slightly off-edge anchor rects
        // when, e.g., a context menu opens near the corner of a window.
        //
        // The clamp must produce a strictly positive (>= 1×1) rectangle:
        // xdg_positioner accepts a zero-sized anchor rect via SetAnchorRect,
        // but the spec considers such a positioner *incomplete* and the
        // subsequent get_popup/reposition raises xdg_wm_base.invalid_positioner
        // — a fatal error that disconnects every Avalonia window. So we
        // shift the floor back by one when the clamp would collapse to zero.
        if (parentGeom is { Width: > 0, Height: > 0 } g)
        {
            var maxX0 = Math.Max(0, g.Width - 1);
            var maxY0 = Math.Max(0, g.Height - 1);
            var x0 = Math.Clamp(anchorX, 0, maxX0);
            var y0 = Math.Clamp(anchorY, 0, maxY0);
            var x1 = Math.Clamp(anchorX + Math.Max(1, anchorW), x0 + 1, g.Width);
            var y1 = Math.Clamp(anchorY + Math.Max(1, anchorH), y0 + 1, g.Height);
            anchorX = x0;
            anchorY = y0;
            anchorW = x1 - x0;
            anchorH = y1 - y0;
        }
        else
        {
            // No parent geometry to clamp against — still ensure the
            // anchor rect is at least 1×1 to keep the positioner valid.
            anchorW = Math.Max(1, anchorW);
            anchorH = Math.Max(1, anchorH);
        }

        positioner.SetAnchorRect(anchorX, anchorY, anchorW, anchorH);
        positioner.SetAnchor(p.Anchor);
        positioner.SetGravity(p.Gravity);
        positioner.SetConstraintAdjustment((XdgPositioner.ConstraintAdjustmentEnum)p.ConstraintAdjustment);

        // Offset is post-resolution (popup-relative); no parent/shadow shift.
        if (p.Offset.X != 0 || p.Offset.Y != 0)
            positioner.SetOffset((int)Math.Round(p.Offset.X), (int)Math.Round(p.Offset.Y));

        return positioner;
    }

    protected override void OnConfigureBatchComplete(uint serial)
    {
        var batch = _pendingBatch;
        batch.Serial = serial;
        _pendingBatch = new();

        _popupEventSink.OnPopupConfigure(batch, DispatcherPriority.AsyncRenderTargetResize);

        base.OnConfigureBatchComplete(serial);
    }

    internal class PopupListener(WXdgPopup p) : XdgPopup.Listener
    {
        protected override void Configure(XdgPopup eventSender, int x, int y, int width, int height)
        {
            p._pendingBatch.X = x;
            p._pendingBatch.Y = y;
            p._pendingBatch.Width = width;
            p._pendingBatch.Height = height;
        }

        protected override void PopupDone(XdgPopup eventSender) =>
            p._popupEventSink.OnPopupDone();

        // Repositioned(token) acks a previous Reposition request. We don't
        // currently surface the token to the UI side (the UI doesn't track
        // outstanding reposition requests — the geometry already arrives
        // via the matching configure event). If we ever need round-trip
        // reposition acknowledgement, plumb the token through OnPopupConfigure.
        protected override void Repositioned(XdgPopup eventSender, uint token) { }
    }

    public override void OnDisconnected()
    {
        EventSink.OnKeyboardLeave();
        // Unregister from the parent's pending list in case we were
        // deferred and the connection went down before we got attached.
        _parent.UnregisterPendingChildPopup(this);
        _xdgPopup?.Destroy();
        _xdgPopup?.Dispose();
        _xdgPopup = null;
        _pendingBatch = new();
        base.OnDisconnected();
    }
}