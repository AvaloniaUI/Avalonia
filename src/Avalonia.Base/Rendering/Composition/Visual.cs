using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// The base visual object in the composition visual hierarchy.
    /// </summary>
    public abstract partial class CompositionVisual
    {
        private IBrush? _opacityMask;

        private protected virtual void OnRootChangedCore()
        {
        }

        partial void OnRootChanged() => OnRootChangedCore();

        partial void OnParentChanged() => Root = Parent?.Root;

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

        internal Matrix? TryGetServerGlobalTransform()
        {
            if (Root == null)
                return null;
            var i = Root.Server.Readback;
            ref var readback = ref Server.GetReadback(i.ReadIndex);
            
            // CompositionVisual wasn't visible or wasn't even attached to the composition target during the lat frame
            if (!readback.Visible || readback.Revision < i.ReadRevision)
                return null;
            
            // CompositionVisual was reparented (potential race here)
            if (readback.TargetId != Root.Server.Id)
                return null;
            
            return readback.Matrix;
        }
        
        internal object? Tag { get; set; }

        internal virtual bool HitTest(Point point) => true;
    }
}
