using Avalonia.Media.Immutable;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSolidColorVisual
{
    protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
    {
        canvas.DrawRectangle(new ImmutableSolidColorBrush(Color), null, new Rect(0, 0, Size.X, Size.Y));
    }
}