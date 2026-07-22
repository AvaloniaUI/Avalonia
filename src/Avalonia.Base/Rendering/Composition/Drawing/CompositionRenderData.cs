using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

/// <summary>
/// Adapts a <see cref="CompositionRenderData"/> to <see cref="ICompositionRenderResource"/>
/// so a parent CompositionRenderData can manage the nested one's lifecycle via its _resources list.
/// AddRef/Release always happen on the UI thread, avoiding cross-thread disposal issues.
/// </summary>
internal class CompositionRenderDataResourceRef : ICompositionRenderResource
{
    private readonly CompositionRenderData _renderData;

    public CompositionRenderDataResourceRef(CompositionRenderData renderData) => _renderData = renderData;

    public void AddRefOnCompositor(Compositor c) => _renderData.AddRef();

    public void ReleaseOnCompositor(Compositor c) => _renderData.Dispose();
}

internal class CompositionRenderData : ICompositorSerializable, IDisposable
{
    private readonly Compositor _compositor;
    private readonly RenderDataStream _stream;
    private PooledInlineList<ICompositionRenderResource> _resources;
    private bool _itemsSent;
    private int _refCount = 1;

    public CompositionRenderData(Compositor compositor, RenderDataStream stream)
    {
        _compositor = compositor;
        _stream = stream;
        Server = new ServerCompositionRenderData(compositor.Server);
    }

    /// <summary>
    /// Creates render data over an empty stream. Used for empty recordings, which
    /// still need a server twin so consumers (e.g. a recording visual) can
    /// serialize a reference to them.
    /// </summary>
    public CompositionRenderData(Compositor compositor)
        : this(compositor, new RenderDataStream())
    {
    }

    public ServerCompositionRenderData Server { get; }

    public void AddResource(ICompositionRenderResource resource) => _resources.Add(resource);

    public void AddRef() => _refCount++;

    public void Dispose()
    {
        if (--_refCount > 0)
            return;

        if (!_itemsSent)
            _stream.DisposeResources();

        foreach (var r in _resources)
            r.ReleaseOnCompositor(_compositor);
        _resources.Dispose();

        _stream.Dispose();
        _itemsSent = false;

        _compositor.DisposeOnNextBatch(Server);
    }

    public SimpleServerObject TryGetServer(Compositor c) => Server;

    public void SerializeChanges(Compositor c, BatchStreamWriter writer)
    {
        _stream.SerializeTo(writer);
        _itemsSent = true;
    }

    /// <summary>
    /// The recorded bounds as seen from the UI thread. Resolved through the live
    /// client resources, so the result is valid synchronously after recording,
    /// before any commit populated the server resource shadows.
    /// </summary>
    public Rect? Bounds
        => ServerCompositionRenderData.ApplyRenderBoundsRounding(
            _stream.CalculateBounds(useClientResources: true, Matrix.Identity));

    /// <summary>
    /// Bounds with <paramref name="transform"/> applied per recorded top-level
    /// item before the union, which keeps rotated and skewed bounds tighter than
    /// transforming <see cref="Bounds"/> once.
    /// </summary>
    public Rect? GetBounds(Matrix transform)
        => ServerCompositionRenderData.ApplyRenderBoundsRounding(
            _stream.CalculateBounds(useClientResources: true, transform));

    public void Render(IDrawingContextImpl context) => _stream.Replay(context);

    public bool HitTest(Point pt) => _stream.HitTest(pt);
}
