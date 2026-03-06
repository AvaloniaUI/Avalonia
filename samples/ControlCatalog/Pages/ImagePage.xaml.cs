using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ControlCatalog.Pages
{
    public partial class ImagePage : UserControl
    {
        public ImagePage()
        {
            InitializeComponent();
        }

        public void BitmapStretchChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bitmapImage != null)
            {
                var comboxBox = (ComboBox)sender;
                bitmapImage.Stretch = (Stretch)comboxBox.SelectedIndex;
            }
        }

        public void DrawingStretchChanged(object sender, SelectionChangedEventArgs e)
        {
            if (drawingImage != null)
            {
                var comboxBox = (ComboBox)sender;
                drawingImage.Stretch = (Stretch)comboxBox.SelectedIndex;
            }
        }

        public void BitmapCropChanged(object sender, SelectionChangedEventArgs e)
        {
            if (croppedImage != null)
            {
                var comboxBox = (ComboBox)sender;
                if (croppedImage.Source is CroppedBitmap croppedBitmap)
                {
                    croppedBitmap.SourceRect = GetCropRect(comboxBox.SelectedIndex);
                }

            }
        }

        private static PixelRect GetCropRect(int index)
        {
            var bitmapWidth = 640;
            var bitmapHeight = 426;
            var cropSize = new PixelSize(320, 240);
            return index switch
            {
                1 => new PixelRect(new PixelPoint((bitmapWidth - cropSize.Width) / 2, (bitmapHeight - cropSize.Width) / 2), cropSize),
                2 => new PixelRect(new PixelPoint(0, 0), cropSize),
                3 => new PixelRect(new PixelPoint(bitmapWidth - cropSize.Width, 0), cropSize),
                4 => new PixelRect(new PixelPoint(0, bitmapHeight - cropSize.Height), cropSize),
                5 => new PixelRect(new PixelPoint(bitmapWidth - cropSize.Width, bitmapHeight - cropSize.Height), cropSize),
                _ => default
            };

        }
    }
}
