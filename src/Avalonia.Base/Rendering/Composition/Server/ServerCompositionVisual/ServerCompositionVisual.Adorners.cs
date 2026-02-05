using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    // Support for adorners is a rather cancerou^W invasive thing, so we isolate all related code in this file
    // and prefix it with AdornerHelper_.
    
    private void AttHelper_OnAdornedVisualWorldTransformChanged() => AdornerHelper_EnqueueForAdornerUpdate();

    private void AdornerHelper_AttachedToRoot()
    {
        if(AdornedVisual != null)
            AdornerHelper_EnqueueForAdornerUpdate();
    }

    public void AdornerHelper_EnqueueForAdornerUpdate()
    {
        var helper = GetAttHelper();
        if(helper.EnqueuedForAdornerUpdate)
            return;
        Compositor.EnqueueAdornerUpdate(this);
        helper.EnqueuedForAdornerUpdate = true;
    }
    
    partial void OnAdornedVisualChanging() =>
        AdornedVisual?.AttHelper_UnsubscribeFromActNotification(GetAttHelper().AdornedVisualActSubscriptionAction);

    partial void OnAdornedVisualChanged()
    {
        AdornedVisual?.AttHelper_SubscribeToActNotification(GetAttHelper().AdornedVisualActSubscriptionAction);
        AdornerHelper_EnqueueForAdornerUpdate();
    }

    private static ServerCompositionVisual? AdornerLayer_GetExpectedSharedAncestor(ServerCompositionVisual adorner)
    {
        // This is hardcoded to VisualLayerManager -> AdornerLayer -> adorner
        // Since AdornedVisual is a private API that's only supposed to be accessible from AdornerLayer
        // it's a safe assumption to make
        return adorner?.Parent?.Parent;
    }
    
    public void UpdateAdorner()
    {
        GetAttHelper().EnqueuedForAdornerUpdate = false;
        var ownTransform = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, TransformMatrix, Scale,
            RotationAngle, Orientation, Offset);

        if (AdornedVisual != null && Parent != null)
        {
            if (
                AdornerLayer_GetExpectedSharedAncestor(this) is {} sharedAncestor
                && ComputeTransformFromAncestor(AdornedVisual, sharedAncestor, out var adornerLayerToAdornedVisual))
                ownTransform = (ownTransform ?? Matrix.Identity) * adornerLayerToAdornedVisual;
            else
                ownTransform = default(Matrix); // Don't render, something is broken

        }
        _ownTransform = ownTransform;
        
        PropagateFlags(true, true);
    }
    
    partial struct RenderContext
    {
        private enum Op
        {
            PopClip,
            PopGeometryClip,
            Stop
        }
        private Stack<int>? _adornerPushedClipStack;
        private ServerCompositionVisual? _currentAdornerLayer;
        
        private bool AdornerLayer_WalkAdornerParentClipRecursive(ServerCompositionVisual? visual)
        {
            if (visual != _currentAdornerLayer!)
            {
                // AdornedVisual is a part of a different subtree, this is not supported
                if (visual == null)
                    return false;

                if (!AdornerLayer_WalkAdornerParentClipRecursive(visual.Parent))
                    return false;
            }

            if (visual._ownTransform.HasValue)
                _canvas.Transform = visual._ownTransform.Value * _canvas.Transform;
            
            if (visual.ClipToBounds)
            {
                _canvas.PushClip(new Rect(0, 0, visual.Size.X, visual.Size.Y));
                _adornerPushedClipStack!.Push((int)Op.PopClip);
            }
            
            if (visual.Clip != null)
            {
                _canvas.PushGeometryClip(visual.Clip);
                _adornerPushedClipStack!.Push((int)Op.PopGeometryClip);
            }

            return true;
        }

        bool SkipAdornerClip(ServerCompositionVisual visual)
        {
            if (!visual.AdornerIsClipped
                || visual == _rootVisual
                || visual._parent == _rootVisual // Root visual is AdornerLayer
                || AdornerLayer_GetExpectedSharedAncestor(visual) == null)
                return true;
            return false;
        }
        
        private void AdornerHelper_RenderPreGraphPushAdornerClip(ServerCompositionVisual visual)
        {
            if (SkipAdornerClip(visual))
                return;
            
            _adornerPushedClipStack ??= _pools.IntStackPool.Rent();
            _adornerPushedClipStack.Push((int)Op.Stop);

            var originalTransform = _canvas.Transform;
            var transform = originalTransform;
            if (visual._ownTransform.HasValue)
            {
                if (!visual._ownTransform.Value.TryInvert(out var transformToAdornerLayer))
                    return;
                transform = transformToAdornerLayer * transform;
            }

            _canvas.Transform = transform;
            _currentAdornerLayer = AdornerLayer_GetExpectedSharedAncestor(visual);

            AdornerLayer_WalkAdornerParentClipRecursive(visual.AdornedVisual);
            
            _canvas.Transform = originalTransform;
        }
        
        private void AdornerHelper_RenderPostGraphPushAdornerClip(ServerCompositionVisual visual)
        {
            if (SkipAdornerClip(visual))
                return;
            
            if (_adornerPushedClipStack == null)
                return;

            while (_adornerPushedClipStack.Count > 0)
            {
                var op = (Op)_adornerPushedClipStack.Pop();
                if (op == Op.Stop)
                    break;
                if (op == Op.PopGeometryClip)
                    _canvas.PopGeometryClip();
                else if (op == Op.PopClip)
                    _canvas.PopClip();
            }
        }

        private void AdornerHelper_Dispose()
        {
            _pools.IntStackPool.Return(ref _adornerPushedClipStack!);
        }
    }
}