using System.Numerics;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    internal partial class ServerCompositionSpriteVisual
    {

        protected override void RenderCore(CompositorDrawingContextProxy canvas, Matrix4x4 transform)
        {
            if (Brush != null)
            {
                //SetTransform(canvas, transform);
                //canvas.FillRect((Vector2)Size, (ICbBrush)Brush.Brush!);
            }

            base.RenderCore(canvas, transform);
        }
    }
}