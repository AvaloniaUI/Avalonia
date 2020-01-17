using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class ImagePage : UserControl
    {
        private readonly Image _bitmapImage;
        private readonly Image _drawingImage;

        public ImagePage()
        {
            InitializeComponent();
            _bitmapImage = this.FindControl<Image>("bitmapImage");
            _drawingImage = this.FindControl<Image>("drawingImage");
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
    }
}
