using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

internal interface ICompositorSerializable
{
    SimpleServerObject? TryGetServer(Compositor c);
    void SerializeChanges(Compositor c, BatchStreamWriter writer);
}