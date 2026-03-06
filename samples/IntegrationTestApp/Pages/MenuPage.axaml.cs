using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class MenuPage : UserControl
{
    public MenuPage()
    {
        InitializeComponent();
    }

    private void MenuClicked(object? sender, RoutedEventArgs e)
    {
        var clickedMenuItemTextBlock = ClickedMenuItem;
        clickedMenuItemTextBlock.Text = (sender as MenuItem)?.Header?.ToString();
    }


    private void MenuClickedMenuItemReset_Click(object? sender, RoutedEventArgs e)
    {
        ClickedMenuItem.Text = "None";
    }
}
