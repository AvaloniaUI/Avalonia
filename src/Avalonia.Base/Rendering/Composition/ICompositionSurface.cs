using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    public interface ICompositionSurface
    {
        internal ServerCompositionSurface Server { get; }
    }
}