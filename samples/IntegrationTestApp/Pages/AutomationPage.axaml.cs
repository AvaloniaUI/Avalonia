using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class AutomationPage : UserControl
{
    public AutomationPage()
    {
        InitializeComponent();
    }

    private void OnButtonAddSomeText(object? sender, RoutedEventArgs? e)
    {
        textLiveRegion.Text += " Lorem ipsum.";
    }
}
