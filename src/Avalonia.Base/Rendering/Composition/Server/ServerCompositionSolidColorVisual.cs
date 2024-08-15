using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSolidColorVisual
{
    protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
    {
        context.Canvas.DrawRectangle(new ImmutableSolidColorBrush(Color), null, new Rect(0, 0, Size.X, Size.Y));
    }
}