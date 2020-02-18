using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages
{
    public class ImagePage : UserControl
    {
        private readonly Image _bitmapImage;
        private readonly Image _drawingImage;
        private readonly Image _croppedImage;
        private readonly IBitmap _croppedBitmapSource;

        public ImagePage()
        {
            InitializeComponent();
            _bitmapImage = this.FindControl<Image>("bitmapImage");
            _drawingImage = this.FindControl<Image>("drawingImage");
            _croppedImage = this.FindControl<Image>("croppedImage");
            _croppedBitmapSource = LoadBitmap("avares://ControlCatalog/Assets/delicate-arch-896885_640.jpg");
            _croppedImage.Source = new CroppedBitmap(_croppedBitmapSource, default);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void BitmapStretchChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_bitmapImage != null)
            {
                var comboxBox = (ComboBox)sender;
                _bitmapImage.Stretch = (Stretch)comboxBox.SelectedIndex;
            }
        }

        public void DrawingStretchChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_drawingImage != null)
            {
                var comboxBox = (ComboBox)sender;
                _drawingImage.Stretch = (Stretch)comboxBox.SelectedIndex;
            }
        }

        public void BitmapCropChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_croppedImage != null)
            {
                var comboxBox = (ComboBox)sender;
                _croppedImage.Source =  new CroppedBitmap( _croppedBitmapSource,  GetCropRect(comboxBox.SelectedIndex));
            }
        }

        private PixelRect GetCropRect(int index)
        {
            var bitmapWidth = _croppedBitmapSource.PixelSize.Width;
            var bitmapHeight = _croppedBitmapSource.PixelSize.Height;
            var cropSize = new PixelSize(bitmapWidth / 2, bitmapHeight / 2);
            return index switch
            {
                1 => new PixelRect(new PixelPoint((bitmapWidth - cropSize.Width) / 2, (bitmapHeight - cropSize.Width) / 2), cropSize),
                2 => new PixelRect(new PixelPoint(0, 0), cropSize),
                3 => new PixelRect(new PixelPoint(bitmapWidth - cropSize.Width, 0), cropSize),
                4 => new PixelRect(new PixelPoint(0, bitmapHeight - cropSize.Height), cropSize),
                5 => new PixelRect(new PixelPoint(bitmapWidth - cropSize.Width, bitmapHeight - cropSize.Height), cropSize),
                _ => PixelRect.Empty
            };
            
        }

        private IBitmap LoadBitmap(string uri)
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            return new Bitmap(assets.Open(new Uri(uri)));
        }
    }
}
