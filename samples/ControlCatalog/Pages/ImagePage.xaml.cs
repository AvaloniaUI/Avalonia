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
        public ImagePage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            iconImage = this.Get<Image>("Icon");
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
