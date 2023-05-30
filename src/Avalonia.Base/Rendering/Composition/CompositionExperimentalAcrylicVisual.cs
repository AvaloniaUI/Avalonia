using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

internal partial class CompositionExperimentalAcrylicVisual
{
    internal CompositionExperimentalAcrylicVisual(Compositor compositor, Visual visual) : base(compositor,
        new ServerCompositionExperimentalAcrylicVisual(compositor.Server, visual), visual)
    {
    }
}