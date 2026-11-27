using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionVisual
{
    protected virtual bool HasEffect => Effect != null;
    
    struct UpdateContext : IServerTreeVisitor, IDisposable
    {
        private TreeWalkContext _context;
        
        private IDirtyRectCollector _dirtyRegion;
        private int _dirtyRegionDisableCount;
        private Stack<int> _dirtyRegionDisableCountStack;
        private Stack<IDirtyRectCollector> _dirtyRegionCollectorStack;
        private bool AreDirtyRegionsDisabled() => _dirtyRegionDisableCount != 0;

        // Number of visuals in the parent chain, within the current layer, whose children collection changed
        // this frame. Reset when a bitmap cache is pushed.
        private int _childrenChangedParentChainCount;
        private Stack<int> _childrenChangedParentChainCountStack;

        // The working set of the current layer. Updated whenever _dirtyRegion is switched.
        private DirtyRectWorkingSet _currentWorkingSet;

        // Number of skipped bitmap caches that the walk is currently inside of (e.g. the cached visual itself
        // moved). The walk state then belongs to an outer layer, while the backdrop visuals below read the
        // bitmap cache layer. A plain counter is enough: a bitmap cache is only skipped when nothing in its
        // subtree is dirty, so no bitmap cache below it is pushed and no dirty rect is collected while this is
        // non-zero.
        private int _skippedBitmapCacheDepth;
        private List<LtrbRect>? _workingSetBuffer;

        public UpdateContext(CompositorPools pools, IDirtyRectCollector dirtyRects, Matrix transform, LtrbRect clip)
        {
            _dirtyRegion = dirtyRects;
            _context = new TreeWalkContext(pools, transform, clip);
            _dirtyRegionDisableCountStack = pools.IntStackPool.Rent();
            _childrenChangedParentChainCountStack = pools.IntStackPool.Rent();
            _dirtyRegionCollectorStack = pools.DirtyRectCollectorStackPool.Rent();
            _currentWorkingSet = _dirtyRegion.GetWorkingSet();
        }

        private void PushCacheIfNeeded(ServerCompositionVisual visual)
        {
            if (visual.Cache != null)
            {
                _dirtyRegionCollectorStack.Push(_dirtyRegion);
                _dirtyRegion = visual.Cache.DirtyRectCollector;
                _currentWorkingSet = _dirtyRegion.GetWorkingSet();
                _dirtyRegionDisableCountStack.Push(_dirtyRegionDisableCount);
                _dirtyRegionDisableCount = 0;
                // A children change outside of the bitmap cache does not affect the backdrop inside of it.
                _childrenChangedParentChainCountStack.Push(_childrenChangedParentChainCount);
                _childrenChangedParentChainCount = 0;

                _context.PushSetTransform(Matrix.Identity);
                _context.ResetClip(LtrbRect.Infinite);
            }
        }

        private void PopCacheIfNeeded(ServerCompositionVisual visual)
        {
            if (visual.Cache != null)
            {
                _context.PopClip();
                _context.PopTransform();
                _dirtyRegion = _dirtyRegionCollectorStack.Pop();
                _currentWorkingSet = _dirtyRegion.GetWorkingSet();
                _dirtyRegionDisableCount = _dirtyRegionDisableCountStack.Pop();
                _childrenChangedParentChainCount = _childrenChangedParentChainCountStack.Pop();
                if (visual.Cache.IsDirty)
                    AddToDirtyRegion(visual._subTreeBounds);
            }
        }
        
        private bool NeedToPushBoundsAffectingProperties(ServerCompositionVisual node)
        {
            return (node._isDirtyForRenderInSubgraph || node._needsToAddExtraDirtyRectToDirtyRegion || node._contentChanged);
        }

        private bool IsBitmapCacheSkipped(ServerCompositionVisual node) =>
            node.Cache != null && !NeedToPushBoundsAffectingProperties(node);
        
        public void PreSubgraph(ServerCompositionVisual node, out bool visitChildren)
        {
            visitChildren = node._isDirtyForRenderInSubgraph || node._needsBoundingBoxUpdate;
            
            // If this node has an alpha mask an we caused its inner bounds to change
            // then treat the node as if _isDirtyForRender was set.
            if (node is { _needsBoundingBoxUpdate: true, OpacityMaskBrush: not null })
                node._isDirtyForRender = true;

            // Same for a backdrop visual with subtree bounds: its output covers its subtree bounds, so when a
            // child changes them, the old and the new bounds of this visual are dirty.
            if (node is { _needsBoundingBoxUpdate: true, BackdropEffectBoundsSource: BackdropEffectBoundsSource.Subtree }
                && node.BackdropEffect != null)
                node._isDirtyForRender = true;

            // Before anything of this visual is collected: its own content and children are drawn in front of
            // its backdrop effect, so they are not input.
            CollectBackdropInputDirtyRects(node);

            // Special handling for effects: just add the entire node's old subtree bounds as a dirty region
            // WPF does this because they had legacy effects with non-affine transforms, we do this because 
            // it's something to be done in the future (maybe)
            if (node._isDirtyForRender || node is { _isDirtyForRenderInSubgraph: true, HasEffect: true })
            {
                // If bounds haven't actually changed, there is no point in adding them now since they will be added
                // again in PostSubgraph.
                if (node._needsBoundingBoxUpdate && !AreDirtyRegionsDisabled())
                {
                    // We add this node's bbox to the dirty region. Alternatively we could walk the sub-graph and add the
                    // bbox of each node's content to the dirty region. Note that this is much harder to do because if the
                    // transform changes we don't know anymore the old transform. We would have to use to a two phased dirty
                    // region algorithm.
                    AddToDirtyRegion(node._transformedSubTreeBounds);
                }
                
                // If we added a node in the parent chain to the bbox we don't need to add anything below this node
                // to the dirty region.
                _dirtyRegionDisableCount++;
            }

            // If a node in the sub-graph of this node is dirty for render and we haven't collected the bbox of one of pNode's
            // ascendants as dirty region, then we need to maintain the transform and clip stack so that we have a world transform
            // when we need to collect the bbox of the descendant node that is dirty for render.  If something has changed
            // in the contents or subgraph, we need to update the cache on this node.
            if (NeedToPushBoundsAffectingProperties(node))
            {
                // Dirty regions will be enabled if we haven't collected an ancestor's bbox or if they were re-enabled
                // by an ancestor's cache.
                if (!AreDirtyRegionsDisabled())
                {
                    PushBoundsAffectingProperties(node);
                }

                PushCacheIfNeeded(node);
            }

            if (IsBitmapCacheSkipped(node))
                _skippedBitmapCacheDepth++;

            // After the bitmap cache push, so that it counts for the backdrop visuals inside of this visual's
            // bitmap cache, and not for this visual's own backdrop effect (handled above).
            if (node._childrenChanged)
                _childrenChangedParentChainCount++;

            // Descent check: visit the children of a clean visual when a backdrop visual below may have dirty
            // input. That is when the layer already has dirty rects, or when a dirty parent or a children change
            // in the parent chain will add them later (in PostSubgraph). Not inside a skipped bitmap cache: its
            // layer has its own backdrop and nothing in it changed.
            if (node._backdropVisualsInSubTree > 0 && _skippedBitmapCacheDepth == 0
                && (!_currentWorkingSet.IsEmpty || _dirtyRegionDisableCount > 0 || _childrenChangedParentChainCount > 0))
                visitChildren = true;

            if (node._needsBoundingBoxUpdate)
            {
                // This node's bbox needs to be updated. We start out by setting his bbox to the bbox of its content. All its
                // children will union their bbox into their parent's bbox. PostSubgraph will clip the bbox and transform it
                // to outer space.
                node._subTreeBounds = node._ownContentBounds;
            }
        }
        
        
        public void PostSubgraph(ServerCompositionVisual node)
        {
            var parent = node.Parent;
            if (node._needsBoundingBoxUpdate)
            {
                //
                // If pNode's bbox got recomputed it is at this point still in inner
                // space. We need to apply the clip and transform.
                //
                FinalizeSubtreeBounds(node);
            }
            
            //
            // Update state on the parent node if we have a parent.

            if (parent != null)
            {
                // Update the bounding box on the parent.
                if (parent._needsBoundingBoxUpdate)
                    parent._subTreeBounds = LtrbRect.FullUnion(parent._subTreeBounds, node._transformedSubTreeBounds);
            }

            // FinalizeSubtreeBounds may have changed the bounds that the backdrop AABB was computed from.
            CheckBackdropStaleAabb(node);

            //
            // If there are additional dirty regions, pick them up. (Additional dirty regions are
            // specified before the tranform, i.e. in inner space, hence we have to pick them
            // up before we pop the transform from the transform stack.
            //
            if (node._needsToAddExtraDirtyRectToDirtyRegion)
            {
                AddToDirtyRegion(node._extraDirtyRect);
            }

            // Before the bitmap cache pop, which restores the count of the outer layer.
            if (node._childrenChanged)
                _childrenChangedParentChainCount--;

            if (IsBitmapCacheSkipped(node))
                _skippedBitmapCacheDepth--;

            // If we pushed transforms here, we need to pop them again.  If we're handling a cache we need
            // to finish handling it here as well.
            if (NeedToPushBoundsAffectingProperties(node))
            {
                PopCacheIfNeeded(node);
                if(!AreDirtyRegionsDisabled())
                    PopBoundsAffectingProperties(node);

            }
            
            // Special handling for effects: just add the entire node's old subtree bounds as a dirty region
            // WPF does this because they had legacy effects with non-affine transforms, we do this because  
            // it's something to be done in the future (maybe)
            if(node._isDirtyForRender || node is { _isDirtyForRenderInSubgraph: true, HasEffect: true })
            {
                _dirtyRegionDisableCount--;
                AddToDirtyRegion(node._transformedSubTreeBounds);
            }

            node._isDirtyForRender = false;
            node._isDirtyForRenderInSubgraph = false;
            node._needsBoundingBoxUpdate = false;
            node._needsToAddExtraDirtyRectToDirtyRegion = false;
            node._contentChanged = false;
            node._childrenChanged = false;
        }

        private void FinalizeSubtreeBounds(ServerCompositionVisual node)
        {
            // WPF simply removes drawing commands from every visual in invisible subtree (on UI thread).
            // We set the bounds to null when computing subtree bounds for invisible nodes.
            if (!node.Visible) 
                node._subTreeBounds = null;

            if (node._subTreeBounds != null)
            {
                if (node.Effect != null)
                    node._subTreeBounds = node._subTreeBounds.Value.Inflate(node.Effect.GetEffectOutputPadding());

                if (node._ownClipRect.HasValue)
                    node._subTreeBounds = node._subTreeBounds.Value.IntersectOrNull(node._ownClipRect.Value);
            }

            if (node._subTreeBounds == null)
                node._transformedSubTreeBounds = null;
            else if (node._ownTransform.HasValue)
                node._transformedSubTreeBounds = node._subTreeBounds?.TransformToAABB(node._ownTransform.Value);
            else
                node._transformedSubTreeBounds = node._subTreeBounds;

            node.EnqueueForReadbackUpdate();
        }

        private void AddToDirtyRegion(LtrbRect? bounds)
        {
            if(_dirtyRegionDisableCount != 0 || !bounds.HasValue)
                return;

            var transformed = bounds.Value.TransformToAABB(_context.Transform).IntersectOrEmpty(_context.Clip);
            if(transformed.IsZeroSize)
                return;

            _dirtyRegion.AddRect(transformed);
        }
        
        // Collects the input dirty rect of a retained backdrop visual. Everything collected before the walk
        // reaches the visual is behind it.
        private void CollectBackdropInputDirtyRects(ServerCompositionVisual node)
        {
            var state = node.BackdropState;
            if (state is not { IsRetained: true, Aabb: { } aabbPixels })
                return;

            // The working set belongs to an outer layer, not to the bitmap cache layer that this visual reads.
            if (_skippedBitmapCacheDepth > 0)
                return;

            var aabb = aabbPixels.ToLtrbRectUnscaled();

            LtrbRect? inputDirty;
            if (_dirtyRegionDisableCount > 0 || _childrenChangedParentChainCount > 0)
                // The dirty rect of a dirty parent or of a children change is collected in that parent's
                // PostSubgraph, so it is not in the working set yet. The whole input is dirty.
                inputDirty = aabb;
            else
            {
                inputDirty = null;
                if (!_currentWorkingSet.IsEmpty)
                {
                    var buffer = _workingSetBuffer ??= _context.Pools.LtrbRectListPool.Rent();
                    _currentWorkingSet.CopyInLayerSpaceTo(buffer);
                    foreach (var rect in buffer)
                    {
                        if (rect.IntersectOrNull(aabb) is { } hit)
                            inputDirty = LtrbRect.FullUnion(inputDirty, hit);
                    }
                }
            }

            if (inputDirty is not { } newDirtyRect)
                return;

            state.InputDirtyRect = LtrbRect.FullUnion(state.InputDirtyRect, newDirtyRect);

            // The output dirty rect. It is in layer space already, so it is added directly and not through
            // AddToDirtyRegion, which applies the walk transform. Not needed under a dirty parent: the parent's
            // bounds cover it.
            if (state.LayerSpaceSamplingRadius > 0 && _dirtyRegionDisableCount == 0
                && newDirtyRect.Inflate(new Thickness(state.LayerSpaceSamplingRadius)).IntersectOrNull(aabb)
                    is { } outputDirtyRect)
                _dirtyRegion.AddRect(outputDirtyRect);
        }

        // The AABB of a backdrop visual with subtree bounds was computed from the bounds of the previous update
        // walk. This walk may have changed them, or computed them for the first time.
        private void CheckBackdropStaleAabb(ServerCompositionVisual node)
        {
            var state = node.BackdropState;
            if (state == null || node.BackdropEffectBoundsSource != BackdropEffectBoundsSource.Subtree)
                return;

            LtrbPixelRect? fresh = null;
            if (node._transformedSubTreeBounds is { } bounds)
            {
                var hostRect = bounds.TransformToAABB(state.ParentToLayerTransform);
                if (!hostRect.IsZeroSize)
                    fresh = LtrbPixelRect.FromRectUnscaled(hostRect);
            }

            if (fresh == state.Aabb)
                return;

            // With the state updated here, the next backdrop state recompute sees no change.
            state.Aabb = fresh;
            if (!state.IsRetained)
                return;

            // Same as an AABB change in RecomputeBackdropState.
            state.InputDirtyRect = fresh?.ToLtrbRectUnscaled();
            if (fresh == null)
                state.DisposeSnapshotWithContext();
        }

        private void PushBoundsAffectingProperties(ServerCompositionVisual node)
        {
            if (node._ownTransform.HasValue)
                _context.PushTransform(node._ownTransform.Value);
            if (node._ownClipRect.HasValue) 
                _context.PushClip(node._ownClipRect.Value.TransformToAABB(_context.Transform));
        }
        
        private void PopBoundsAffectingProperties(ServerCompositionVisual node)
        {
            if (node._ownTransform.HasValue)
                _context.PopTransform();
            if (node._ownClipRect.HasValue)
                _context.PopClip();
        }

        public void Dispose()
        {
            _context.Pools.IntStackPool.Return(ref _dirtyRegionDisableCountStack);
            _context.Pools.IntStackPool.Return(ref _childrenChangedParentChainCountStack);
            _context.Pools.DirtyRectCollectorStackPool.Return(ref _dirtyRegionCollectorStack);
            if (_workingSetBuffer != null)
                _context.Pools.LtrbRectListPool.Return(ref _workingSetBuffer);
            _context.Dispose();
        }
    }
    
    public void UpdateRoot(IDirtyRectCollector tracker, Matrix transform, LtrbRect clip)
    {
        var context = new UpdateContext(Compositor.Pools, tracker, transform, clip);
        ServerTreeWalker<UpdateContext>.Walk(ref context, this);
        context.Dispose();
    }

}
