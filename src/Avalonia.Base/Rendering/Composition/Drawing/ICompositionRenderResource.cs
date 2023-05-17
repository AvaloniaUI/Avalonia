namespace Avalonia.Rendering.Composition.Drawing;

internal interface ICompositionRenderResource
{
    void AddRefOnCompositor(Compositor c);
    void ReleaseOnCompositor(Compositor c);
}

internal interface ICompositionRenderResource<T> : ICompositionRenderResource where T : class
{
    T GetForCompositor(Compositor c);
}