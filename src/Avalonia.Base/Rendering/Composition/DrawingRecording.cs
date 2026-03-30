using System;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An immutable, compositor-integrated recorded draw list that can be replayed
/// with minimal overhead. Created via <see cref="Compositor.CreateDrawingRecording"/>.
/// </summary>
public sealed class DrawingRecording : IDisposable
{
    private readonly CompositionRenderData _renderData;
    private bool _disposed;

    internal DrawingRecording(Compositor compositor, CompositionRenderData renderData)
    {
        Compositor = compositor;
        _renderData = renderData;
    }

    /// <summary>
    /// The compositor this recording belongs to.
    /// </summary>
    public Compositor Compositor { get; }

    /// <summary>
    /// Gets the bounds of the recorded content.
    /// Bounds are computed lazily on the server thread after the next compositor commit.
    /// Returns <c>default</c> if no content was recorded or if the commit has not yet occurred.
    /// </summary>
    public Rect Bounds
    {
        get
        {
            ThrowIfDisposed();
            return _renderData.Server.Bounds?.ToRect() ?? default;
        }
    }

    /// <summary>
    /// Whether this recording has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// The server-side render data for this recording.
    /// </summary>
    internal ServerCompositionRenderData ServerRenderData
    {
        get
        {
            ThrowIfDisposed();
            return _renderData.Server;
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
        return _renderData.HitTest(point);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _renderData.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DrawingRecording));
    }
}
