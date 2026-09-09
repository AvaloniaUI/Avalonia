using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NWayland;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Double-buffered wl_surface state. Pending fields accumulate between commits; <see cref="Commit"/> promotes
/// them, produces a fresh <see cref="Bitmap"/> from the committed SHM buffer (copied once, then the buffer is
/// released), runs the role's commit hook, and — for a mapped root — assembles a <see cref="SurfaceCommit"/>
/// snapshot and hands it to the UI. Compositor-thread only.
/// </summary>
internal sealed class SurfaceState
{
    public SurfaceState(uint id, WlSurface.Server resource, ClientContext client)
    {
        Id = id;
        Resource = resource;
        Client = client;
    }

    public uint Id { get; }
    public WlSurface.Server Resource { get; }
    public ClientContext Client { get; }

    private ShmBufferState? _pendingBuffer;
    private int _pendingBufferX, _pendingBufferY;
    private bool _pendingBufferAttached;
    private readonly List<WlCallback.Server> _pendingFrameCallbacks = new();
    private readonly List<WlCallback.Server> _activeFrameCallbacks = new();

    // Cached state for a synchronized subsurface: its own commit only caches; the parent's commit applies it.
    private ShmBufferState? _cachedBuffer;
    private bool _cachedBufferAttached;
    private bool _hasCachedBuffer;
    private readonly List<WlCallback.Server> _cachedFrameCallbacks = new();

    private Bitmap? _freshBitmap;

    // wl_surface.set_buffer_scale (HiDPI): double-buffered like the buffer. The bitmap is tagged at 96*scale
    // DPI so Avalonia lays it out at bufferPx/scale DIPs; Width/Height below are in those DIPs too.
    private int _pendingBufferScale = 1;
    private int _bufferScale = 1;

    /// <summary>Surface size in DIPs (buffer pixels ÷ buffer_scale).</summary>
    public int Width { get; private set; }
    public int Height { get; private set; }

    public XdgSurfaceState? XdgSurface { get; set; }
    public SubsurfaceState? Subsurface { get; set; }

    public List<SubsurfaceState> ChildrenAbove { get; } = new();
    public List<SubsurfaceState> ChildrenBelow { get; } = new();

    public bool HasActiveFrameCallbacks => _activeFrameCallbacks.Count > 0;

    public void Attach(ShmBufferState? buffer, int x, int y)
    {
        //Console.Error.WriteLine($"Buffer attached {buffer?.Width}:{buffer?.Height}");
        _pendingBuffer = buffer;
        _pendingBufferX = x;
        _pendingBufferY = y;
        _pendingBufferAttached = true;
        buffer?.Reattach();
    }

    public void AddFrameCallback(WlCallback.Server callback) => _pendingFrameCallbacks.Add(callback);

    public void SetPendingBufferScale(int scale)
    {
        if (scale > 0)
            _pendingBufferScale = scale;
    }

    /// <summary>Send wl_surface.preferred_buffer_scale (HiDPI hint). No-op on pre-v6 surfaces (the event
    /// was introduced in wl_surface v6); the client's negotiated wl_compositor version gates it.</summary>
    public void SendPreferredBufferScale(int scale)
    {
        if (Client.CompositorVersion >= 6)
            Resource.PreferredBufferScale(scale);
    }

    /// <summary>
    /// The client committed THIS surface. A <em>synchronized</em> subsurface only caches its pending state —
    /// per wl_subsurface semantics it becomes current atomically when its parent commits, not now. Every
    /// other surface (the toplevel root, a desynchronized subsurface) promotes pending→current immediately
    /// and delivers the tree.
    /// </summary>
    public void ClientCommit(CompositorState state)
    {
        if (Subsurface is { IsSync: true })
        {
            CachePendingState();
            return;
        }
        PromoteAndDeliver(state, deliver: true);
    }

    /// <summary>Move pending→cached for a sync subsurface; the parent's commit promotes it via <see cref="ApplyCachedAndPromote"/>.</summary>
    private void CachePendingState()
    {
        if (_pendingBufferAttached)
        {
            // A second sync commit before the parent applies replaces the cached buffer — release the
            // superseded one so the client may reuse it (it never became current).
            if (_hasCachedBuffer)
                _cachedBuffer?.Release();
            _cachedBuffer = _pendingBuffer;
            _cachedBufferAttached = true;
            _hasCachedBuffer = true;
            _pendingBuffer = null;
            _pendingBufferAttached = false;
        }
        _cachedFrameCallbacks.AddRange(_pendingFrameCallbacks);
        _pendingFrameCallbacks.Clear();
    }

