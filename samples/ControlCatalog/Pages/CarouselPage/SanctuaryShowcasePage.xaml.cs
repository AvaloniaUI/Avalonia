using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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

        var carouselWrapper = nav.NavigationStack.LastOrDefault();

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
        headerGrid.Children.Add(new TextBlock
        {
            Text = "Sanctuary",
            VerticalAlignment = VerticalAlignment.Center
        });
        var closeIcon = Geometry.Parse(
            "M4.397 4.397a1 1 0 0 1 1.414 0L12 10.585l6.19-6.188a1 1 0 0 1 1.414 1.414L13.413 12l6.19 6.189a1 1 0 0 1-1.414 1.414L12 13.413l-6.189 6.19a1 1 0 0 1-1.414-1.414L10.585 12 4.397 5.811a1 1 0 0 1 0-1.414z");
        var closeBtn = new Button
        {
            Content = new PathIcon { Data = closeIcon },
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(closeBtn, 1);
        headerGrid.Children.Add(closeBtn);
        closeBtn.Click += async (_, _) => await nav.PopAsync(null);

        var mainPage = new ContentPage
        {
            Header = headerGrid,
            Content = new SanctuaryMainPage()
        };
        NavigationPage.SetHasBackButton(mainPage, false);

        await nav.PushAsync(mainPage);

        if (carouselWrapper != null)
        {
            nav.RemovePage(carouselWrapper);
        }
    }
}
