using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// The base visual object in the composition visual hierarchy.
    /// </summary>
    public abstract partial class CompositionVisual
    {
        private IBrush? _opacityMask;
        protected int CustomHitTestCountInSubTree;
        public bool DisableSubTreeBoundsHitTestOptimization => CustomHitTestCountInSubTree != 0;
        
        private protected virtual void OnRootChangedCore()
        {
        }

        partial void OnRootChanged()
        {
            OnRootChangedCore();
            Root?.InvalidateHitTestIndex();
        }

            partial void OnRootChanging() => Root?.InvalidateHitTestIndex();

        partial void OnParentChanging()
        {
            // Propagate the blight
            if (CustomHitTestCountInSubTree != 0)
            {
                var parent = Parent;
                while (parent != null)
                {
                    parent.CustomHitTestCountInSubTree -= CustomHitTestCountInSubTree;
                    parent = parent.Parent;
                }
            }
        }
        
        partial void OnParentChanged()
        {
            Root = Parent?.Root;
            // Propagate the blight
            if (CustomHitTestCountInSubTree != 0)
            {
                var parent = Parent;
                while (parent != null)
                {
                    parent.CustomHitTestCountInSubTree -= CustomHitTestCountInSubTree;
                    parent = parent.Parent;
                }
            }
        }


        partial void OnVisibleChanged() => Root?.InvalidateHitTestIndex();
        partial void OnClipChanged() => Root?.InvalidateHitTestIndex();
        partial void OnClipToBoundsChanged() => Root?.InvalidateHitTestIndex();
        partial void OnOffsetChanged() => Root?.InvalidateHitTestIndex();
        partial void OnSizeChanged() => Root?.InvalidateHitTestIndex();
        partial void OnAnchorPointChanged() => Root?.InvalidateHitTestIndex();
        partial void OnCenterPointChanged() => Root?.InvalidateHitTestIndex();
        partial void OnRotationAngleChanged() => Root?.InvalidateHitTestIndex();
        partial void OnOrientationChanged() => Root?.InvalidateHitTestIndex();
        partial void OnScaleChanged() => Root?.InvalidateHitTestIndex();
        partial void OnTransformMatrixChanged() => Root?.InvalidateHitTestIndex();
        partial void OnAdornedVisualChanged() => Root?.InvalidateHitTestIndex();
        partial void OnAdornerIsClippedChanged() => Root?.InvalidateHitTestIndex();

        public IBrush? OpacityMask
        {
            get => _opacityMask;
            set
            {
                if (ReferenceEquals(_opacityMask, value))
                    return;
                
                // Release the previous compositor-resource based brush
                if (_opacityMask is ICompositionRenderResource<IBrush> oldCompositorBrush)
                {
                    oldCompositorBrush.ReleaseOnCompositor(Compositor);
                    _opacityMask = null;
                    OpacityMaskBrushTransportField = null;
                }

                if (value is ICompositionRenderResource<IBrush> newCompositorBrush)
                {
                    newCompositorBrush.AddRefOnCompositor(Compositor);
                    OpacityMaskBrushTransportField = newCompositorBrush.GetForCompositor(Compositor);
                    _opacityMask = value;
                }
                else
                    OpacityMaskBrushTransportField = (_opacityMask = value)?.ToImmutable();
            }
        }

        internal ServerCompositionVisual.ReadbackData? TryGetValidReadback()
        {
            if (Root == null)
                return null;
            var i = Server.Compositor.Readback;
            var readback = Server.GetReadback(i.ReadRevision);
            if (readback == null)
                return null;
            
            // CompositionVisual wasn't visible or wasn't even attached to the composition target during the lat frame
            if (!readback.Visible || readback.TargetId != Root.Server.Id)
                return null;
            
            // CompositionVisual was reparented (potential race here)
            if (readback.TargetId != Root.Server.Id)
                return null;

            return readback;
        }
        
        internal object? Tag { get; set; }

        internal virtual bool HitTest(Point point) => true;
    }
}