    /// <summary>
    /// Parent commit applying this sync child: fold cached→pending, promote, and recurse into our own sync
    /// children. We never deliver here — the root that started the parent commit delivers the whole tree once.
    /// </summary>
    public void ApplyCachedAndPromote(CompositorState state)
    {
        if (_hasCachedBuffer)
        {
            _pendingBuffer = _cachedBuffer;
            _pendingBufferAttached = _cachedBufferAttached;
            _cachedBuffer = null;
            _hasCachedBuffer = false;
            _cachedBufferAttached = false;
        }
        if (_cachedFrameCallbacks.Count > 0)
        {
            _pendingFrameCallbacks.AddRange(_cachedFrameCallbacks);
            _cachedFrameCallbacks.Clear();
        }
        PromoteAndDeliver(state, deliver: false);
    }

    private void PromoteAndDeliver(CompositorState state, bool deliver)
    {
        if (_pendingBufferAttached)
        {
            _pendingBufferAttached = false;
            var buffer = _pendingBuffer;
            _pendingBuffer = null;

            if (buffer is not null)
            {
                _bufferScale = _pendingBufferScale;
                Width = buffer.Width / _bufferScale;   // DIPs
                Height = buffer.Height / _bufferScale;
                var bitmap = BuildBitmap(buffer);
                buffer.Release(); // copied into the bitmap — give it straight back to the client (D6)
                if (bitmap is not null)
                {
                    _freshBitmap?.Dispose();
                    _freshBitmap = bitmap;
                }
            }
        }

        _activeFrameCallbacks.AddRange(_pendingFrameCallbacks);
        _pendingFrameCallbacks.Clear();

        // Role commit may map the toplevel (enqueues ToplevelMapped before our snapshot below).
        XdgSurface?.OnSurfaceCommit(state);

        // Apply sync children atomically with this parent commit.
        foreach (var child in ChildrenAbove)
            if (child.IsSync)
                child.ApplyCachedState(state);
        foreach (var child in ChildrenBelow)
            if (child.IsSync)
                child.ApplyCachedState(state);

        if (deliver)
            DeliverFrame(state);
    }

    private void DeliverFrame(CompositorState state)
    {
        // A wl_pointer.set_cursor surface isn't part of any host tree — capture its committed buffer as the cursor
        // image for the host the pointer is currently over, rather than trying to render it into a host.
        if (ReferenceEquals(Client.CursorSurface, this))
        {
            if (Client.CurrentPointerHostId != 0)
                DeliverCursor(state, Client.CurrentPointerHostId, Client.CursorHotspotX, Client.CursorHotspotY);
            FireFrameCallbacks();
            return;
        }

        var rootXdg = FindRenderRoot();
        if (rootXdg is not { IsMappedRenderRoot: true })
        {
            // No mapped host to render into (e.g. the "commit an empty surface with a frame callback, wait
            // for done, then attach the first buffer" startup pattern). Don't strand callbacks waiting for a
            // future delivering commit — there's nothing to throttle against. Fire this surface AND its
            // subtree (a sync child's callbacks were just folded in by the parent commit above).
            FireFrameCallbacksTree();
            return;
        }

        var rootSurface = rootXdg.Surface;
        var items = new List<SurfaceDrawItem>();
        var newBitmaps = new Dictionary<uint, Bitmap>();
        var frameIds = new List<uint>();

        double geoX = rootXdg.HasGeometry ? rootXdg.GeometryX : 0;
        double geoY = rootXdg.HasGeometry ? rootXdg.GeometryY : 0;
        rootSurface.Visit(-geoX, -geoY, items, newBitmaps, frameIds);

        double clipW = rootXdg.HasGeometry ? rootXdg.GeometryWidth : rootSurface.Width;
        double clipH = rootXdg.HasGeometry ? rootXdg.GeometryHeight : rootSurface.Height;

        // Mapped: route everything (new pixels and/or frame callbacks) through the UI render so callbacks
        // fire on Avalonia's present cadence, never at raw network speed.
        if (newBitmaps.Count > 0 || frameIds.Count > 0)
            state.ToUi.Enqueue(new SurfaceCommitEvent(
                new SurfaceCommit(rootXdg.RenderHostId, items, newBitmaps, frameIds, clipW, clipH)));
    }

