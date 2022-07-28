// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition.Server
{
    internal abstract class ServerCompositionSurface : ServerObject
    {
        protected ServerCompositionSurface(ServerCompositor compositor) : base(compositor)
        {
        }
    }
}