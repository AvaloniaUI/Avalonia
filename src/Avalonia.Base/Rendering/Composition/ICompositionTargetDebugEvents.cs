namespace Avalonia.Rendering.Composition;

internal interface ICompositionTargetDebugEvents
{
    int RenderedVisuals { get; }
    void IncrementRenderedVisuals();
    void RectInvalidated(Rect rc);
}
