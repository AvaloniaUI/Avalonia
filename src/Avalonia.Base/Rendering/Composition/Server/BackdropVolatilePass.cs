using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionTarget
{
    /// <summary>
    /// The volatile pass. Runs in <see cref="ServerCompositionTarget.Update"/> after the update walk.
    /// A volatile backdrop visual cannot be redrawn in part: when a dirty rect touches its AABB, the whole AABB
    /// becomes dirty (whole-AABB invalidation). That is new input for the backdrop visuals in front of it, so
    /// the pass repeats until nothing changes. It only adds dirty rects and grows input dirty rects, it draws
    /// nothing.
    ///
    /// The pass reads the working sets only. <c>Intersects()</c> and <c>CombinedRect</c> are not valid before
    /// <c>FinalizeFrame</c>, and <c>GetUninflatedDirtyRegions()</c> makes the multi-rect tracker ignore rects
    /// that are added later.
    /// </summary>
    internal sealed class BackdropVolatilePass
    {
        internal struct VolatileEntry
        {
            public int DrawOrderIndex;
            public ServerCompositionVisualBackdropState State;
            public bool Invalidated;
        }

        internal struct RetainedEntry
        {
            public int DrawOrderIndex;
            public ServerCompositionVisualBackdropState State;
        }

        internal sealed class LayerScan
        {
            // Null for the root layer.
            public ServerCompositionVisualCache? Layer;

            // The dirty region of the layer.
            public IDirtyRectCollector Collector = null!;

            // The root visual of the target, or the visual with the bitmap cache.
            public ServerCompositionVisual LayerRootVisual = null!;

            // The layer that this bitmap cache is drawn into, and the place and bounds of the cached visual in it.
            public LayerScan? OuterLayer;
            public int DrawOrderIndexInOuterLayer;
            public LtrbRect? BoundsInOuterLayer;

            public List<VolatileEntry> Volatiles = null!;
            public List<RetainedEntry> Retained = null!;

            // Whether this pass added a dirty rect to the layer.
            public bool ReceivedNewDirtyRects;
            public int Counter;

            // Volatiles and Retained are set by NewLayerScan.
            public void Reset()
            {
                Layer = null;
                Collector = null!;
                LayerRootVisual = null!;
                OuterLayer = null;
                DrawOrderIndexInOuterLayer = 0;
                BoundsInOuterLayer = null;
                ReceivedNewDirtyRects = false;
                Counter = 0;
            }
        }

        private CompositorPools _pools = null!;
        private List<LayerScan> _layers = null!;
        private List<LtrbRect> _workingBuffer = null!;
        // Used by AddInputDirtyRectInFront.
        private List<LtrbRect> _deliveryBuffer = null!;
        // LayerScan objects are reused across frames. Their lists come from CompositorPools.
        private readonly Stack<LayerScan> _layerScanPool = new();

        public void Run(ServerCompositionTarget target, IDirtyRectCollector rootCollector, double rootScaling)
        {
            if (target.BackdropVisuals.Count == 0 || target.Root == null)
                return;
            Execute(target, rootCollector, rootScaling);
        }

        private void Execute(ServerCompositionTarget target, IDirtyRectCollector rootCollector, double rootScaling)
        {
            _pools = target.Compositor.Pools;
            _layers = _pools.BackdropLayerScanListPool.Rent();
            _workingBuffer = _pools.LtrbRectListPool.Rent();
            _deliveryBuffer = _pools.LtrbRectListPool.Rent();
            try
            {
                var root = target.Root!;
                var rootScan = NewLayerScan();
                rootScan.Layer = null;
                rootScan.Collector = rootCollector;
                rootScan.LayerRootVisual = root;
                _layers.Add(rootScan);

                // Layer space of the root layer is device pixels.
                var scale = Matrix.CreateScale(rootScaling, rootScaling);
                var rootToLayer = root.OwnTransform is { } rootTransform ? rootTransform * scale : scale;
                Scan(rootScan, root, rootToLayer);

                // Scan adds a bitmap cache layer after the layer it is drawn into. Go in reverse, so that a
                // bitmap cache layer has passed its dirty rects to the outer layer before that one is handled.
                for (var i = _layers.Count - 1; i >= 0; i--)
                {
                    var scan = _layers[i];
                    InvalidateVolatiles(scan);
                    if (scan.Layer != null && scan.ReceivedNewDirtyRects)
                        PropagateToOuterLayer(scan);
                }
            }
            finally
            {
                foreach (var scan in _layers)
                {
                    _pools.BackdropVolatileEntryListPool.Return(scan.Volatiles);
                    _pools.BackdropRetainedEntryListPool.Return(scan.Retained);
                    scan.Volatiles = null!;
                    scan.Retained = null!;
                    _layerScanPool.Push(scan);
                }
                _pools.BackdropLayerScanListPool.Return(ref _layers);
                _pools.LtrbRectListPool.Return(ref _workingBuffer);
                _pools.LtrbRectListPool.Return(ref _deliveryBuffer);
            }
        }

        private LayerScan NewLayerScan()
        {
            var scan = _layerScanPool.Count > 0 ? _layerScanPool.Pop() : new LayerScan();
            scan.Reset();
            scan.Volatiles = _pools.BackdropVolatileEntryListPool.Rent();
            scan.Retained = _pools.BackdropRetainedEntryListPool.Rent();
            return scan;
        }

        // Finds the backdrop visuals and the bitmap caches of one layer, in draw order. `toLayer` maps the local
        // space of `node` to layer space.
        private void Scan(LayerScan scan, ServerCompositionVisual node, Matrix toLayer)
        {
            var index = scan.Counter++;

            var state = node.BackdropState;
            if (state is { Aabb: not null } && ReferenceEquals(state.Layer, scan.Layer))
            {
                if (state.IsRetained)
                    scan.Retained.Add(new RetainedEntry { DrawOrderIndex = index, State = state });
                else
                    scan.Volatiles.Add(new VolatileEntry { DrawOrderIndex = index, State = state });
            }

            // A bitmap cache below the layer's root visual is a layer of its own: scan it separately.
            if (node.Cache is { } childLayer && !ReferenceEquals(node, scan.LayerRootVisual))
            {
                if (node._backdropVisualsInSubTree > 0)
                {
                    var childScan = NewLayerScan();
                    childScan.Layer = childLayer;
                    childScan.Collector = childLayer.DirtyRectCollector;
                    childScan.LayerRootVisual = node;
                    childScan.OuterLayer = scan;
                    childScan.DrawOrderIndexInOuterLayer = index;
                    childScan.BoundsInOuterLayer =
                        node.SubTreeBounds is { } bounds ? bounds.TransformToAABB(toLayer) : null;
                    _layers.Add(childScan);
                    // Layer space of a bitmap cache layer is the local space of the cached visual.
                    Scan(childScan, node, Matrix.Identity);
                }

                return;
            }

            if (node.Children is not { } children)
                return;

            foreach (var child in children.List)
            {
                // Skip subtrees without backdrop visuals.
                if (child._backdropVisualsInSubTree > 0 || child.BackdropState != null)
                {
                    var childToLayer = child.OwnTransform is { } ownTransform ? ownTransform * toLayer : toLayer;
                    Scan(scan, child, childToLayer);
                }
            }
        }

        // Whole-AABB invalidation, repeated until nothing changes. Every entry is invalidated at most once, so the
        // loop ends.
        private void InvalidateVolatiles(LayerScan scan)
        {
            if (scan.Volatiles.Count == 0)
                return;

            var volatiles = CollectionsMarshal.AsSpan(scan.Volatiles);
            var changed = true;
            while (changed)
            {
                changed = false;

                var workingSet = scan.Collector.GetWorkingSet();
                if (workingSet.IsEmpty)
                    break;
                workingSet.CopyInLayerSpaceTo(_workingBuffer);
                if (_workingBuffer.Count == 0)
                    break;

                for (var i = 0; i < volatiles.Length; i++)
                {
                    ref var entry = ref volatiles[i];
                    if (entry.Invalidated || entry.State.LayerSpaceSamplingRadius <= 0
                        || entry.State.Aabb is not { } aabbPixels)
                        continue;

                    var aabb = aabbPixels.ToLtrbRectUnscaled();
                    if (!IntersectsWorkingSet(aabb))
                        continue;

                    entry.Invalidated = true;
                    scan.Collector.AddRect(aabb);
                    scan.ReceivedNewDirtyRects = true;
                    AddInputDirtyRectInFront(scan, entry.DrawOrderIndex, aabb);
                    changed = true;
                }
            }
        }

        private bool IntersectsWorkingSet(LtrbRect aabb)
        {
            foreach (var rect in _workingBuffer)
                if (rect.IntersectOrNull(aabb) is not null)
                    return true;
            return false;
        }

        // Adds `source` to the input dirty rect of every retained backdrop visual in front of
        // `sourceDrawOrderIndex`, and their output dirty rects to the dirty region. An output dirty rect is input
        // for the retained backdrop visuals in front of it, so it is added to the sources (scan.Retained is in
        // draw order).
        private void AddInputDirtyRectInFront(LayerScan scan, int sourceDrawOrderIndex, LtrbRect source)
        {
            var sources = _deliveryBuffer;
            sources.Clear();
            sources.Add(source);

            foreach (var retained in scan.Retained)
            {
                if (retained.DrawOrderIndex <= sourceDrawOrderIndex)
                    continue;

                var state = retained.State;
                if (state.Aabb is not { } aabbPixels)
                    continue;

                var aabb = aabbPixels.ToLtrbRectUnscaled();
                LtrbRect? hit = null;
                foreach (var rect in sources)
                    if (rect.IntersectOrNull(aabb) is { } intersection)
                        hit = LtrbRect.FullUnion(hit, intersection);

                if (hit is not { } inputDirty)
                    continue;

                state.InputDirtyRect = LtrbRect.FullUnion(state.InputDirtyRect, inputDirty);

                if (state.LayerSpaceSamplingRadius > 0
                    && inputDirty.Inflate(new Thickness(state.LayerSpaceSamplingRadius)).IntersectOrNull(aabb)
                        is { } outputDirtyRect)
                {
                    scan.Collector.AddRect(outputDirtyRect);
                    scan.ReceivedNewDirtyRects = true;
                    sources.Add(outputDirtyRect);
                }
            }
        }

        // A bitmap cache layer that got new dirty rects is redrawn, so the bounds of the cached visual are dirty in
        // the outer layer.
        private void PropagateToOuterLayer(LayerScan scan)
        {
            var outer = scan.OuterLayer;
            if (outer == null || scan.BoundsInOuterLayer is not { } bounds || bounds.IsZeroSize)
                return;

            // The update walk may have collected these bounds already. AddInputDirtyRectInFront is still needed for
            // the retained backdrop visuals in front of the cached visual.
            outer.Collector.AddRect(bounds);
            outer.ReceivedNewDirtyRects = true;
            AddInputDirtyRectInFront(outer, scan.DrawOrderIndexInOuterLayer, bounds);
        }
    }
}
