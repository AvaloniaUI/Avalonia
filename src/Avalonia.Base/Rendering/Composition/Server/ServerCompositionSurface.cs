namespace Avalonia.Rendering.Composition.Server
{
    internal abstract class ServerCompositionSurface : ServerObject
    {
        protected ServerCompositionSurface(ServerCompositor compositor) : base(compositor)
        {
        }
    }
}