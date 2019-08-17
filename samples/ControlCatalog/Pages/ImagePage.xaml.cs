using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace ControlCatalog.Pages
{
    public class ImagePage : UserControl
    {
        private Image iconImage;
        private WriteableBitmap writeableBitmap = new WriteableBitmap(new PixelSize(300, 200), new Vector(96, 96));

        public ImagePage()
        {
            this.InitializeComponent();
        }

        private unsafe void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            iconImage = this.Get<Image>("Icon");
            this.Get<Image>("WriteableBitmapImage").Source = writeableBitmap;

            using (var framebuffer = writeableBitmap.Lock())
            {
                byte* ptr = (byte*)framebuffer.Address.ToPointer();

                for (int y = 0; y < framebuffer.Size.Height; ++y)
                {
                    for (int x = 0; x < framebuffer.Size.Width; ++x)
                    {
                        int offset = y * framebuffer.RowBytes + x * 4;

                        ptr[offset + 0] = (byte)(((double)x / framebuffer.Size.Width) * 255);
                        ptr[offset + 1] = (byte)(((double)y / framebuffer.Size.Height) * 255);
                        ptr[offset + 3] = 255;
                    }
                }
            }
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
