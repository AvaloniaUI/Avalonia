using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// An immutable recorded draw list that can be replayed with minimal overhead.
/// Created via <see cref="DrawingRecording.Create(System.Action{DrawingContext})"/> for
/// immutable resources or
/// <see cref="DrawingRecording.Create(Compositor, System.Action{DrawingContext})"/> for
/// compositor-bound resources that support animations and change tracking.
/// </summary>
public sealed class DrawingRecording : IDisposable
{
    private readonly RenderDataStream? _stream;
    private readonly CompositionRenderData? _renderData;
    private readonly IReadOnlyList<DrawingRecording>? _ownedChildren;
    private Rect? _immutableBounds;
    private bool _registeredForSerialization;
    private bool _disposed;
    private EventHandler<Rect>? _boundsChanged;
    private bool _subscribedToAfterCommit;
    private Rect _lastObservedBounds;

    internal DrawingRecording(RenderDataStream stream, IReadOnlyList<DrawingRecording>? ownedChildren = null)
    {
        _stream = stream;
        _ownedChildren = ownedChildren;
    }

    internal DrawingRecording(Compositor compositor, CompositionRenderData renderData,
        IReadOnlyList<DrawingRecording>? ownedChildren = null)
    {
        Compositor = compositor;
        _renderData = renderData;
        _ownedChildren = ownedChildren;
    }

    /// <summary>
    /// Creates a new <see cref="DrawingRecording"/> with immutable resources.
    /// No compositor is required. Mutable brushes, pens and effects are snapshotted
    /// at record time (scene brushes via their current content), so later mutations
    /// do not affect the recording. Resources that cannot be snapshotted, and any
    /// reference to a compositor-bound recording (directly or through a brush's
    /// content), throw <see cref="InvalidOperationException"/> - use
    /// <see cref="Create(Compositor, System.Action{DrawingContext})"/> for such content.
    /// </summary>
    public static DrawingRecording Create(Action<DrawingContext> record)
    {
        _ = record ?? throw new ArgumentNullException(nameof(record));

        using var context = new RenderDataDrawingContext(null, buildingRecording: true);
        record(context);

        var stream = context.GetRenderStream();
        var owned = context.TakeOwnedRecordings();
        return new DrawingRecording(stream, owned);
    }