    private void Visit(double offsetX, double offsetY,
        List<SurfaceDrawItem> items, Dictionary<uint, Bitmap> newBitmaps, List<uint> frameIds)
    {
        foreach (var child in ChildrenBelow)
            child.Surface.Visit(offsetX + child.X, offsetY + child.Y, items, newBitmaps, frameIds);

        items.Add(new SurfaceDrawItem(Id, offsetX, offsetY));
        if (_freshBitmap is not null)
        {
            newBitmaps[Id] = _freshBitmap;
            _freshBitmap = null;
        }
        if (HasActiveFrameCallbacks)
            frameIds.Add(Id);

        foreach (var child in ChildrenAbove)
            child.Surface.Visit(offsetX + child.X, offsetY + child.Y, items, newBitmaps, frameIds);
    }

    /// <summary>Fire and clear this surface's deferred frame callbacks. Called after the UI renders.</summary>
    public void FireFrameCallbacks()
    {
        if (_activeFrameCallbacks.Count == 0)
            return;
        var timestamp = unchecked((uint)(Environment.TickCount64 & 0xFFFFFFFF));
        foreach (var callback in _activeFrameCallbacks)
        {
            try
            {
                callback.Done(timestamp);
                callback.Dispose();
            }
            catch { /* client may be gone */ }
        }
        _activeFrameCallbacks.Clear();
    }

    /// <summary>Fire deferred frame callbacks for this surface and its whole subtree (the not-mapped path,
    /// where the mapped tree-walk in <see cref="DeliverFrame"/> doesn't run).</summary>
    private void FireFrameCallbacksTree()
    {
        FireFrameCallbacks();
        foreach (var child in ChildrenBelow)
            child.Surface.FireFrameCallbacksTree();
        foreach (var child in ChildrenAbove)
            child.Surface.FireFrameCallbacksTree();
    }

    /// <summary>Walk up the subsurface chain to the xdg_surface that carries this tree's render root — a
    /// toplevel (auto-window / embedded control) or a popup (its own Avalonia Popup). Each renders independently.</summary>
    public XdgSurfaceState? FindRenderRoot()
    {
        if (XdgSurface is { } xdg && (xdg.Toplevel is not null || xdg.Popup is not null))
            return xdg;
        return Subsurface?.Parent.FindRenderRoot();
    }

    /// <summary>True if a committed buffer has been turned into a bitmap that hasn't been consumed yet — used by
    /// set_cursor to emit a cursor image that was committed before the set_cursor request arrived.</summary>
    internal bool HasFreshBitmap => _freshBitmap is not null;

    /// <summary>Hand this surface's committed bitmap to the UI as the cursor image for <paramref name="hostId"/>
    /// (ownership transfers; a null bitmap means "no image" → the host reverts to its default cursor).</summary>
    internal void DeliverCursor(CompositorState state, uint hostId, int hotspotX, int hotspotY)
    {
        var bitmap = _freshBitmap;
        _freshBitmap = null;
        state.ToUi.Enqueue(new CursorChangedEvent(hostId, bitmap, hotspotX, hotspotY));
    }

    /// <summary>
    /// Topmost surface in this tree containing the point (this surface's local coords; children offset by their
    /// subsurface position), returning the hit surface and the point in ITS local coords. Paint order is
    /// below-children → self → above-children, so hit-testing checks above (topmost first), then self, then below.
    /// Input regions (set_input_region) are not honored — the whole surface rect is hit-testable (a simplification).
    /// </summary>
    public SurfaceState? HitTest(double x, double y, out double localX, out double localY)
    {
        for (var i = ChildrenAbove.Count - 1; i >= 0; i--)
        {
            var child = ChildrenAbove[i];
            if (child.Surface.HitTest(x - child.X, y - child.Y, out localX, out localY) is { } hitAbove)
                return hitAbove;
        }
        if (x >= 0 && y >= 0 && x < Width && y < Height)
        {
            localX = x;
            localY = y;
            return this;
        }
        for (var i = ChildrenBelow.Count - 1; i >= 0; i--)
        {
            var child = ChildrenBelow[i];
            if (child.Surface.HitTest(x - child.X, y - child.Y, out localX, out localY) is { } hitBelow)
                return hitBelow;
        }
        localX = localY = 0;
        return null;
    }

