using System;

// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition.Transport;

internal class BatchStreamDebugMarkers
{
    public static object ObjectEndMarker = new object();
    public static Guid ObjectEndMagic = Guid.NewGuid();
}