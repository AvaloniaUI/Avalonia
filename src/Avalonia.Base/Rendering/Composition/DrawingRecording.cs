using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An immutable recorded draw list that can be replayed with minimal overhead.
/// Created via <see cref="DrawingRecording.Create(System.Action{DrawingContext})"/> for immutable resources
/// or <see cref="DrawingRecording.Create(Compositor, System.Action{DrawingContext})"/> for compositor-bound
/// resources that support animations and change tracking.
/// </summary>
public sealed class DrawingRecording : IDisposable
{
    private readonly RenderItemList? _items;
    private readonly CompositionRenderData? _renderData;
    private bool _disposed;

    internal DrawingRecording(RenderItemList items)
    {
        _items = items;
    }

    internal DrawingRecording(Compositor compositor, CompositionRenderData renderData)
    {
        Compositor = compositor;
        _renderData = renderData;
    }

    /// <summary>
    /// Creates a new <see cref="DrawingRecording"/> with immutable resources.
    /// No compositor is required. Only immutable brushes and pens are supported.
    /// </summary>
    public static DrawingRecording Create(Action<DrawingContext> record)
    {
        _ = record ?? throw new ArgumentNullException(nameof(record));

        using var context = new RenderDataDrawingContext(null);
        record(context);

        var items = context.GetRenderItemList();
        return new DrawingRecording(items);
    }

    /// <summary>
    /// Creates a new <see cref="DrawingRecording"/> bound to a compositor.
    /// Supports mutable resources (animated brushes, pens) with automatic change tracking.
    /// </summary>
    public static DrawingRecording Create(Compositor compositor, Action<DrawingContext> record)
    {
        _ = compositor ?? throw new ArgumentNullException(nameof(compositor));
        _ = record ?? throw new ArgumentNullException(nameof(record));

        using var context = new RenderDataDrawingContext(compositor);
        record(context);

        var renderData = context.GetRenderResults()
            ?? new CompositionRenderData(compositor);

        return new DrawingRecording(compositor, renderData);
    }

    /// <summary>
    /// The compositor this recording is bound to, or null for immutable recordings.
    /// </summary>
    public Compositor? Compositor { get; }

    /// <summary>
    /// Whether this recording is bound to a compositor and supports mutable resources.
    /// </summary>
    internal bool IsCompositorBound => _renderData != null;

    /// <summary>
    /// Gets the bounds of the recorded content.
    /// For compositor-bound recordings, bounds are available after a compositor commit.
    /// </summary>
    public Rect Bounds
    {
        get
        {
            ThrowIfDisposed();
            if (_renderData != null)
                return _renderData.Bounds ?? default;
            return _items!.Bounds ?? default;
        }
    }

    /// <summary>
    /// Whether this recording has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// The render item list for immutable recordings.
    /// </summary>
    internal RenderItemList? ItemList => _disposed ? null : _items;

    /// <summary>
    /// The composition render data for compositor-bound recordings.
    /// </summary>
    internal CompositionRenderData? RenderData => _disposed ? null : _renderData;

    /// <summary>
    /// The server-side render data for compositor-bound recordings.
    /// </summary>
    internal ServerCompositionRenderData? ServerRenderData
    {
        get
        {
            ThrowIfDisposed();
            return _renderData?.Server;
        }
    }

    /// <summary>
    /// Hit tests the recorded content against a point.
    /// </summary>
    public bool HitTest(Point point)
    {
        ThrowIfDisposed();
        if (_renderData != null)
            return _renderData.HitTest(point);
        return _items!.HitTest(point);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _renderData?.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DrawingRecording));
    }
}
