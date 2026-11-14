using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class WindowCustomizationsPage : ContentPage
    {
        public WindowCustomizationsPage()
        {
            InitializeComponent();
        }

        private void OpenTrafficLightPlaceholder(object? sender, RoutedEventArgs e) =>
            new TrafficLightPlaceholderWindow().Show();
    }
}
