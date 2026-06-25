using System;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland.Server;

class WaylandPlatformGraphics : IPlatformGraphicsWithFeatures, IPlatformGraphicsReadyStateFeature
{
    private IWaylandGraphics? _inner;

    public interface IWaylandGraphics : IDisposable
    {
        IPlatformGraphicsContext CreateContext();
        IPlatformRenderSurface CreateRenderSurface(WSurface surface);
    }

    public bool UsesSharedContext => false;
    public IPlatformGraphicsContext CreateContext() => (_inner ?? throw new InvalidOperationException()).CreateContext();

    /// <summary>
    /// Currently-bound backend, or <c>null</c> if none. Identity also
    /// serves as a cache key for callers that need to invalidate
    /// per-backend state across reconnects.
    /// </summary>
    public IWaylandGraphics? CurrentGraphics => _inner;

    public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IPlatformGraphicsReadyStateFeature))
            return this;
        return null;
    }

    public bool IsReady { get; private set; }

    public bool UsesContexts => _inner != null;

    public void Reset()
    {
        IsReady = false;
        _inner?.Dispose();
        _inner = null;
    }

    public void Initialize(IWaylandGraphics? graphics)
    {
        _inner?.Dispose();
        _inner = graphics;
        IsReady = true;
    }
}