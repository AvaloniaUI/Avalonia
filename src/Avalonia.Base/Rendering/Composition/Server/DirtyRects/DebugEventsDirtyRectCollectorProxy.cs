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

    public LtrbRect? UninflatedCombinedIntersect(LtrbRect rect) => inner.UninflatedCombinedIntersect(rect);
    public bool UninflatedIntersects(LtrbRect rect) => inner.UninflatedIntersects(rect);
}