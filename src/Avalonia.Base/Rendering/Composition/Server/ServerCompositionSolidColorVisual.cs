using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSolidColorVisual
{
    protected override void RenderCore(CompositorDrawingContextProxy canvas, LtrbRect currentTransformedClip,
        IDirtyRectTracker dirtyRects)
    {
        canvas.DrawRectangle(new ImmutableSolidColorBrush(Color), null, new Rect(0, 0, Size.X, Size.Y));
    }
}