    private Bitmap? BuildBitmap(ShmBufferState buffer)
    {
        var src = buffer.GetData();
        if (src == IntPtr.Zero)
            return null;
        var alpha = buffer.Format == WlShm.FormatEnum.Xrgb8888 ? AlphaFormat.Opaque : AlphaFormat.Premul;
        var dpi = 96.0 * _bufferScale; // tag HiDPI buffers so Avalonia draws them at bufferPx/scale DIPs
        return new Bitmap(
            PixelFormat.Bgra8888,
            alpha,
            src,
            new PixelSize(buffer.Width, buffer.Height),
            new Vector(dpi, dpi),
            buffer.Stride);
    }

    public void Destroy(CompositorState state)
    {
        if (ReferenceEquals(Client.PointerFocus, this))
            Client.PointerFocus = null; // don't leave a dangling pointer focus on a destroyed surface
        if (ReferenceEquals(Client.CursorSurface, this))
            Client.CursorSurface = null; // nor a dangling cursor surface (the client may destroy its cursor surface)
        XdgSurface?.Destroy(state);
        XdgSurface = null;

        foreach (var cb in _activeFrameCallbacks)
            try { cb.Dispose(); } catch { }
        _activeFrameCallbacks.Clear();
        foreach (var cb in _pendingFrameCallbacks)
            try { cb.Dispose(); } catch { }
        _pendingFrameCallbacks.Clear();
        foreach (var cb in _cachedFrameCallbacks)
            try { cb.Dispose(); } catch { }
        _cachedFrameCallbacks.Clear();
        if (_hasCachedBuffer)
        {
            _cachedBuffer?.Release();
            _cachedBuffer = null;
            _hasCachedBuffer = false;
        }

        _freshBitmap?.Dispose();
        _freshBitmap = null;

        state.UnregisterSurface(this);
        Client.Surfaces.Remove(this);
    }
}

/// <summary>wl_subsurface: position, sync mode, stacking, and sync-mode cached-state apply.</summary>
internal sealed class SubsurfaceState
{
    public SubsurfaceState(SurfaceState surface, SurfaceState parent)
    {
        Surface = surface;
        Parent = parent;
        Surface.Subsurface = this;
        parent.ChildrenAbove.Add(this); // default: immediately above the parent
    }

    public WlSubsurface.Server Resource { get; private set; } = null!;
    public SurfaceState Surface { get; }
    public SurfaceState Parent { get; }

    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsSync { get; private set; } = true;

    private int _cachedX, _cachedY;
    private bool _hasCachedPosition;

    public void Attach(WlSubsurface.Server resource) => Resource = resource;

    public void SetPosition(int x, int y)
    {
        if (IsSync)
        {
            _cachedX = x;
            _cachedY = y;
            _hasCachedPosition = true;
        }
        else
        {
            X = x;
            Y = y;
        }
    }

    public void PlaceAbove(SurfaceState sibling)
    {
        RemoveFromParent();
        var idx = Parent.ChildrenAbove.FindIndex(s => s.Surface == sibling);
        if (idx >= 0)
            Parent.ChildrenAbove.Insert(idx + 1, this);
        else
            Parent.ChildrenAbove.Add(this);
    }

    public void PlaceBelow(SurfaceState sibling)
    {
        RemoveFromParent();
        var idx = Parent.ChildrenBelow.FindIndex(s => s.Surface == sibling);
        if (idx >= 0)
            Parent.ChildrenBelow.Insert(idx, this);
        else
            Parent.ChildrenBelow.Add(this);
    }

    public void SetSync() => IsSync = true;
    public void SetDesync() => IsSync = false;

    public void ApplyCachedState(CompositorState state)
    {
        if (_hasCachedPosition)
        {
            X = _cachedX;
            Y = _cachedY;
            _hasCachedPosition = false;
        }
        Surface.ApplyCachedAndPromote(state);
    }

    public void Destroy()
    {
        RemoveFromParent();
        Surface.Subsurface = null;
    }

    private void RemoveFromParent()
    {
        Parent.ChildrenAbove.Remove(this);
        Parent.ChildrenBelow.Remove(this);
    }
}

internal sealed class CompositorListener : WlCompositor.ServerListener
{
    private readonly ClientContext _client;
    public CompositorListener(ClientContext client) => _client = client;

    protected override void CreateSurface(WlCompositor.Server resource, NewId<WlSurface.Server, WlSurface.ServerListener> id)
    {
        var listener = new SurfaceListener();
        var surfaceResource = id.GetAndConsume(listener);
        var state = new SurfaceState(_client.State.NextSurfaceId(), surfaceResource, _client);
        listener.Init(_client, state);
        _client.Surfaces.Add(state);
        _client.State.RegisterSurface(state);
    }

