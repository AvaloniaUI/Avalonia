using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An immutable recorded draw list that can be replayed with minimal overhead.
/// Created via <see cref="DrawingRecording.Create"/>.
/// </summary>
public sealed class DrawingRecording : IDisposable
{
    private readonly RenderItemList _items;
    private bool _disposed;

    internal DrawingRecording(RenderItemList items)
    {
        _items = items;
    }

    /// <summary>
    /// Creates a new immutable <see cref="DrawingRecording"/> by recording drawing commands
    /// via the provided callback.
    /// </summary>
    /// <param name="record">A callback that receives a <see cref="DrawingContext"/>
    /// to record drawing commands into.</param>
    /// <returns>An immutable <see cref="DrawingRecording"/> that can be replayed.</returns>
    public static DrawingRecording Create(Action<DrawingContext> record)
    {
        _ = record ?? throw new ArgumentNullException(nameof(record));

        using var context = new RenderDataDrawingContext(null);
        record(context);

        var items = context.GetRenderItemList();
        return new DrawingRecording(items);
    }

    /// <summary>
    /// Gets the bounds of the recorded content.
    /// </summary>
    public Rect Bounds
    {
        get
        {
            ThrowIfDisposed();
            return _items.Bounds ?? default;
        }
    }

    /// <summary>
    /// Whether this recording has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// The render item list backing this recording.
    /// </summary>
    internal RenderItemList Items
    {
        get
        {
            ThrowIfDisposed();
            return _items;
        }
    }

    /// <summary>
    /// Hit tests the recorded content against a point.
    /// </summary>
    /// <param name="point">The point to test, in the recording's coordinate space.</param>
    /// <returns><c>true</c> if the point hits recorded geometry; otherwise <c>false</c>.</returns>
    public bool HitTest(Point point)
    {
        ThrowIfDisposed();
        return _items.HitTest(point);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DrawingRecording));
    }
}
