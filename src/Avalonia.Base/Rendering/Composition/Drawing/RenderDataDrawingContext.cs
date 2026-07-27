using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class RenderDataDrawingContext : DrawingContext
{
    private readonly Compositor? _compositor;
    private readonly bool _buildingRecording;
    private RenderDataStream? _stream;
    private CompositionRenderData? _renderData;
    private HashSet<object>? _resourcesHashSet;
    private Stack<PushEntry>? _pushStack;
    private List<DrawingRecording>? _ownedRecordings;
    private HashSet<DrawingRecording>? _ownedRecordingsDedup;
    private bool _containsCompositorResources;
    private bool _containsMutableResources;
    private static readonly ThreadSafeObjectPool<HashSet<object>> s_hashSetPool = new();
    private static readonly ThreadSafeObjectPool<Stack<PushEntry>> s_pushStackPool = new();

    private struct PushEntry
    {
        public bool Emitted;
        public int PositionBefore;
        public int PositionAfter;
        public int DepthBefore;
        public bool KeepWhenEmpty;
    }

    /// <param name="compositor">The compositor whose server resources the recorded
    /// content binds to, or null for content that only uses immutable resources.</param>
    /// <param name="buildingRecording">True when this context builds a long-lived
    /// <see cref="DrawingRecording"/>. Recording-building contexts honor
    /// <see cref="DrawingRecordingOwnership.Owned"/> registrations, and - when
    /// <paramref name="compositor"/> is null - enforce that captured resources are
    /// (or can be snapshotted to) immutable so the recording can be replayed on the
    /// render thread. Transient contexts (the visual-content recorder, immediate
    /// scene-brush content) capture resources as-is.</param>
    public RenderDataDrawingContext(Compositor? compositor, bool buildingRecording = false)
    {
        _compositor = compositor;
        _buildingRecording = buildingRecording;
    }

    private RenderDataStream Stream => _stream ??= new RenderDataStream();

    private CompositionRenderData RenderData => _renderData ??= new CompositionRenderData(_compositor!, Stream);

    private void AddResource(object? resource)
    {
        if (_compositor == null)
            return;

        if (resource == null
            || resource is IImmutableBrush
            || resource is ImmutablePen
            || resource is ImmutableTransform
            || resource is CompositionBrush)
            return;

        if (resource is ICompositionRenderResource renderResource)
        {
            _resourcesHashSet ??= s_hashSetPool.Get();
            if (!_resourcesHashSet.Add(renderResource))
                return;

            renderResource.AddRefOnCompositor(_compositor);
            RenderData.AddResource(renderResource);
            return;
        }

        throw new InvalidOperationException(resource.GetType().FullName + " can not be used with this DrawingContext");
    }

    /// <summary>
    /// Captures a brush for the recorded stream. Compositor-bound contexts register the
    /// brush as a composition resource and store its server-side counterpart. Contexts
    /// building an immutable <see cref="DrawingRecording"/> snapshot the brush (see
    /// <see cref="SnapshotBrush"/>) so the recording stays valid and thread-safe when
    /// replayed on the render thread. Transient contexts capture the brush as-is and
    /// only track whether non-immutable resources were seen.
    /// </summary>
    private IBrush? CaptureBrush(IBrush? brush)
    {
        if (brush == null)
            return null;

        if (_compositor != null)
        {
            AddResource(brush);
            return brush.GetServer(_compositor);
        }

        if (!_buildingRecording)
        {
            if (brush is not IImmutableBrush)
                _containsMutableResources = true;
            return brush;
        }

        return SnapshotBrush(brush);
    }

    /// <summary>
    /// Captures a pen, returning the client-side instance (used for synchronous bounds
    /// and hit-test queries) and the server-side instance (used at replay). For
    /// immutable recordings both are the same immutable snapshot.
    /// </summary>
    private (IPen? Client, IPen? Server) CapturePen(IPen? pen)
    {
        if (pen == null)
            return (null, null);

        if (_compositor != null)
        {
            AddResource(pen);
            return (pen, pen.GetServer(_compositor));
        }

        if (!_buildingRecording)
        {
            if (pen is not ImmutablePen)
                _containsMutableResources = true;
            return (pen, pen);
        }

        var snapshot = SnapshotPen(pen);
        return (snapshot, snapshot);
    }

    /// <summary>
    /// Snapshots a brush for capture into an immutable <see cref="DrawingRecording"/>.
    /// Immutable brushes pass through; everything else converts via
    /// <see cref="BrushExtensions.ToImmutable(IBrush)"/> (mutable brushes are cloned;
    /// scene brushes are resolved to their current content), so no
    /// <see cref="AvaloniaObject"/> is touched at replay time. Throws for brushes
    /// that cannot be made immutable and for scene-brush content that references
    /// compositor-bound or mutable resources.
    /// </summary>
    private static IImmutableBrush SnapshotBrush(IBrush brush)
    {
        switch (brush)
        {
            case IImmutableBrush immutable:
                return immutable;
            case ISceneBrush:
            case IMutableBrush:
            {
                var snapshot = brush.ToImmutable();
                ThrowIfRestrictedSceneContent(snapshot, brush);
                return snapshot;
            }
            case CompositionBrush:
                throw new InvalidOperationException(
                    brush.GetType() + " is compositor-bound and cannot be captured by an immutable " +
                    "DrawingRecording. Use DrawingRecording.Create(Compositor, Action<DrawingContext>) instead.");
            default:
                throw new InvalidOperationException(
                    brush.GetType() + " cannot be captured by an immutable DrawingRecording. Use an immutable brush.");
        }
    }

    /// <summary>
    /// Snapshots a pen for capture into an immutable <see cref="DrawingRecording"/>
    /// via <see cref="BrushExtensions.ToImmutable(IPen)"/>.
    /// </summary>
    private static IPen SnapshotPen(IPen pen)
    {
        switch (pen)
        {
            case ImmutablePen immutable:
                return immutable;
            case Pen:
            {
                var snapshot = pen.ToImmutable();
                ThrowIfRestrictedSceneContent(snapshot.Brush, pen);
                return snapshot;
            }
            default:
                throw new InvalidOperationException(
                    pen.GetType() + " cannot be captured by an immutable DrawingRecording. Use ImmutablePen.");
        }
    }

    /// <summary>
    /// Rejects scene-brush content snapshots that an immutable recording must not
    /// embed: content referencing compositor-bound render data (could be neither
    /// retained nor tracked) or live mutable resources (unsafe to read at replay
    /// time on the render thread).
    /// </summary>
    private static void ThrowIfRestrictedSceneContent(IBrush? snapshot, object source)
    {
        if (snapshot is EmbeddedSceneBrushContent { ContainsCompositorResources: true })
            throw new InvalidOperationException(
                source.GetType().Name + " content references compositor-bound resources and cannot be " +
                "captured by an immutable DrawingRecording. Use DrawingRecording.Create(Compositor, ...) instead.");
        if (snapshot is EmbeddedSceneBrushContent { ContainsMutableResources: true })
            throw new InvalidOperationException(
                source.GetType().Name + " content references mutable resources and cannot be captured " +
                "by an immutable DrawingRecording. Use immutable brushes and pens inside the brush content.");
    }

    private void PushedScope(int positionBefore, bool keepWhenEmpty = false) =>
        (_pushStack ??= s_pushStackPool.Get()).Push(new PushEntry
        {
            Emitted = true,
            PositionBefore = positionBefore,
            PositionAfter = Stream.OpcodeLength,
            DepthBefore = Stream.Depth - 1,
            KeepWhenEmpty = keepWhenEmpty
        });

    private void PushedNoOpScope() =>
        (_pushStack ??= s_pushStackPool.Get()).Push(new PushEntry { Emitted = false });

    private void PopCore()
    {
        var entry = _pushStack!.Pop();
        if (!entry.Emitted)
            return;

        // Empty push/pop pairs are erased from the stream - except scopes that
        // produce output even without children (e.g. an effect layer painting
        // from a source-generating filter), which set KeepWhenEmpty.
        if (!entry.KeepWhenEmpty && Stream.OpcodeLength == entry.PositionAfter)
            Stream.Rewind(entry.PositionBefore, entry.DepthBefore);
        else
            Stream.Pop();
    }

    protected override void DrawLineCore(IPen? pen, Point p1, Point p2)
    {
        if (pen == null)
            return;
        var (clientPen, serverPen) = CapturePen(pen);
        Stream.DrawLine(serverPen, clientPen, p1, p2);
    }

    protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
    {
        if (brush == null && pen == null)
            return;
        var serverBrush = CaptureBrush(brush);
        var (clientPen, serverPen) = CapturePen(pen);
        Stream.DrawGeometry(serverBrush, serverPen, clientPen, geometry);
    }

    protected override void DrawGeometryCore(IBrush? brush, IPen? pen, Geometry geometry)
    {
        if (brush is null && pen is null)
            return;

        var serverBrush = CaptureBrush(brush);
        var (clientPen, serverPen) = CapturePen(pen);
        AddResource(geometry);

        // Null-compositor contexts resolve the geometry's current platform impl,
        // which is an immutable snapshot; later geometry mutations don't reach
        // the recorded content.
        Stream.DrawGeometry(serverBrush, serverPen, clientPen, geometry.GetServer(_compositor));
    }

    protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default)
    {
        if (rrect.IsEmpty())
            return;
        if (brush == null && pen == null && boxShadows == default)
            return;
        var serverBrush = CaptureBrush(brush);
        var (clientPen, serverPen) = CapturePen(pen);
        Stream.DrawRectangle(serverBrush, serverPen, clientPen, rrect, boxShadows);
    }

    protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
    {
        if (rect.IsEmpty())
            return;
        if (brush == null && pen == null)
            return;
        var serverBrush = CaptureBrush(brush);
        var (clientPen, serverPen) = CapturePen(pen);
        Stream.DrawEllipse(serverBrush, serverPen, clientPen, rect);
    }

    public override void Custom(ICustomDrawOperation custom) => Stream.DrawCustom(custom);

    public override void DrawGlyphRun(IBrush? foreground, GlyphRun? glyphRun)
    {
        if (foreground == null || glyphRun == null)
            return;
        Stream.DrawGlyphRun(CaptureBrush(foreground), glyphRun.PlatformImpl.Clone());
    }

    internal override void DrawBitmap(IRef<IBitmapImpl>? source, double opacity, Rect sourceRect, Rect destRect)
    {
        if (source == null || sourceRect.IsEmpty() || destRect.IsEmpty())
            return;
        Stream.DrawBitmap(source.Clone(), opacity, sourceRect, destRect);
    }

    internal override void RegisterOwnedRecording(DrawingRecording recording)
    {
        // Ownership is honored only when this context builds a DrawingRecording
        // (per the DrawingRecordingOwnership contract). The shared visual-content
        // recorder and transient scene-brush contents leave disposal to the caller.
        if (!_buildingRecording)
            return;
        _ownedRecordingsDedup ??= new();
        if (!_ownedRecordingsDedup.Add(recording))
            return;
        (_ownedRecordings ??= new()).Add(recording);
    }

    /// <summary>
    /// Returns (and clears) the list of <see cref="DrawingRecordingOwnership.Owned"/>
    /// child recordings registered during this context's lifetime, for transfer to
    /// the resulting <see cref="DrawingRecording"/>.
    /// </summary>
    public IReadOnlyList<DrawingRecording>? TakeOwnedRecordings()
    {
        var list = _ownedRecordings;
        _ownedRecordings = null;
        _ownedRecordingsDedup?.Clear();
        _ownedRecordingsDedup = null;
        return list;
    }

    internal override void DrawRecordingCore(DrawingRecording recording) =>
        DrawRecordingCore(recording, Matrix.Identity);

    internal override void DrawRecordingCore(DrawingRecording recording, Matrix transform)
    {
        if (recording.IsCompositorBound)
        {
            if (_compositor != null && recording.Compositor != _compositor)
                throw new InvalidOperationException(
                    "Cannot draw a compositor-bound DrawingRecording into a context belonging to a different compositor.");

            if (_compositor == null)
            {
                if (_buildingRecording)
                    throw new InvalidOperationException(
                        "An immutable DrawingRecording cannot reference a compositor-bound DrawingRecording: " +
                        "it would neither retain nor track the compositor-bound content. " +
                        "Use DrawingRecording.Create(Compositor, ...) for the enclosing recording instead.");
                _containsCompositorResources = true;
            }

            recording.EnsureRegisteredForSerialization();
            var renderData = recording.RenderData!;
            AddResource(new CompositionRenderDataResourceRef(renderData));
            Stream.DrawRecording(renderData.Server, renderData, transform);
        }
        else
        {
            Stream.DrawRecording(recording.Stream!, transform);
        }
    }

    protected override void PushClipCore(RoundedRect rect)
    {
        var before = Stream.OpcodeLength;
        Stream.PushClip(rect);
        PushedScope(before);
    }

    protected override void PushClipCore(Rect rect)
    {
        var before = Stream.OpcodeLength;
        Stream.PushClip(new RoundedRect(rect));
        PushedScope(before);
    }

    protected override void PushGeometryClipCore(Geometry? clip)
    {
        if (clip == null)
        {
            PushedNoOpScope();
            return;
        }

        AddResource(clip);
        var before = Stream.OpcodeLength;
        Stream.PushGeometryClip(clip.GetServer(_compositor));
        PushedScope(before);
    }

    protected override void PushOpacityCore(double opacity)
    {
        if (opacity == 1)
        {
            PushedNoOpScope();
            return;
        }

        var before = Stream.OpcodeLength;
        Stream.PushOpacity(opacity);
        PushedScope(before);
    }

    protected override void PushOpacityMaskCore(IBrush? mask, Rect bounds)
    {
        if (mask == null)
        {
            PushedNoOpScope();
            return;
        }

        var before = Stream.OpcodeLength;
        Stream.PushOpacityMask(CaptureBrush(mask), bounds);
        PushedScope(before);
    }

    protected override void PushTransformCore(Matrix matrix)
    {
        if (matrix.IsIdentity)
        {
            PushedNoOpScope();
            return;
        }

        var before = Stream.OpcodeLength;
        Stream.PushTransform(matrix);
        PushedScope(before);
    }

    protected override void PushRenderOptionsCore(RenderOptions renderOptions)
    {
        var before = Stream.OpcodeLength;
        Stream.PushRenderOptions(renderOptions);
        PushedScope(before);
    }

    protected override void PushTextOptionsCore(TextOptions textOptions)
    {
        var before = Stream.OpcodeLength;
        Stream.PushTextOptions(textOptions);
        PushedScope(before);
    }

    protected override void PushEffectCore(IEffect effect, Rect bounds)
    {
        var before = Stream.OpcodeLength;
        Stream.PushEffect(effect.ToImmutable(), bounds.Inflate(effect.GetEffectOutputPadding()));
        PushedScope(before);
    }

    protected override void PopClipCore() => PopCore();

    protected override void PopGeometryClipCore() => PopCore();

    protected override void PopOpacityCore() => PopCore();

    protected override void PopOpacityMaskCore() => PopCore();

    protected override void PopTransformCore() => PopCore();

    protected override void PopRenderOptionsCore() => PopCore();

    protected override void PopTextOptionsCore() => PopCore();

    protected override void PopEffectCore() => PopCore();

    private void FlushStack()
    {
        while (_pushStack is { Count: > 0 })
            PopCore();
    }

    public CompositionRenderData? GetRenderResults()
    {
        var rv = GetRenderResultsCore();
        if (rv != null)
            _compositor!.RegisterForSerialization(rv);
        return rv;
    }

    /// <summary>
    /// Finalizes the recorded content without registering it for serialization.
    /// Used by compositor-bound <see cref="DrawingRecording"/>s, which register
    /// lazily on first use so unused recordings never allocate server resources.
    /// </summary>
    internal CompositionRenderData? GetRenderResultsWithoutRegistration()
    {
        return GetRenderResultsCore();
    }

    private CompositionRenderData? GetRenderResultsCore()
    {
        Debug.Assert(_compositor != null);
        FlushStack();

        var rv = _renderData;
        if (rv == null)
        {
            if (_stream is { OpcodeLength: > 0 })
                rv = new CompositionRenderData(_compositor!, _stream);
            else
            {
                _stream?.Dispose();
                _stream = null;
                return null;
            }
        }

        _renderData = null;
        _stream = null;
        _resourcesHashSet?.Clear();

        return rv;
    }

    /// <summary>
    /// Transfers the recorded stream out of the context for an immutable
    /// <see cref="DrawingRecording"/>. Always returns a stream, possibly empty,
    /// so recordings have a non-null replayable payload.
    /// </summary>
    public RenderDataStream GetRenderStream()
    {
        Debug.Assert(_compositor == null);
        Debug.Assert(_renderData == null);
        FlushStack();

        var stream = _stream ?? new RenderDataStream();
        _stream = null;
        return stream;
    }

    public ImmediateRenderDataSceneBrushContent? GetImmediateSceneBrushContent(ITileBrush brush, Rect? rect, bool useScalableRasterization)
    {
        Debug.Assert(_compositor == null);
        Debug.Assert(_renderData == null);
        FlushStack();

        if (_stream is not { OpcodeLength: > 0 })
        {
            _stream?.Dispose();
            _stream = null;
            return null;
        }

        var stream = _stream;
        _stream = null;
        return new ImmediateRenderDataSceneBrushContent(brush, stream, rect, useScalableRasterization,
            _containsCompositorResources, _containsMutableResources);
    }

    public void Reset()
    {
        if (_renderData != null)
        {
            _renderData.Dispose();
            _renderData = null;
        }
        else
            _stream?.Dispose();

        // Ownership of these children was transferred by DrawRecording(..., Owned).
        // If they are still here the discarded work never reached a DrawingRecording
        // (e.g. the record delegate threw), so disposing them is this context's job.
        if (_ownedRecordings != null)
        {
            foreach (var owned in _ownedRecordings)
                owned.Dispose();
            _ownedRecordings = null;
        }
        _ownedRecordingsDedup?.Clear();

        _stream = null;
        _pushStack?.Clear();
        _resourcesHashSet?.Clear();
        _containsCompositorResources = false;
        _containsMutableResources = false;
    }

    protected override void DisposeCore()
    {
        Reset();
        if (_resourcesHashSet != null)
            s_hashSetPool.ReturnAndSetNull(ref _resourcesHashSet);
        if (_pushStack != null)
            s_pushStackPool.ReturnAndSetNull(ref _pushStack);
    }
}
