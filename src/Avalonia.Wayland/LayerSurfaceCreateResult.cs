using System;
using System.Threading.Tasks;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

/// <summary>
/// Result of creating a worker-side <c>zwlr_layer_surface_v1</c> from the UI
/// thread. Mirrors <see cref="WaylandSurfaceCreateResult{T}"/>, but carries a
/// <see cref="LayerConfigure"/> initial batch instead of an xdg one.
/// </summary>
internal sealed class LayerSurfaceCreateResult(
    WLayerSurfaceProxy proxy,
    Func<IPlatformRenderSurface[]> getRenderSurfaces,
    Task<LayerConfigure> basicInitCompleted)
{
    public WLayerSurfaceProxy Proxy { get; } = proxy;

    public Func<IPlatformRenderSurface[]> GetRenderSurfaces { get; } = getRenderSurfaces;

    public Task<LayerConfigure> BasicInitCompleted { get; } = basicInitCompleted;
}
