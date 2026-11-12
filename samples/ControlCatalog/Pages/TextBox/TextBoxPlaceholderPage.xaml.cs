using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TextBoxPlaceholderPage : UserControl
    {
        public TextBoxPlaceholderPage()
        {
            InitializeComponent();
        }

        private void OnClearInnerContentBox(object? sender, RoutedEventArgs e)
        {
            InnerContentBox.Clear();
            InnerContentBox.Focus();
        }
    }
}
