using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages;

public partial class SanctuaryShowcasePage : UserControl
{
    public SanctuaryShowcasePage()
    {
        InitializeComponent();
    }

    private void OnPage1CTA(object? sender, RoutedEventArgs e)
    {
        DemoCarousel.SelectedIndex = 1;
    }

    private void OnPage2CTA(object? sender, RoutedEventArgs e)
    {
        DemoCarousel.SelectedIndex = 2;
    }

    private async void OnPage3CTA(object? sender, RoutedEventArgs e)
    {
        var nav = this.FindAncestorOfType<NavigationPage>();
        if (nav == null)
            return;

        var previousPages = nav.NavigationStack.ToList();
        var mainPage = new ContentPage { Content = new SanctuaryMainPage() };
        NavigationPage.SetHasNavigationBar(mainPage, false);
        await nav.PushAsync(mainPage);
        foreach (var page in previousPages)
            nav.RemovePage(page);
    }
}
