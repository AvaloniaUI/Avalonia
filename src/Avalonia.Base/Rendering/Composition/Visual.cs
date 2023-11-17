using System;
using System.Numerics;
using Avalonia.Media;
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
                if (_opacityMask == value)
                    return;
                OpacityMaskBrush = (_opacityMask = value)?.ToImmutable();
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
