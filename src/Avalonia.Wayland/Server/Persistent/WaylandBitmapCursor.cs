using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Transient;
using Avalonia.Wayland.Server.Transient.Rendering;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// A custom bitmap cursor. A persistent worker-thread entity that owns a bare <c>wl_surface</c>
/// holding the cursor image. Unlike <see cref="WSurface"/> it has no xdg-shell role, no configure
/// handshake and no frame-callback throttling: the image is rendered once per connection and the
/// surface is only ever referenced by <c>wl_pointer.set_cursor</c>. Rendering reuses
/// <see cref="WaylandFramebuffer"/> via <see cref="IWaylandFramebufferSurface"/>.
/// </summary>
sealed class WaylandBitmapCursor : WaylandCursor, IPersistentWaylandObject, IWaylandFramebufferSurface
{
    private readonly WaylandWorker _worker;
    private readonly byte[] _pixels; // tightly packed Bgra8888 premultiplied, stride = Width * 4
    private readonly PixelSize _size;
    private readonly int _hotspotX;
    private readonly int _hotspotY;
    private WaylandConnection? _connection;
    private WaylandGlobals? _globals;
    private WlSurface? _wlSurface;
    private readonly List<IDisposable> _activeRenderTargets = new();

    public WaylandBitmapCursor(WaylandWorker worker, byte[] pixels, PixelSize size, int hotspotX, int hotspotY)
    {
        _worker = worker;
        _pixels = pixels;
        _size = size;
        _hotspotX = hotspotX;
        _hotspotY = hotspotY;
        worker.PostOob(() => worker.RegisterPersistentObject(this));
    }

    public WaylandGlobals? Globals => _globals;
    public WlSurface? WlSurface => _wlSurface;

    public PlatformRenderTargetState State =>
        _globals != null && _wlSurface != null ? PlatformRenderTargetState.Ready : default;

    public void RegisterRenderTarget(IDisposable renderTarget) => _activeRenderTargets.Add(renderTarget);
    public void UnregisterRenderTarget(IDisposable renderTarget) => _activeRenderTargets.Remove(renderTarget);

    // No xdg role and no frame-callback throttling: nothing to stage before a buffer attach.
    public void OnBeforeNewBufferAttached(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
    }

    // Cursor buffers are created outside the throttled render loop, so flush their fds eagerly.
    public bool EnforceBufferCreationRoundtrip => true;

    public override WaylandCursorImage? Resolve(WaylandGlobals globals)
        => _wlSurface is { } surface ? new WaylandCursorImage(surface, _hotspotX, _hotspotY) : null;

    public void OnConnected(WaylandConnection connection, WaylandGlobals globals)
    {
        _connection = connection;
        _globals = globals;
        _wlSurface = globals.WlCompositor.CreateSurface(null);
        Render();
    }

    private void Render()
    {
        if (_size.Width <= 0 || _size.Height <= 0)
            return;

        var framebuffer = new WaylandFramebuffer(this);
        using var renderTarget = framebuffer.CreateFramebufferRenderTarget();
        using var locked = renderTarget.Lock(new IRenderTarget.RenderTargetSceneInfo(_size, 1, CompositionTransparencyLevel.None), out _);
        // The locked framebuffer is Bgra8888 premultiplied with stride == Width * 4, matching the
        // packed pixel data we captured on the UI thread. Disposing the lock attaches + commits.
        Marshal.Copy(_pixels, 0, locked.Address, _pixels.Length);
    }

    public void OnDisconnected()
    {
        foreach (var rt in _activeRenderTargets.ToList())
            rt.Dispose();
        _wlSurface?.Destroy();
        _wlSurface = null;
        _globals = null;
        _connection = null;
    }

    // Invoked on the worker thread via WaylandCursorProxy when the owning UI-thread cursor is disposed.
    public override void Destroy() => _worker.UnregisterPersistentObject(this);
}
