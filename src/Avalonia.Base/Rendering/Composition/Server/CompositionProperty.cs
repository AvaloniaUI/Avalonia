using System.Collections.Generic;
using System.Threading;

// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

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