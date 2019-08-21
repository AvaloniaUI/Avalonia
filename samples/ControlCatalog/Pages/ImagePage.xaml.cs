using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class ImagePage : UserControl
    {
        private Image iconImage;
        private WriteableBitmap writeableBitmap = new WriteableBitmap(new PixelSize(300, 200), new Vector(96, 96));
        private Image _image;

        public ImagePage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            iconImage = this.Get<Image>("Icon");
            _image = this.Get<Image>("WriteableBitmapImage");
            _image.Source = writeableBitmap;

            using (var context = new FramebufferContext(writeableBitmap))
            {
                for (int y = 0; y < context.Framebuffer.Size.Height; ++y)
                {
                    for (int x = 0; x < context.Framebuffer.Size.Width; ++x)
                    {
                        var pixel = context.GetPixel(x, y);

                        pixel[0] = (byte)(((double)x / context.Framebuffer.Size.Width) * 255);
                        pixel[1] = (byte)(((double)y / context.Framebuffer.Size.Height) * 255);
                        pixel[3] = 255;
                    }
                }
            }

            var timer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Background, Callback);

            timer.Start();
        }

        private Random random = new Random();

        private void Callback(object sender, EventArgs e)
        {
            using (var context = new FramebufferContext(writeableBitmap))
            {
                for (int y = 80; y < 120; ++y)
                {
                    for (int x = 125; x < 175; ++x)
                    {
                        var pixel = context.GetPixel(x, y);

                        pixel[0] = (byte)(random.NextDouble() * 255);
                        pixel[1] = (byte)(random.NextDouble() * 255);
                        pixel[3] = 255;
                    }
                }
            }

            _image.InvalidateVisual();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (iconImage.Source == null)
            {
                var windowRoot = e.Root as Window;
                if (windowRoot != null)
                {
                    using (var stream = new MemoryStream())
                    {
                        windowRoot.Icon.Save(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        iconImage.Source = new Bitmap(stream);
                    }
                }
            }
        }
    }
}
