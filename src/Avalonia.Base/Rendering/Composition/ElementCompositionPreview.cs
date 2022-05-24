namespace Avalonia.Rendering.Composition;

public static class ElementCompositionPreview
{
    public static CompositionVisual? GetElementVisual(Visual visual) => visual.CompositionVisual;
}