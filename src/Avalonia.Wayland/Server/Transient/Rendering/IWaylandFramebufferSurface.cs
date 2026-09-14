using System;
using Avalonia.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server.Transient.Rendering;

/// <summary>
/// The minimal surface contract <see cref="WaylandFramebuffer"/> needs to attach shm buffers.
/// Implemented by <see cref="Persistent.WSurface"/> (full toplevel/popup lifecycle) and by
/// lightweight entities (e.g. cursor surfaces) that own a bare <c>wl_surface</c> without the
/// xdg-shell configure handshake or frame-callback throttling.
/// </summary>
interface IWaylandFramebufferSurface
{
    /// <summary>The bound globals for the current connection, or <c>null</c> when disconnected.</summary>
    WaylandGlobals? Globals { get; }

    /// <summary>The target <c>wl_surface</c>, or <c>null</c> when disconnected.</summary>
    WlSurface? WlSurface { get; }

    /// <summary>Current readiness of the surface for buffer attachment.</summary>
    PlatformRenderTargetState State { get; }

    /// <summary>Tracks an active render target so it can be torn down on disconnect.</summary>
    void RegisterRenderTarget(IDisposable renderTarget);

    /// <summary>Stops tracking a render target.</summary>
    void UnregisterRenderTarget(IDisposable renderTarget);

    /// <summary>
    /// Invoked right before a new buffer is attached and committed. Implementations stage any
    /// per-commit double-buffered state here (frame callback, ack_configure, scale, geometry…).
    /// Lightweight surfaces can leave this a no-op.
    /// </summary>
    void OnBeforeNewBufferAttached(IRenderTarget.RenderTargetSceneInfo sceneInfo);

    /// <summary>
    /// When <c>true</c>, <see cref="WaylandFramebuffer"/> forces a <c>wl_display</c> roundtrip after
    /// creating each <c>wl_buffer</c>. libwayland-client only allows a bounded number of file
    /// descriptors per <c>wl_display_flush</c> (~28, undocumented), so surfaces that create buffers
    /// outside the throttled render loop (e.g. cursors) must flush eagerly to stay within budget.
    /// </summary>
    bool EnforceBufferCreationRoundtrip { get; }
}
