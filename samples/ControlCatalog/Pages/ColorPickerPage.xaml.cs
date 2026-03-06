using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ColorPickerPage : UserControl
    {
        public ColorPickerPage()
        {
            InitializeComponent();

            // ColorPicker added from code-behind
            var colorPicker = new ColorPicker()
            {
                Color = Colors.Blue,
                Margin = new Thickness(0, 50, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Palette = new MaterialHalfColorPalette(),
            };
            Grid.SetColumn(colorPicker, 2);
            Grid.SetRow(colorPicker, 1);

            LayoutRoot.Children.Add(colorPicker);
        }
    }
}
