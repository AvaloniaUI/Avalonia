using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class ButtonsPage : UserControl
    {
        private int repeatButtonClickCount = 0;

        public ButtonsPage()
        {
            InitializeComponent();
        }

        public void OnRepeatButtonClick(object? sender, RoutedEventArgs args)
        {
            repeatButtonClickCount++;
            RepeatButtonTextBlock.Text = $"Repeat Button: {repeatButtonClickCount}";
        }
    }
}
