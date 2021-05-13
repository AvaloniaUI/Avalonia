using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class ViewboxPage : UserControl
    {
        private readonly Viewbox _viewbox;
        private readonly ComboBox _stretchSelector;

        public ViewboxPage()
        {
            InitializeComponent();

            _viewbox = this.FindControl<Viewbox>("Viewbox");

            _stretchSelector = this.FindControl<ComboBox>("StretchSelector");

            _stretchSelector.Items = new[]
            {
                Stretch.Uniform, Stretch.UniformToFill, Stretch.Fill, Stretch.None
            };

            _stretchSelector.SelectedIndex = 0;

            var stretchDirectionSelector = this.FindControl<ComboBox>("StretchDirectionSelector");

            stretchDirectionSelector.Items = new[]
            {
                StretchDirection.Both, StretchDirection.DownOnly, StretchDirection.UpOnly
            };

            stretchDirectionSelector.SelectedIndex = 0;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void StretchSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewbox.Stretch = (Stretch) _stretchSelector.SelectedItem!;
        }
    }
}
