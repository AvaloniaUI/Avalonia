using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class DebugEventsDirtyRectCollectorProxy(IDirtyRectCollector inner, ICompositionTargetDebugEvents events)
    : IDirtyRectCollector
{
    public void AddRect(LtrbRect rect)
    {
        inner.AddRect(rect);
        events.RectInvalidated(rect);
    }
}