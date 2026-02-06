using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    [StructLayout(LayoutKind.Auto)]
    partial struct RenderContext : IServerTreeVisitor, IDisposable
    {
        private readonly IDrawingContextImpl _canvas;
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
            _dirtyRects = dirtyRects;
            _pools = pools;
            _renderChildren = renderChildren;

            _rootVisual = rootVisual;
            
            _walkContext = new TreeWalkContext(pools, matrix, clip);

            _opacity = 1;
            _opacityStack = pools.DoubleStackPool.Rent();
            _skipNextVisualTransform = skipRootVisualTransform;
            _renderingToBitmapCache = renderingToBitmapCache;
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
                _opacityStack.Push(effectiveOpacity);
                _canvas.PushOpacity(visual.Opacity, visual._transformedSubTreeBounds.Value.ToRect());
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
                _canvas.PushClip(new Rect(0, 0, visual.Size.X, visual.Size.Y));

            if (visual.Clip != null)
                _canvas.PushGeometryClip(visual.Clip);

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
                _canvas.PushRenderOptions(visual.RenderOptions);
            
            if (visual.TextOptions != default)
                _canvas.PushTextOptions(visual.TextOptions);

            if (visual.OpacityMaskBrush != null)
                _canvas.PushOpacityMask(visual.OpacityMaskBrush, visual._subTreeBounds!.Value.ToRect());
            
            if (visual.Effect != null && _canvas is IDrawingContextImplWithEffects effects)
                effects.PushEffect(visual._subTreeBounds!.Value.ToRect(), visual.Effect);

            visual.RenderCore(_publicContext, _walkContext.Clip);
            
            visitChildren = _renderChildren;
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
                    effects.PopEffect();

                if (visual.OpacityMaskBrush != null)
                    _canvas.PopOpacityMask();

                if (visual.TextOptions != default)
                    _canvas.PopTextOptions();
                
                if (visual.RenderOptions != default)
                    _canvas.PopRenderOptions();
            }
            
            // If we are rendering to bitmap cache, PreSubgraph skipped those for the root visual
            if (!bitmapCacheRoot)
            {
                if (visual.AdornedVisual != null)
                    AdornerHelper_RenderPostGraphPushAdornerClip(visual);

                if (visual.Clip != null)
                    _canvas.PopGeometryClip();

                if (visual.ClipToBounds)
                    _canvas.PopClip();

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
                    _opacity = _opacityStack.Pop();
                }
            }
        }

        public void Dispose()
        {
            _walkContext.Dispose();
            _pools.DoubleStackPool.Return(ref _opacityStack);
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

}