    protected override void CreateRegion(WlCompositor.Server resource, NewId<WlRegion.Server, WlRegion.ServerListener> id)
        => id.GetAndConsume(new RegionListener());

    protected override void Release(WlCompositor.Server resource) => resource.Dispose();
}

internal sealed class SurfaceListener : WlSurface.ServerListener
{
    private ClientContext _client = null!;
    private SurfaceState _state = null!;

    public void Init(ClientContext client, SurfaceState state)
    {
        _client = client;
        _state = state;
    }

    protected override void Attach(WlSurface.Server resource, WlBuffer.Server? buffer, int x, int y)
        => _state.Attach(buffer is null ? null : _client.GetBuffer(buffer), x, y);

    protected override void Damage(WlSurface.Server resource, int x, int y, int width, int height) { }
    protected override void DamageBuffer(WlSurface.Server resource, int x, int y, int width, int height) { }

    protected override void Frame(WlSurface.Server resource, NewId<WlCallback.Server, WlCallback.ServerListener> callback)
        => _state.AddFrameCallback(callback.GetAndConsume());

    protected override void GetRelease(WlSurface.Server resource, NewId<WlCallback.Server, WlCallback.ServerListener> callback)
        => callback.GetAndConsume(); // explicit-sync release point unused with our copy-at-commit model (D6)

    protected override void SetOpaqueRegion(WlSurface.Server resource, WlRegion.Server? region) { }
    protected override void SetInputRegion(WlSurface.Server resource, WlRegion.Server? region) { }
    protected override void SetBufferTransform(WlSurface.Server resource, WlOutput.TransformEnum transform) { }
    protected override void SetBufferScale(WlSurface.Server resource, int scale) => _state.SetPendingBufferScale(scale);
    protected override void Offset(WlSurface.Server resource, int x, int y) { }

    protected override void Commit(WlSurface.Server resource) => _state.ClientCommit(_client.State);

    protected override void Destroy(WlSurface.Server resource)
    {
        _state.Destroy(_client.State);
        resource.Dispose();
    }
}

internal sealed class RegionListener : WlRegion.ServerListener
{
    protected override void Add(WlRegion.Server resource, int x, int y, int width, int height) { }
    protected override void Subtract(WlRegion.Server resource, int x, int y, int width, int height) { }
    protected override void Destroy(WlRegion.Server resource) => resource.Dispose();
}

internal sealed class SubcompositorListener : WlSubcompositor.ServerListener
{
    private readonly ClientContext _client;
    public SubcompositorListener(ClientContext client) => _client = client;

    protected override void GetSubsurface(WlSubcompositor.Server resource,
        NewId<WlSubsurface.Server, WlSubsurface.ServerListener> id, WlSurface.Server? surface, WlSurface.Server? parent)
    {
        var surfaceState = _client.State.GetSurface(surface);
        var parentState = _client.State.GetSurface(parent);
        if (surfaceState is null || parentState is null)
        {
            id.GetAndConsume(new SubsurfaceListener(null));
            return;
        }
        var subsurface = new SubsurfaceState(surfaceState, parentState);
        var subsurfaceResource = id.GetAndConsume(new SubsurfaceListener(subsurface));
        subsurface.Attach(subsurfaceResource);
    }

    protected override void Destroy(WlSubcompositor.Server resource) => resource.Dispose();
}

internal sealed class SubsurfaceListener : WlSubsurface.ServerListener
{
    private readonly SubsurfaceState? _state;
    public SubsurfaceListener(SubsurfaceState? state) => _state = state;

    protected override void SetPosition(WlSubsurface.Server resource, int x, int y) => _state?.SetPosition(x, y);

    protected override void PlaceAbove(WlSubsurface.Server resource, WlSurface.Server? sibling)
    {
        if (_state is null)
            return;
        var siblingState = _state.Parent.Client.State.GetSurface(sibling);
        if (siblingState is not null)
            _state.PlaceAbove(siblingState);
    }

    protected override void PlaceBelow(WlSubsurface.Server resource, WlSurface.Server? sibling)
    {
        if (_state is null)
            return;
        var siblingState = _state.Parent.Client.State.GetSurface(sibling);
        if (siblingState is not null)
            _state.PlaceBelow(siblingState);
    }

    protected override void SetSync(WlSubsurface.Server resource) => _state?.SetSync();
    protected override void SetDesync(WlSubsurface.Server resource) => _state?.SetDesync();

    protected override void Destroy(WlSubsurface.Server resource)
    {
        _state?.Destroy();
        resource.Dispose();
    }
}
