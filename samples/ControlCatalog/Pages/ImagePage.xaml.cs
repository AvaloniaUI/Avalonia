using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class ImagePage : UserControl
    {
        private Image iconImage;
        private readonly WriteableBitmap writeableBitmap =
            new WriteableBitmap(new PixelSize(300, 200), new Vector(96, 96));
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

            using (var framebuffer = writeableBitmap.Lock())
            {
                for (var y = 0; y < framebuffer.Size.Height; ++y)
                {
                    for (var x = 0; x < framebuffer.Size.Width; ++x)
                    {
                        var color = new Color(255, (byte)((double)x / framebuffer.Size.Width * 255),
                            (byte)((double)y / framebuffer.Size.Height * 255), 0);

                        framebuffer.SetPixel(x, y, color);
                    }
                }
            }

            var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, Callback);

            timer.Start();
        }

        private Random random = new Random();

        private void Callback(object sender, EventArgs e)
        {
            var color = new Color((byte)(random.NextDouble() * 255), (byte)(random.NextDouble() * 255),
                (byte)(random.NextDouble() * 255), (byte)(random.NextDouble() * 255));

            using (var framebuffer = writeableBitmap.Lock())
            {
                for (var y = 80; y < 120; ++y)
                {
                    for (var x = 125; x < 175; ++x)
                    {
                        framebuffer.SetPixel(x, y, color);
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
