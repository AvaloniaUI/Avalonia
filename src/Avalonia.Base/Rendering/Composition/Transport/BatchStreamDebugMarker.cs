using System;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Transport;

internal class BatchStreamDebugMarkers
{
    public static object ObjectEndMarker = new object();
    public static Guid ObjectEndMagic = Guid.NewGuid();
}
