using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public class CompositionSurface : CompositionObject
{
    internal CompositionSurface(Compositor compositor, ServerObject server) : base(compositor, server)
    {
    }
}