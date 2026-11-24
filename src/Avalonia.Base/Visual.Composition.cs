using Avalonia.Collections;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.VisualTree;

namespace Avalonia;

public partial class Visual
{
    internal CompositionDrawListVisual? CompositionVisual { get; private set; }
    internal CompositionVisual? ChildCompositionVisual { get; set; }

    // The parts that the renderer must send to the compositor. Not None only while the visual is in the dirty set
    // of the renderer, which also writes it.
    internal VisualDirtyFlags CompositionDirtyFlags { get; set; }
    
    
    private protected virtual CompositionDrawListVisual CreateCompositionVisual(Compositor compositor)
        => new CompositionDrawListVisual(compositor,
            new ServerCompositionDrawListVisual(compositor.Server, this), this);
        
    internal CompositionVisual AttachToCompositor(Compositor compositor)
    {
        if (CompositionVisual == null || CompositionVisual.Compositor != compositor)
        {
            CompositionVisual = CreateCompositionVisual(compositor);
        }

        return CompositionVisual;
    }

    internal virtual void DetachFromCompositor()
    {
        if (CompositionVisual != null)
        {
            if (ChildCompositionVisual != null)
                CompositionVisual.Children.Remove(ChildCompositionVisual);
                
            CompositionVisual.DrawList = null;
            CompositionVisual.OpacityMask = null;
            CompositionVisual = null;
        }
    }

    internal virtual void SynchronizeCompositionChildVisuals()
    {
        if(CompositionVisual == null)
            return;
        var compositionChildren = CompositionVisual.Children;
        var visualChildren = TypedVisualChildren;
        
        PooledList<(Visual visual, int index)>? sortedChildren = null;
        if (HasNonUniformZIndexChildren && visualChildren.Count > 1)
        {
            sortedChildren = new (visualChildren.Count);
            for (var c = 0; c < visualChildren.Count; c++) 
                sortedChildren.Add((visualChildren[c], c));
            
            // Regular Array.Sort is unstable, we need to provide indices as well to avoid reshuffling elements.
            sortedChildren.Sort(static (lhs, rhs) =>
            {
                var result = lhs.visual.ZIndex.CompareTo(rhs.visual.ZIndex);
                return result == 0 ? lhs.index.CompareTo(rhs.index) : result;
            });
        }
        
        var childVisual = ChildCompositionVisual;
        
        // Check if the current visual somehow got migrated to another compositor
        if (childVisual != null && childVisual.Compositor != CompositionVisual.Compositor)
            childVisual = null;
        
        var expectedCount = visualChildren.Count;
        if (childVisual != null)
            expectedCount++;
        
        if (compositionChildren.Count == expectedCount)
        {
            bool mismatch = false;
            if (sortedChildren != null)
                for (var c = 0; c < visualChildren.Count; c++)
                {
                    if (!ReferenceEquals(compositionChildren[c], sortedChildren[c].visual.CompositionVisual))
                    {
                        mismatch = true;
                        break;
                    }
                }
            else
                for (var c = 0; c < visualChildren.Count; c++)
                    if (!ReferenceEquals(compositionChildren[c], visualChildren[c].CompositionVisual))
                    {
                        mismatch = true;
                        break;
                    }

            if (childVisual != null &&
                !ReferenceEquals(compositionChildren[compositionChildren.Count - 1], childVisual))
                mismatch = true;

            if (!mismatch)
            {
                sortedChildren?.Dispose();
                return;
            }
        }
        
        compositionChildren.Clear();
        if (sortedChildren != null)
        {
            foreach (var ch in sortedChildren)
            {
                var compositionChild = ch.visual.CompositionVisual;
                if (compositionChild != null)
                    compositionChildren.Add(compositionChild);
            }
            sortedChildren.Dispose();
        }
        else
            foreach (var ch in visualChildren)
            {
                var compositionChild = ch.CompositionVisual;
                if (compositionChild != null)
                    compositionChildren.Add(compositionChild);
            }

        if (childVisual != null)
            compositionChildren.Add(childVisual);
    }

    internal virtual void SynchronizeCompositionProperties()
    {
        if(CompositionVisual == null)
            return;
        var comp = CompositionVisual;
        var flags = CompositionDirtyFlags;

        if ((flags & VisualDirtyFlags.Offset) != 0)
            comp.Offset = new (Bounds.Left, Bounds.Top, 0);
        if ((flags & VisualDirtyFlags.Size) != 0)
            comp.Size = new (Bounds.Width, Bounds.Height);
        if ((flags & VisualDirtyFlags.Visible) != 0)
            comp.Visible = IsVisible;
        if ((flags & VisualDirtyFlags.Opacity) != 0)
            comp.Opacity = (float)Opacity;
        if ((flags & VisualDirtyFlags.ClipToBounds) != 0)
            comp.ClipToBounds = ClipToBounds;
        if ((flags & VisualDirtyFlags.Clip) != 0)
            comp.Clip = Clip?.PlatformImpl;

        if ((flags & VisualDirtyFlags.OpacityMask) != 0 && !Equals(comp.OpacityMask, OpacityMask))
            comp.OpacityMask = OpacityMask;

        if ((flags & VisualDirtyFlags.CacheMode) != 0)
        {
            var cacheMode = CacheMode?.GetForCompositor(comp.Compositor);
            if (!ReferenceEquals(comp.CacheMode, cacheMode))
                comp.CacheMode = cacheMode;
        }

        if ((flags & VisualDirtyFlags.Effect) != 0 && !comp.Effect.EffectEquals(Effect))
            comp.Effect = Effect?.ToImmutable();

        if ((flags & VisualDirtyFlags.BackdropEffect) != 0)
        {
            if (!comp.BackdropEffect.EffectEquals(BackdropEffect))
                comp.BackdropEffect = BackdropEffect?.ToImmutable();

            comp.BackdropEffectBoundsSource = BackdropEffectBoundsSource;

            var backdropEffectCache = BackdropEffectCache?.GetForCompositor(comp.Compositor);
            if (!ReferenceEquals(comp.BackdropEffectCache, backdropEffectCache))
                comp.BackdropEffectCache = backdropEffectCache;
        }

        if ((flags & VisualDirtyFlags.RenderOptions) != 0)
            comp.RenderOptions = RenderOptions;
        if ((flags & VisualDirtyFlags.TextOptions) != 0)
            comp.TextOptions = TextOptions;

        if ((flags & VisualDirtyFlags.Transform) != 0)
        {
            var renderTransform = Matrix.Identity;

            if (HasMirrorTransform)
                renderTransform = new Matrix(-1.0, 0.0, 0.0, 1.0, Bounds.Width, 0);

            if (RenderTransform != null)
            {
                var origin = RenderTransformOrigin.ToPixels(new Size(Bounds.Width, Bounds.Height));
                var offset = Matrix.CreateTranslation(origin);
                renderTransform *= (-offset) * RenderTransform.Value * (offset);
            }

            comp.TransformMatrix = renderTransform;
        }
    }
}
