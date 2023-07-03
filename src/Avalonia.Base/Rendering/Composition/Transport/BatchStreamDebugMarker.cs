using System;

namespace Avalonia.Rendering.Composition.Transport;

internal class BatchStreamDebugMarkers
{
    public static object ObjectEndMarker = new object();
    public static Guid ObjectEndMagic = Guid.NewGuid();
}
