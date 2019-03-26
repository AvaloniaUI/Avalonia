using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class DropDownPage : UserControl
    {
        public DropDownPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var fontDropDown = this.Find<ComboBox>("fontDropDown");
            fontDropDown.Items = Avalonia.Media.FontFamily.SystemFontFamilies;
            fontDropDown.SelectedIndex = 0;
        }
    }
}
