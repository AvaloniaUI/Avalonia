using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

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
            fontComboBox.Items = FontManager.Current.GetInstalledFontFamilyNames().Select(x => new FontFamily(x));
            fontComboBox.SelectedIndex = 0;
        }
    }
}
