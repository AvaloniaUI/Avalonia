using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics
{
    static class VisualExtensions
    {
        /// <summary>
        /// Rendered control to stream
        /// </summary>
        /// <param name="source">the control I want to render in the stream</param>
        /// <param name="destination">destination destina</param>
        /// <param name="dpi">(optional) dpi quality default 96</param>
        public static void RenderTo(this IControl source, Stream destination, double dpi = 96)
        {
            var rect = source.Bounds;
            var pixelSize = new PixelSize((int)rect.Width, (int)rect.Height);
            var dpiVector = new Vector(dpi, dpi);

            // get Visual root
            var root = (source.VisualRoot 
                ?? source.GetVisualRoot())
                as IControl;

            // Backup current vaules
            var oldgeometry = root.Clip;
            var oldClipToBounds = root.ClipToBounds;
            var oldRenderTransformOrigin = root.RenderTransformOrigin;
            var oldrenderTransform = root.RenderTransform;

            try
            {
                // Translate coordinate clinet to root
                var top = source.TranslatePoint(new Point(0,0), root);
                var bottomRight = source.TranslatePoint(rect.BottomRight, root);

                // Set clip region
                var clipRegion = new Media.RectangleGeometry(new Rect(top.Value, bottomRight.Value));
                root.ClipToBounds = true;
                root.Clip = clipRegion;
                // Translate origin
                root.RenderTransformOrigin = new RelativePoint(top.Value, RelativeUnit.Absolute);
                root.RenderTransform = new Media.TranslateTransform(-top.Value.X, -top.Value.Y);
                using (var bitmap = new RenderTargetBitmap(pixelSize, dpiVector))
                {
                    bitmap.Render(root);
                    bitmap.Save(destination);
                }

            }
            finally
            {
                // Restore current vaules
                root.ClipToBounds = oldClipToBounds;
                root.Clip = oldgeometry;
                root.RenderTransformOrigin = oldRenderTransformOrigin;
                root.RenderTransform = oldrenderTransform;
            }
        }
    }
}
