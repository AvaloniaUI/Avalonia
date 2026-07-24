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

        // Number of ancestor visuals (within the current layer host) whose children collection changed
        // this frame. Consumed by retained backdrop capture; scoped per host (reset across cache boundaries).
        private int _childrenChangedAncestorCount;
        private Stack<int> _childrenChangedAncestorCountStack;

        // A view of the current collector's working set + host-space mapping, resolved on every collector swap.
        private DirtyRectWorkingSet _currentWorkingSet;
        private List<LtrbRect>? _workingSetBuffer;

        public UpdateContext(CompositorPools pools, IDirtyRectCollector dirtyRects, Matrix transform, LtrbRect clip)
        {
            _dirtyRegion = dirtyRects;
            _context = new TreeWalkContext(pools, transform, clip);
            _dirtyRegionDisableCountStack = pools.IntStackPool.Rent();
            _childrenChangedAncestorCountStack = pools.IntStackPool.Rent();
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
                // Children-changed damage is scoped per host: ancestors above the cache don't blanket
                // backdrops inside it (different backbuffer).
                _childrenChangedAncestorCountStack.Push(_childrenChangedAncestorCount);
                _childrenChangedAncestorCount = 0;

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
                _childrenChangedAncestorCount = _childrenChangedAncestorCountStack.Pop();
                if (visual.Cache.IsDirty)
                    AddToDirtyRegion(visual._subTreeBounds);
            }
        }
        
        private bool NeedToPushBoundsAffectingProperties(ServerCompositionVisual node)
        {
            return (node._isDirtyForRenderInSubgraph || node._needsToAddExtraDirtyRectToDirtyRegion || node._contentChanged);
        }
        
        public void PreSubgraph(ServerCompositionVisual node, out bool visitChildren)
        {
            visitChildren = node._isDirtyForRenderInSubgraph || node._needsBoundingBoxUpdate;
            
            // If this node has an alpha mask an we caused its inner bounds to change
            // then treat the node as if _isDirtyForRender was set.
            if (node is { _needsBoundingBoxUpdate: true, OpacityMaskBrush: not null })
                node._isDirtyForRender = true;

            // Same reason for a backdrop: its output region is a function of its subtree bounds, so a
            // descendant-driven bounds change must emit the backdrop's own old∪new bounds.
            if (node is { _needsBoundingBoxUpdate: true } && node.BackdropEffect.IsSupportedBackdropEffect())
                node._isDirtyForRender = true;

            // Retained backdrop capture, before this node's own/descendant damage (old-bounds emission and
            // the dirty-region-disable increment below) which paints on top of the effect.
            CaptureRetainedBackdropDirtyRects(node);

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

            // Consumed by descendant backdrop capture. Incremented after any cache push so a cache owner's
            // structural change blankets the backdrops inside its cache, but not for its own backdrop
            // (captured above) nor for ancestors above the cache.
            if (node._childrenChanged)
                _childrenChangedAncestorCount++;

            // Descent gate: keep descending a clean spine toward backdrops while the current host collector
            // has damage. Uses the cache's tracker once a cache has been pushed for this node.
            if (node._backdropsInSubTree > 0 && !_currentWorkingSet.IsEmpty)
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

            // FinalizeSubtreeBounds may have just changed this backdrop's bounds after the registry snapshot;
            // re-ingest its whole input and refresh the record so the registry doesn't repeat the work next frame.
            CheckRetainedBackdropStaleAabb(node);

            //
            // If there are additional dirty regions, pick them up. (Additional dirty regions are
            // specified before the tranform, i.e. in inner space, hence we have to pick them
            // up before we pop the transform from the transform stack.
            //
            if (node._needsToAddExtraDirtyRectToDirtyRegion)
            {
                AddToDirtyRegion(node._extraDirtyRect);
            }

            // Mirror of the PreSubgraph increment (before the matching cache pop restores the host's count).
            if (node._childrenChanged)
                _childrenChangedAncestorCount--;

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
        
        // Accumulates a retained backdrop's pending input damage at its DFS position.
        private void CaptureRetainedBackdropDirtyRects(ServerCompositionVisual node)
        {
            var record = node.BackdropState;
            if (record is not { IsRetained: true, Aabb: { } aabbPixels })
                return;

            var aabb = aabbPixels.ToLtrbRectUnscaled();

            LtrbRect? damage;
            if (_dirtyRegionDisableCount > 0 || _childrenChangedAncestorCount > 0)
                // A covering dirty-for-render ancestor (whose rect isn't in the working set until its own
                // PostSubgraph) or a structural ancestor change ⇒ treat the whole input as dirty.
                damage = aabb;
            else
            {
                damage = null;
                if (!_currentWorkingSet.IsEmpty)
                {
                    var buffer = _workingSetBuffer ??= _context.Pools.LtrbRectListPool.Rent();
                    _currentWorkingSet.CollectHostSpace(buffer);
                    foreach (var rect in buffer)
                    {
                        if (rect.IntersectOrNull(aabb) is { } hit)
                            damage = LtrbRect.FullUnion(damage, hit);
                    }
                }
            }

            if (damage is not { } newDamage)
                return;

            record.RetainedInputDirtyArea = LtrbRect.FullUnion(record.RetainedInputDirtyArea, newDamage);

            // Output inflation (R > 0): the blurred halo (D ⊕ R) ∩ AABB. Added directly to the current
            // collector in host space (never via AddToDirtyRegion, which re-applies the walk transform and
            // no-ops under disabled regions). Skipped while disabled: the covering ancestor's bounds subsume it.
            if (record.HostSpaceSamplingRadius > 0 && _dirtyRegionDisableCount == 0
                && newDamage.Inflate(new Thickness(record.HostSpaceSamplingRadius)).IntersectOrNull(aabb) is { } halo)
                _dirtyRegion.AddRect(halo);
        }

        // Stale-AABB check in a retained backdrop's PostSubgraph.
        private void CheckRetainedBackdropStaleAabb(ServerCompositionVisual node)
        {
            var record = node.BackdropState;
            if (record is not { IsRetained: true })
                return;

            LtrbPixelRect? fresh = null;
            if (node._transformedSubTreeBounds is { } bounds)
            {
                var hostRect = bounds.TransformToAABB(record.VisualToHostTransform);
                if (!hostRect.IsZeroSize)
                    fresh = LtrbPixelRect.FromRectUnscaled(hostRect);
            }

            if (fresh != record.Aabb)
            {
                record.Aabb = fresh;
                record.RetainedInputDirtyArea = fresh?.ToLtrbRectUnscaled();
                // The AABB moved, so the retained texture must be re-realized; matches the registry's own
                // reset on an AABB change (which won't fire next frame now that the record is updated in place).
                record.DisposeRetainedStateWithContext();
            }
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
            _context.Pools.IntStackPool.Return(ref _childrenChangedAncestorCountStack);
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