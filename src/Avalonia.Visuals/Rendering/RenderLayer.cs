using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class RenderLayer
    {
        private readonly IDrawingContextImpl _drawingContext;

        public RenderLayer(
            IDrawingContextImpl drawingContext,
            Size size,
            double scaling,
            IVisual layerRoot)
        {
            _drawingContext = drawingContext;
            Bitmap = drawingContext.CreateLayer(size);
            Size = size;
            Scaling = scaling;
            LayerRoot = layerRoot;
        }

        public IRenderTargetBitmapImpl Bitmap { get; private set; }
        public double Scaling { get; private set; }
        public Size Size { get; private set; }
        public IVisual LayerRoot { get; }

        public void ResizeBitmap(Size size, double scaling)
        {
            if (Size != size || Scaling != scaling)
            {
                var resized = _drawingContext.CreateLayer(size);

                using (var context = resized.CreateDrawingContext(null))
                {
                    context.Clear(Colors.Transparent);
                    context.DrawImage(Bitmap, 1, new Rect(Size), new Rect(Size));
                    Bitmap.Dispose();
                    Bitmap = resized;
                    Size = size;
                }
            }
        }
    }
}
