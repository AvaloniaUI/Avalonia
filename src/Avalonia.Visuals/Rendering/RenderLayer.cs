using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class RenderLayer
    {
        private IRenderTarget _parent;

        public RenderLayer(
            IRenderTarget parent,
            Size size,
            double scaling,
            IVisual layerRoot)
        {
            _parent = parent;
            Bitmap = parent.CreateLayer(
                (int)(size.Width * scaling),
                (int)(size.Height * scaling));
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
                var resized = _parent.CreateLayer(
                    (int)(size.Width * scaling),
                    (int)(size.Height * scaling));

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
