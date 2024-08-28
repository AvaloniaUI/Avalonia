using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ContainerQueryPage : UserControl
    {
        private TextBlock? _borderLabel;
        private Border? _border;

        public ContainerQueryPage()
        {
            this.InitializeComponent();
            _borderLabel = this.Find<TextBlock>("borderSize");
            _border = this.Find<Border>("border");
            this.SizeChanged += Container_SizeChanged;
        }

        private void Container_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (_border != null && _borderLabel != null)
                _borderLabel.Text = $"Border Size: {_border.Bounds.Height} x {_border.Bounds.Width}";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
