using Avalonia.Controls;
using Avalonia.Media;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ComboBoxPage : UserControl
    {
        public ComboBoxPage()
        {
            InitializeComponent();
            fontComboBox.ItemsSource = FontManager.Current.SystemFonts;
            fontComboBox.SelectedIndex = 0;
            DataContext = new ComboBoxPageViewModel();
        }
    }
}
