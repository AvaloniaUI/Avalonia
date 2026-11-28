using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    [StructLayout(LayoutKind.Auto)]
    partial struct RenderContext : IServerTreeVisitor, IDisposable
    {
        private readonly IDrawingContextImpl _canvas;
        private readonly IDrawingContextImplWithBackdropSupport? _backdropCanvas;
        private readonly IDirtyRectTracker? _dirtyRects;
        private readonly CompositorPools _pools;
        private readonly bool _renderChildren;
        private TreeWalkContext _walkContext;
        private Stack<double> _opacityStack;
        private double _opacity;
        private bool _fullSkip;
        private bool _usedCache;
        public int RenderedVisuals;
        public int VisitedVisuals;
        private ServerVisualRenderContext _publicContext;
        private readonly ServerCompositionVisual _rootVisual;
        private bool _skipNextVisualTransform;
        private bool _renderingToBitmapCache;
        // The canvas pushes that are currently open, in push order. Used to rewind the canvas to an open
        // intermediate layer and replay its pushes around a backdrop visual's retained draw.
        private List<CanvasJournalEntry>? _canvasJournal;

        public RenderContext(ServerCompositionVisual rootVisual, IDrawingContextImpl canvas,
            IDirtyRectTracker? dirtyRects, CompositorPools pools, Matrix matrix, LtrbRect clip,
            bool renderChildren, bool skipRootVisualTransform, bool renderingToBitmapCache)
        {
            _publicContext = new ServerVisualRenderContext(canvas);

            if (dirtyRects != null)
            {
                var dirtyClip = dirtyRects.CombinedRect;
                if (dirtyRects is SingleDirtyRectTracker)
                    dirtyRects = null;
                clip = clip.IntersectOrEmpty(dirtyClip);
            }

            _canvas = canvas;
            _backdropCanvas = canvas as IDrawingContextImplWithBackdropSupport;
            _dirtyRects = dirtyRects;
            _pools = pools;
            _renderChildren = renderChildren;

            _rootVisual = rootVisual;

            _walkContext = new TreeWalkContext(pools, matrix, clip);

            _opacity = 1;
            _opacityStack = pools.DoubleStackPool.Rent();
            _skipNextVisualTransform = skipRootVisualTransform;
            _renderingToBitmapCache = renderingToBitmapCache;
            _canvasJournal = pools.CanvasJournalEntryListPool.Rent();
        }


        private bool HandlePreGraphTransformClipOpacity(ServerCompositionVisual visual)
        {
            if (!visual.Visible || visual._transformedSubTreeBounds == null)
                return false;
            var effectiveOpacity = visual.Opacity * _opacity;
            if (effectiveOpacity <= 0.003)
                return false;
            
            ref var effectiveNewTransform = ref _walkContext.Transform;
            Matrix transformToPush;
            if (visual._ownTransform.HasValue)
            {
                if (!_skipNextVisualTransform)
                {
                    transformToPush = visual._ownTransform.Value * _walkContext.Transform;
                    effectiveNewTransform = ref transformToPush;
                }
            }

            _skipNextVisualTransform = false;
            
            var effectiveClip = _walkContext.Clip;
            if (visual._ownClipRect != null) 
                effectiveClip = effectiveClip.IntersectOrEmpty(visual._ownClipRect.Value.TransformToAABB(effectiveNewTransform));

            var worldBounds = visual._transformedSubTreeBounds.Value.TransformToAABB(_walkContext.Transform);
            if (!effectiveClip.Intersects(worldBounds) 
                || _dirtyRects?.Intersects(worldBounds) == false)
                return false;
            
            
            RenderedVisuals++;
            
            // We are still in parent's coordinate space here

            
            if (visual.Opacity != 1)
            {
                _opacityStack.Push(_opacity);
                _opacity = effectiveOpacity;
                var journalMatrix = _canvas.Transform;
                var journalDepth = CanvasLayerDepth;
                _canvas.PushOpacity(visual.Opacity, visual._transformedSubTreeBounds.Value.ToRect());
                JournalPush(CanvasJournalKind.Opacity, visual, journalMatrix, journalDepth);
            }

            // Switch coordinate space to this visual's space

            if (visual._ownTransform.HasValue)
            {
                _walkContext.PushSetTransform(effectiveNewTransform); // Reuse one computed before
                _canvas.Transform = effectiveNewTransform;
            }

            if (visual._ownClipRect.HasValue)
                _walkContext.PushClip(effectiveClip);

            if (visual.ClipToBounds)
            {
                var journalMatrix = _canvas.Transform;
                var journalDepth = CanvasLayerDepth;
                visual.PushClipToBounds(_canvas);
                JournalPush(CanvasJournalKind.ClipToBounds, visual, journalMatrix, journalDepth);
            }

            if (visual.Clip != null)
            {
                var journalMatrix = _canvas.Transform;
                var journalDepth = CanvasLayerDepth;
                _canvas.PushGeometryClip(visual.Clip);
                JournalPush(CanvasJournalKind.GeometryClip, visual, journalMatrix, journalDepth);
            }

            return true;
        }

        public void PreSubgraph(ServerCompositionVisual visual, out bool visitChildren)
        {
            VisitedVisuals++;
            var bitmapCacheRoot = _renderingToBitmapCache && visual == _rootVisual;

            if (!bitmapCacheRoot) // Skip those for the root visual if we are rendering to bitmap cache
            {
                // Push transform, clip, opacity and check if those make the visual effectively invisible
                if (!HandlePreGraphTransformClipOpacity(visual))
                {
                    _fullSkip = true;
                    visitChildren = false;
                    return;
                }

                // Push adorner clip
                if (visual.AdornedVisual != null)
                    AdornerHelper_RenderPreGraphPushAdornerClip(visual);

                // Before the visual's own content and children. Not reached for the root of a bitmap cache
                // render: its backdrop effect belongs to the outer layer and the outer render walk drew it.
                if (visual.BackdropEffect != null)
                    DrawBackdropEffect(visual);

                // If caching is enabled, draw from cache and skip rendering
                if (visual.Cache != null)
                {
                    var (visited, rendered) = visual.Cache.Draw(_canvas);
                    VisitedVisuals += visited;
                    RenderedVisuals += rendered;
                    _usedCache = true;
                    visitChildren = false;
                    return;
                }
            }

            if(visual.RenderOptions != default)
            {
                var journalMatrix = _canvas.Transform;
                var journalDepth = CanvasLayerDepth;
                _canvas.PushRenderOptions(visual.RenderOptions);
                JournalPush(CanvasJournalKind.RenderOptions, visual, journalMatrix, journalDepth);
            }

            if (visual.TextOptions != default)
                _canvas.PushTextOptions(visual.TextOptions);

            if (visual.OpacityMaskBrush != null)
            {
                var journalMatrix = _canvas.Transform;
                var journalDepth = CanvasLayerDepth;
                _canvas.PushOpacityMask(visual.OpacityMaskBrush, visual._subTreeBounds!.Value.ToRect());
                JournalPush(CanvasJournalKind.OpacityMask, visual, journalMatrix, journalDepth);
            }

            if (visual.Effect != null && _canvas is IDrawingContextImplWithEffects effects)
            {
                var journalMatrix = _canvas.Transform;
                var journalDepth = CanvasLayerDepth;
                effects.PushEffect(visual._subTreeBounds!.Value.ToRect(), visual.Effect);
                JournalPush(CanvasJournalKind.Effect, visual, journalMatrix, journalDepth);
            }

            visual.RenderCore(_publicContext, _walkContext.Clip);
            
            visitChildren = _renderChildren;
        }

        // Layer space to layer pixels.
        private static LtrbPixelRect ToLayerPixels(LtrbRect hostRect, DirtyRectSpaceMapping mapping) =>
            LtrbPixelRect.FromRectUnscaled(mapping.IsIdentity ? hostRect : mapping.LayerSpaceToLayerPixels(hostRect));

        private int CanvasLayerDepth => _backdropCanvas?.IntermediateLayerDepth ?? 0;

        // Records a canvas push in the journal. `matrix` and `depthBefore` are the canvas transform and the
        // open intermediate layer count before the push.
        private void JournalPush(CanvasJournalKind kind, ServerCompositionVisual visual, Matrix matrix, int depthBefore)
        {
            _canvasJournal!.Add(new CanvasJournalEntry(kind, visual, matrix,
                CanvasLayerDepth > depthBefore));
        }

        // Removes the newest journal entry after the walk popped the matching canvas push.
        private void EndCanvasJournalEntry(CanvasJournalKind kind)
        {
            var journal = _canvasJournal!;
            if (journal.Count > 0)
            {
                Debug.Assert(journal[journal.Count - 1].Kind == kind,
                    "The canvas push/pop journal does not match the walk's push/pop sequence.");
                journal.RemoveAt(journal.Count - 1);
            }
        }

        // Pops the canvas push of a journal entry, without touching the journal. Used to rewind the canvas.
        private void PopCanvasJournalEntry(CanvasJournalEntry entry)
        {
            switch (entry.Kind)
            {
                case CanvasJournalKind.Opacity:
                    _canvas.PopOpacity();
                    break;
                case CanvasJournalKind.ClipToBounds:
                    _canvas.PopClip();
                    break;
                case CanvasJournalKind.GeometryClip:
                    _canvas.PopGeometryClip();
                    break;
                case CanvasJournalKind.OpacityMask:
                    _canvas.PopOpacityMask();
                    break;
                case CanvasJournalKind.Effect:
                    if (_canvas is IDrawingContextImplWithEffects effects)
                        effects.PopEffect();
                    break;
                case CanvasJournalKind.RenderOptions:
                    _canvas.PopRenderOptions();
                    break;
                default:
                    Debug.Assert(false, "Adorner clips and unsupported pushes cannot be rewound.");
                    break;
            }
        }

        // Re-pushes the journal entries from `from` to the top, each at its recorded transform. The pushes are
        // the same expressions the walk issued originally, with the visual's frame-constant server state.
        private void ReplayCanvasJournal(int from)
        {
            var journal = _canvasJournal!;
            for (var i = from; i < journal.Count; i++)
            {
                var entry = journal[i];
                var visual = entry.Visual;
                _canvas.Transform = entry.Matrix;
                switch (entry.Kind)
                {
                    case CanvasJournalKind.Opacity:
                        _canvas.PushOpacity(visual.Opacity, visual._transformedSubTreeBounds!.Value.ToRect());
                        break;
                    case CanvasJournalKind.ClipToBounds:
                        visual.PushClipToBounds(_canvas);
                        break;
                    case CanvasJournalKind.GeometryClip:
                        _canvas.PushGeometryClip(visual.Clip!);
                        break;
                    case CanvasJournalKind.OpacityMask:
                        _canvas.PushOpacityMask(visual.OpacityMaskBrush!, visual._subTreeBounds!.Value.ToRect());
                        break;
                    case CanvasJournalKind.Effect:
                        if (visual.Effect != null && _canvas is IDrawingContextImplWithEffects effects)
                            effects.PushEffect(visual._subTreeBounds!.Value.ToRect(), visual.Effect);
                        break;
                    case CanvasJournalKind.RenderOptions:
                        _canvas.PushRenderOptions(visual.RenderOptions);
                        break;
                    default:
                        Debug.Assert(false, "Adorner clips and unsupported pushes cannot be replayed.");
                        break;
                }
            }
        }

        // Draws the backdrop effect of a backdrop visual.
        private void DrawBackdropEffect(ServerCompositionVisual visual)
        {
            var state = visual.BackdropState;
            if (state?.Aabb is not { } aabbPixels)
                return;

            // A layer without readable pixels (PDF, picture recording) draws no backdrop effects.
            if (_canvas is not IDrawingContextImplWithBackdropSupport { SupportsBackdrop: true } backdropCanvas)
                return;

            var mapping = state.Layer?.GetDirtyRectSpaceMapping() ?? DirtyRectSpaceMapping.Identity;
            if (!mapping.IsUsable)
                return;

            var aabbInLayer = aabbPixels.ToLtrbRectUnscaled();
            var aabbInLayerPixels = ToLayerPixels(aabbInLayer, mapping);

            // The backdrop bounds. They are in the visual's local space, which is the current coordinate
            // space of the canvas, so the clip follows the visual's transform.
            var localBounds = visual.BackdropEffectBoundsSource == Media.BackdropEffectBoundsSource.Layout
                ? new LtrbRect(0, 0, visual.Size.X, visual.Size.Y)
                : visual._subTreeBounds;
            if (localBounds is not { IsZeroSize: false } boundsRect)
                return;

            // The layer has no pixels outside of its surface. Limit the bounds to the layer's area, so that the
            // effect sees the edge of the layer as it sees the edge of the bounds. Under a rotation this is the
            // bounding box of the layer's area in local space, which can be larger than the area itself.
            var layerSize = state.Layer?.LayerPixelSize ?? visual.Root?.PixelSize ?? default;
            if (!_canvas.Transform.TryInvert(out var layerPixelsToLocal))
                return;
            var layerAreaInLocal = new LtrbRect(0, 0, layerSize.Width, layerSize.Height)
                .TransformToAABB(layerPixelsToLocal);
            if (boundsRect.IntersectOrNull(layerAreaInLocal) is not { IsZeroSize: false } visibleBounds)
                return;
            var bounds = visibleBounds.ToRect();

            if (!backdropCanvas.IsBackdropSamplingSafe)
            {
                // An intermediate layer with content is open, so the surface does not hold the backdrop yet
                // and the retained path cannot read it. Rewind the canvas to that layer and replay its pushes
                // as a fresh empty segment: the layer's content is composited into the surface, so the
                // retained path can refresh the snapshot from it, and the effect draws into the segment that
                // the layer's mask/effect/opacity applies to when it closes. The pushes are replayed again for
                // the rest of the subtree afterwards. Applying the mask per segment composes to the same
                // pixels as applying it once, because alpha masks are linear.
                if (state.IsRetained &&
                    TryRewindAndDrawRetainedBackdrop(visual, backdropCanvas, state, aabbPixels, mapping, bounds, layerSize))
                    return;

                // Rewinding is not possible (several open layers, pushes outside of this walk such as the
                // save-layer root clip, unsupported pushes above the layer). Fall back to the volatile draw,
                // which the backend reads through open intermediate layers, so the effect keeps rendering
                // instead of disappearing. The snapshot and its input dirty rect are kept: the walks keep
                // accumulating the input dirty rect while the fallback is active, so the next safe draw
                // catches the snapshot up on everything that changed in between. For a rotated visual that is
                // partly outside of the layer this drops the retained path's repeated edge pixels (see it in DrawVolatileBackdropEffect).
                backdropCanvas.DrawVolatileBackdropEffect(bounds, visual.BackdropEffect!);
                return;
            }

            if (state.IsRetained)
                DrawRetainedBackdrop(visual, backdropCanvas, state, aabbInLayer, aabbInLayerPixels, mapping, bounds, layerSize);
            else
            {
                // The backend reads the backdrop itself, also through open intermediate layers.
                backdropCanvas.DrawVolatileBackdropEffect(bounds, visual.BackdropEffect!);
            }
        }

        // Refreshes the retained backdrop snapshot from its input dirty rect and draws it through the effect.
        private void DrawRetainedBackdrop(ServerCompositionVisual visual, IDrawingContextImplWithBackdropSupport backdropCanvas,
            ServerCompositionVisualBackdropState state, LtrbRect aabbInLayer, LtrbPixelRect aabbInLayerPixels,
            DirtyRectSpaceMapping mapping, Rect bounds, PixelSize layerSize)
        {
            // The snapshot covers only the part of the AABB that is inside of the layer.
            var snapshotBounds = new LtrbPixelRect(
                Math.Max(aabbInLayerPixels.Left, 0), Math.Max(aabbInLayerPixels.Top, 0),
                Math.Min(aabbInLayerPixels.Right, layerSize.Width),
                Math.Min(aabbInLayerPixels.Bottom, layerSize.Height));
            if (snapshotBounds.Right <= snapshotBounds.Left || snapshotBounds.Bottom <= snapshotBounds.Top)
            {
                state.DisposeSnapshot();
                return;
            }

            var snapshot = state.Snapshot ??= new ServerCompositionBackdropSnapshot();

            LtrbPixelRect? inputDirtyRect = null;
            if (state.InputDirtyRect is { } dirtyInLayer)
            {
                var clamped = dirtyInLayer.IntersectOrEmpty(aabbInLayer)
                    .IntersectOrEmpty(mapping.LayerPixelsToLayerSpace(snapshotBounds.ToLtrbRectUnscaled()));
                if (!clamped.IsZeroSize)
                    inputDirtyRect = ToLayerPixels(clamped, mapping);
            }

            var renderContext = visual.Compositor.RenderInterface.Value;
            snapshot.Draw(backdropCanvas, renderContext, snapshotBounds, inputDirtyRect,
                visual.BackdropEffect!, bounds);
            state.InputDirtyRect = null; // the snapshot is up to date
        }

        // Rewinds the canvas to the single open intermediate layer, replays its pushes as a fresh empty
        // segment, draws the retained backdrop effect inside the segment and closes it, then replays the
        // pushes again for the rest of the subtree. Returns false when the canvas cannot be rewound this way.
        private bool TryRewindAndDrawRetainedBackdrop(ServerCompositionVisual visual,
            IDrawingContextImplWithBackdropSupport backdropCanvas, ServerCompositionVisualBackdropState state,
            LtrbPixelRect aabbPixels, DirtyRectSpaceMapping mapping, Rect bounds, PixelSize layerSize)
        {
            // Only one intermediate layer can be rewound: deeper ones would stay open and keep content out of
            // the surface, so the retained read would be stale.
            if (CanvasLayerDepth != 1)
                return false;

            var journal = _canvasJournal!;
            var layerIndex = -1;
            for (var i = journal.Count - 1; i >= 0; i--)
            {
                if (journal[i].OpenedLayer)
                {
                    layerIndex = i;
                    break;
                }
            }

            if (layerIndex < 0)
                return false;

            // Adorner clips above the layer cannot be replayed: their push helper keeps its own bookkeeping.
            for (var i = journal.Count - 1; i > layerIndex; i--)
            {
                if (journal[i].Kind is CanvasJournalKind.AdornerClipRect or CanvasJournalKind.AdornerGeometryClip)
                    return false;
            }

            // Close the layer and everything pushed above it. Their content composites into the surface, which
            // then holds everything behind the backdrop visual.
            for (var i = journal.Count - 1; i >= layerIndex; i--)
                PopCanvasJournalEntry(journal[i]);

            if (!backdropCanvas.IsBackdropSamplingSafe)
            {
                // The surface is still not current (this should not happen with one journaled layer). Reopen
                // the pushes and let the volatile path run.
                ReplayCanvasJournal(layerIndex);
                _canvas.Transform = _walkContext.Transform;
                return false;
            }

            // The fresh segment replays the closed pushes and is still empty, so the retained read is safe.
            ReplayCanvasJournal(layerIndex);
            _canvas.Transform = _walkContext.Transform;
            DrawRetainedBackdrop(visual, backdropCanvas, state, aabbPixels.ToLtrbRectUnscaled(),
                ToLayerPixels(aabbPixels.ToLtrbRectUnscaled(), mapping), mapping, bounds, layerSize);

            // Close the segment and reopen the pushes for the rest of the subtree.
            for (var i = journal.Count - 1; i >= layerIndex; i--)
                PopCanvasJournalEntry(journal[i]);
            ReplayCanvasJournal(layerIndex);
            _canvas.Transform = _walkContext.Transform;
            return true;
        }

        public void PostSubgraph(ServerCompositionVisual visual)
        {
            if (_fullSkip)
            {
                _fullSkip = false;
                return;
            }
            
            var bitmapCacheRoot = _renderingToBitmapCache && visual == _rootVisual;
            
            // If we've used cache, those never got pushed in PreSubgraph
            if (!_usedCache)
            {
                if (visual.Effect != null && _canvas is IDrawingContextImplWithEffects effects)
                {
                    effects.PopEffect();
                    EndCanvasJournalEntry(CanvasJournalKind.Effect);
                }

                if (visual.OpacityMaskBrush != null)
                {
                    _canvas.PopOpacityMask();
                    EndCanvasJournalEntry(CanvasJournalKind.OpacityMask);
                }

                if (visual.TextOptions != default)
                    _canvas.PopTextOptions();

                if (visual.RenderOptions != default)
                {
                    _canvas.PopRenderOptions();
                    EndCanvasJournalEntry(CanvasJournalKind.RenderOptions);
                }
            }

            // A cached visual is always a leaf in the walk (children are skipped), so its
            // PreSubgraph/PostSubgraph are adjacent. Reset here so the flag never leaks into a
            // later sibling and causes its effect/mask/options pushes to be left unpopped.
            _usedCache = false;

            // If we are rendering to bitmap cache, PreSubgraph skipped those for the root visual
            if (!bitmapCacheRoot)
            {
                if (visual.AdornedVisual != null)
                    AdornerHelper_RenderPostGraphPushAdornerClip(visual);

                if (visual.Clip != null)
                {
                    _canvas.PopGeometryClip();
                    EndCanvasJournalEntry(CanvasJournalKind.GeometryClip);
                }

                if (visual.ClipToBounds)
                {
                    _canvas.PopClip();
                    EndCanvasJournalEntry(CanvasJournalKind.ClipToBounds);
                }

                if (visual._ownClipRect.HasValue)
                    _walkContext.PopClip();

                if (visual._ownTransform.HasValue)
                {
                    _walkContext.PopTransform();
                    _canvas.Transform = _walkContext.Transform;
                }

                if (visual.Opacity != 1)
                {
                    _canvas.PopOpacity();
                    EndCanvasJournalEntry(CanvasJournalKind.Opacity);
                    _opacity = _opacityStack.Pop();
                }
            }
        }

        public void Dispose()
        {
            _walkContext.Dispose();
            _pools.DoubleStackPool.Return(ref _opacityStack);
            _pools.CanvasJournalEntryListPool.Return(_canvasJournal);
            _canvasJournal = null;
            AdornerHelper_Dispose();
        }
    }

    protected virtual void PushClipToBounds(IDrawingContextImpl canvas) =>
        canvas.PushClip(new Rect(0, 0, Size.X, Size.Y));

    public (int visited, int rendered) Render(IDrawingContextImpl canvas, LtrbRect clip, IDirtyRectTracker? dirtyRects,
        bool renderChildren = true, bool skipRootVisualTransform = false, bool renderingToBitmapCache = false)
    {
        var renderContext = new RenderContext(this, canvas, dirtyRects, Compositor.Pools, canvas.Transform,
            clip, renderChildren, skipRootVisualTransform, renderingToBitmapCache);
        try
        {
            ServerTreeWalker<RenderContext>.Walk(ref renderContext, this);
            return (renderContext.VisitedVisuals, renderContext.RenderedVisuals);
        }
        finally
        {
            renderContext.Dispose();
        }
    }

    // The kinds of canvas pushes that the render walk journals. TextOptions is not journaled: it does not
    // take part in the canvas save/restore stack and no pop reads it.
    internal enum CanvasJournalKind
    {
        Opacity,
        ClipToBounds,
        GeometryClip,
        OpacityMask,
        Effect,
        RenderOptions,
        AdornerClipRect,
        AdornerGeometryClip,
    }

    // One currently open canvas push of the render walk. The pushes are replayed at their recorded transform
    // around a backdrop visual's retained draw under an intermediate layer. Replays re-issue the same push
    // expressions with the visual's frame-constant server state, so the entry only needs the visual.
    internal readonly struct CanvasJournalEntry
    {
        public CanvasJournalEntry(CanvasJournalKind kind, ServerCompositionVisual visual, Matrix matrix,
            bool openedLayer)
        {
            Kind = kind;
            Visual = visual;
            Matrix = matrix;
            OpenedLayer = openedLayer;
        }

        public CanvasJournalKind Kind { get; }

        public ServerCompositionVisual Visual { get; }

        // The canvas transform at the time of the push.
        public Matrix Matrix { get; }

        // Whether this push opened an intermediate layer.
        public bool OpenedLayer { get; }
    }
}
