using System;
using System.Threading.Tasks;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

/// <summary>
/// Result of creating a worker-side wayland surface from the UI thread.
/// Bundles the UI→worker proxy together with whatever extra UI-thread
/// accessors the caller needs to drive the surface.
/// </summary>
/// <typeparam name="T">
/// The concrete proxy subtype (e.g. <see cref="WXdgTopLevelProxy"/>) — all
/// such proxies derive from <see cref="WSurfaceProxy"/>.
/// </typeparam>
/// <remarks>
/// <see cref="GetRenderSurfaces"/> is a callback rather than a snapshot
/// because <c>ITopLevelImpl.Surfaces</c> is queried by the render thread,
/// not the UI thread; routing it through a callback avoids any need for the
/// UI side to cache (or even read) worker-thread state.
/// </remarks>
internal sealed record WaylandSurfaceCreateResult<T>(
    T Proxy,
    Func<IPlatformRenderSurface[]> GetRenderSurfaces,
    Task<XdgConfigureBatch> BasicInitCompleted)
    where T : WSurfaceProxy;
