using System.Collections.Generic;
using System.Threading;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Server;

internal class CompositionProperty
{
    private static volatile int s_NextId = 1;
    public int Id { get; private set; }

    public static CompositionProperty Register() => new()
    {
        Id = Interlocked.Increment(ref s_NextId)
    };
}
