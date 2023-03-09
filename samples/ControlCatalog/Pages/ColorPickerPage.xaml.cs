using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ColorPickerPage : UserControl
    {
        public ColorPickerPage()
        {
            InitializeComponent();

            var layoutRoot = this.GetControl<Grid>("LayoutRoot");

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
            
            layoutRoot.Children.Add(colorPicker);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
