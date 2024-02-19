using System;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Composition;

public static class CompositionGlContextExtensions
{
    public static ICompositionGlSwapchain CreateSwapchain(this ICompositionGlContext context, Visual visual, PixelSize size)
    {
        if (visual.CompositionVisual == null)
            throw new InvalidOperationException("Visual isn't attached to composition tree");
        if (visual.CompositionVisual.Compositor != context.Compositor)
            throw new InvalidOperationException("Visual is attached to a different compositor");

        var surface = context.Compositor.CreateDrawingSurface();
        var surfaceVisual = context.Compositor.CreateSurfaceVisual();
        surfaceVisual.Surface = surface;

        void Resize() => surfaceVisual!.Size = new Vector(visual.Bounds.Width, visual.Bounds.Height);
        ElementComposition.SetElementChildVisual(visual, surfaceVisual);

        void OnVisualOnPropertyChanged(object? s, AvaloniaPropertyChangedEventArgs e) => Resize();
        visual.PropertyChanged += OnVisualOnPropertyChanged;

        void Dispose()
        {
            visual.PropertyChanged -= OnVisualOnPropertyChanged;
            ElementComposition.SetElementChildVisual(visual, null);
        }

        Resize();
        bool success = false;
        try
        {
            var res = context.CreateSwapchain(surface, size, Dispose);
            success = true;
            return res;
        }
        finally
        {
            if(!success)
                Dispose();
        }
    }
}