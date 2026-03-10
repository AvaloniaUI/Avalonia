using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages;

public partial class SanctuaryShowcasePage : UserControl
{
    private static readonly IBrush ActiveDotBrush = new SolidColorBrush(Color.Parse("#f47b25"));
    private static readonly IBrush InactiveDotBrush = new SolidColorBrush(Color.Parse("#4DFFFFFF"));

    private Border[]? _dots;

    public SanctuaryShowcasePage()
    {
        InitializeComponent();
        DemoCarousel.PropertyChanged += OnCarouselPropertyChanged;
    }

    private void OnCarouselPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SelectingMultiPage.SelectedIndexProperty)
        {
            UpdateDots(e.GetNewValue<int>());
        }
    }

    private void UpdateDots(int activeIndex)
    {
        _dots ??= [Dot0, Dot1, Dot2];

        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].Width = i == activeIndex ? 32 : 8;
            _dots[i].Background = i == activeIndex ? ActiveDotBrush : InactiveDotBrush;
        }
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

    private void OnDotClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out var index))
        {
            DemoCarousel.SelectedIndex = index;
        }
    }
}
