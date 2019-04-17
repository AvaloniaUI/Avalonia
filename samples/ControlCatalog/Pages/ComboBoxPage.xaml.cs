using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ComboBoxPage : UserControl
    {
        public ComboBoxPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var fontComboBox = this.Find<ComboBox>("fontComboBox");
            fontComboBox.Items = Avalonia.Media.FontFamily.SystemFontFamilies;
            fontComboBox.SelectedIndex = 0;
        }
    }
}