    /// <summary>
    /// Creates a new <see cref="DrawingRecording"/> bound to a compositor.
    /// Supports mutable resources (animated brushes, pens) with automatic change tracking.
    /// </summary>
    public static DrawingRecording Create(Compositor compositor, Action<DrawingContext> record)
    {
        _ = compositor ?? throw new ArgumentNullException(nameof(compositor));
        _ = record ?? throw new ArgumentNullException(nameof(record));

        using var context = new RenderDataDrawingContext(compositor, buildingRecording: true);
        record(context);

        var renderData = context.GetRenderResultsWithoutRegistration()
            ?? new CompositionRenderData(compositor);
        var owned = context.TakeOwnedRecordings();

        return new DrawingRecording(compositor, renderData, owned);
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
    /// Gets the bounds of the recorded content in the recording's local coordinate space.
    /// Available synchronously after <see cref="Create(System.Action{DrawingContext})"/> or
    /// <see cref="Create(Compositor, System.Action{DrawingContext})"/> returns - no compositor
    /// commit is required. Returns a default (empty) <see cref="Rect"/> if the recording
    /// contains no drawn content.
    /// </summary>
    public Rect Bounds
    {
        get
        {
            ThrowIfDisposed();
            if (_renderData != null)
                return _renderData.Bounds ?? default;
            // An immutable stream never changes, so the walk result is cached.
            return _immutableBounds ??=
                ServerCompositionRenderData.ApplyRenderBoundsRounding(_stream!.CalculateBounds()) ?? default;
        }
    }

    /// <summary>
    /// Gets the bounds of the recorded content after applying <paramref name="transform"/>
    /// to each drawn item. Produces tighter bounds than <see cref="Bounds"/>.TransformToAABB
    /// for rotations and skews over recordings containing multiple items, since each item's
    /// bounds are transformed individually before being unioned. Returns a default (empty)
    /// <see cref="Rect"/> if the recording contains no drawn content.
    /// </summary>
    /// <param name="transform">The outer transform to apply. When identity, equivalent to
    /// <see cref="Bounds"/>.</param>
    public Rect GetBounds(Matrix transform)
    {
        ThrowIfDisposed();
        if (transform.IsIdentity)
            return Bounds;
        if (_renderData != null)
            return _renderData.GetBounds(transform) ?? default;
        return ServerCompositionRenderData.ApplyRenderBoundsRounding(
            _stream!.CalculateBounds(useClientResources: false, transform)) ?? default;
    }

    /// <summary>
    /// Whether this recording has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// The recorded stream for immutable recordings.
    /// </summary>
    internal RenderDataStream? Stream => _disposed ? null : _stream;

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
    /// Ensures the compositor-bound render data is registered for serialization.
    /// Called lazily on first use to avoid leaking server resources for unused recordings.
    /// Registration must happen at most once: re-serializing the same render data would
    /// make the server reset its previous stream and dispose resource refs the fresh
    /// copy still shares.
    /// </summary>
    internal void EnsureRegisteredForSerialization()
    {
        if (!_registeredForSerialization && _renderData != null && Compositor != null)
        {
            Compositor.RegisterForSerialization(_renderData);
            _registeredForSerialization = true;
        }
    }

    /// <summary>
    /// Replays an immutable recording into a drawing context implementation.
    /// Backends use this to rasterize recording-sourced effects (e.g. the
    /// SVG feImage primitive).
    /// </summary>
    internal void Render(Avalonia.Platform.IDrawingContextImpl context)
    {
        ThrowIfDisposed();
        if (_stream == null)
        {
            throw new InvalidOperationException(
                "Only immutable DrawingRecordings can render outside the compositor.");
        }

        _stream.Replay(context);
    }

    /// <summary>
    /// Hit tests the recorded content against a point.
    /// </summary>
    public bool HitTest(Point point)
    {
        ThrowIfDisposed();
        if (_renderData != null)
            return _renderData.HitTest(point);
        return _stream!.HitTest(point);
    }

    /// <summary>
    /// Hit tests the recorded content against a geometry.
    /// </summary>
    public IntersectionResult HitTest(Geometry geometry)
    {
        ThrowIfDisposed();
        if (_renderData != null)
            return _renderData.HitTest(geometry);
        return _stream!.HitTest(geometry);
    }

    /// <summary>
    /// Raised on the UI thread after a compositor commit when <see cref="Bounds"/>
    /// has changed since the previous commit (or since the first subscription).
    /// Supported on compositor-bound recordings only; immutable recordings never
    /// change bounds and subscribing throws.
    ///
    /// Fires at most once per commit. The event handler receives the new bounds.
    /// </summary>
    public event EventHandler<Rect> BoundsChanged
    {
        add
        {
            ThrowIfDisposed();
            if (Compositor == null)
                throw new InvalidOperationException(
                    "BoundsChanged is only supported on compositor-bound DrawingRecordings.");
            _boundsChanged += value;
            EnsureSubscribedToAfterCommit();
        }
        remove
        {
            _boundsChanged -= value;
            if (_boundsChanged == null)
                UnsubscribeFromAfterCommit();
        }
    }

    private void EnsureSubscribedToAfterCommit()
    {
        if (_subscribedToAfterCommit || Compositor == null)
            return;
        _lastObservedBounds = Bounds;
        Compositor.AfterCommit += OnCompositorAfterCommit;
        _subscribedToAfterCommit = true;
    }

    private void UnsubscribeFromAfterCommit()
    {
        if (!_subscribedToAfterCommit || Compositor == null)
            return;
        Compositor.AfterCommit -= OnCompositorAfterCommit;
        _subscribedToAfterCommit = false;
    }

    private void OnCompositorAfterCommit()
    {
        if (_disposed)
            return;
        var current = Bounds;
        if (current != _lastObservedBounds)
        {
            _lastObservedBounds = current;
            _boundsChanged?.Invoke(this, current);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            UnsubscribeFromAfterCommit();
            _boundsChanged = null;
            _renderData?.Dispose();
            // The immutable stream is deliberately not disposed: enclosing
            // recordings and scene-brush contents reference it without a ref
            // count (the child may be disposed independently by its owner), so
            // returning its pooled buffers here could corrupt a replay that is
            // still reachable. The GC reclaims the stream instead.
            if (_ownedChildren != null)
            {
                foreach (var child in _ownedChildren)
                    child.Dispose();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DrawingRecording));
    }
}
