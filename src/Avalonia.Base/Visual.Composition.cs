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
            CompositionVisual = null;
        }
    }

    internal virtual void SynchronizeCompositionChildVisuals()
    {
        if(CompositionVisual == null)
            return;
        var compositionChildren = CompositionVisual.Children;
        var visualChildren = (AvaloniaList<Visual>)VisualChildren;
        
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
        
        // TODO: Introduce a dirty mask like WPF has, so we don't overwrite properties every time
        
        comp.Offset = new (Bounds.Left, Bounds.Top, 0);
        comp.Size = new (Bounds.Width, Bounds.Height);
        comp.Visible = IsVisible;
        comp.Opacity = (float)Opacity;
        comp.ClipToBounds = ClipToBounds;
        comp.Clip = Clip?.PlatformImpl;
        
        if (!Equals(comp.OpacityMask, OpacityMask))
            comp.OpacityMask = OpacityMask?.ToImmutable();

        if (!comp.Effect.EffectEquals(Effect))
            comp.Effect = Effect?.ToImmutable();

        comp.RenderOptions = RenderOptions;

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