using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Threading;

namespace RenderDemo.Pages
{
    public class WriteableBitmapPage : Control
    {
        private WriteableBitmap _unpremulBitmap;
        private WriteableBitmap _premulBitmap;
        private readonly Stopwatch _st = Stopwatch.StartNew();

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _unpremulBitmap = new WriteableBitmap(new PixelSize(256, 256), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            _premulBitmap = new WriteableBitmap(new PixelSize(256, 256), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            _unpremulBitmap?.Dispose();
            _unpremulBitmap = null;

            _premulBitmap?.Dispose();
            _unpremulBitmap = null;
        }

        public override void Render(DrawingContext context)
        {
            void FillPixels(WriteableBitmap bitmap, byte fillAlpha, bool premul)
            {
                using (var fb = bitmap.Lock())
                {
                    var data = new int[fb.Size.Width * fb.Size.Height];

                    for (int y = 0; y < fb.Size.Height; y++)
                    {
                        for (int x = 0; x < fb.Size.Width; x++)
                        {
                            var color = new Color(fillAlpha, 0, 255, 0);

                            if (premul)
                            {
                                byte r = (byte) (color.R * color.A / 255);
                                byte g = (byte) (color.G * color.A / 255);
                                byte b = (byte) (color.B * color.A / 255);

                                color = new Color(fillAlpha, r, g, b);
                            }

                            data[y * fb.Size.Width + x] = (int) color.ToUInt32();
                        }
                    }

                    Marshal.Copy(data, 0, fb.Address, fb.Size.Width * fb.Size.Height);
                }
            }

            base.Render(context);

            byte alpha = (byte)((_st.ElapsedMilliseconds / 10) % 256);

            FillPixels(_unpremulBitmap, alpha, false);
            FillPixels(_premulBitmap, alpha, true);

            context.FillRectangle(Brushes.Red, new Rect(0, 0, 256 * 3, 256));

            context.DrawImage(_unpremulBitmap,
                new Rect(0, 0, 256, 256),
                new Rect(0, 0, 256, 256));

            context.DrawImage(_premulBitmap,
                new Rect(0, 0, 256, 256),
                new Rect(256, 0, 256, 256));

            context.FillRectangle(new ImmutableSolidColorBrush(Colors.Lime, alpha / 255d), new Rect(512, 0, 256, 256));

            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }
    }
}
