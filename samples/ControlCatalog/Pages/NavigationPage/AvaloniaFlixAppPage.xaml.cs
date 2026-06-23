using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class AvaloniaFlixAppPage : UserControl
{
    NavigationPage? _detailNav;
    ScrollViewer? _infoPanel;

    public AvaloniaFlixAppPage()
    {
        InitializeComponent();

        _detailNav = this.FindControl<NavigationPage>("DetailNav");
        if (_detailNav != null)
        {
            _detailNav.ModalTransition = new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical);

            var homeView = new AvaloniaFlixHomeView();
            homeView.MovieSelected   = title => PushDetailPage(title);
            homeView.SearchRequested = () => _ = PushSearchPageAsync();

            var homePage = new ContentPage
            {
                Content    = homeView,
                Background = Brushes.Transparent,
                Header     = BuildHomeHeader(),
            };
            NavigationPage.SetTopCommandBar(homePage, BuildHomeCommandBar());
            _ = _detailNav.PushAsync(homePage);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoPanelVisibility();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty)
            UpdateInfoPanelVisibility();
    }

    void UpdateInfoPanelVisibility()
    {
        if (_infoPanel != null)
            _infoPanel.IsVisible = Bounds.Width >= 650;
    }

    TextBlock BuildHomeHeader() => new TextBlock
    {
        Text = "AVALONIAFLIX",
        Foreground = new SolidColorBrush(Color.Parse("#E50914")),
        FontSize = 18,
        FontWeight = Avalonia.Media.FontWeight.Black,
        VerticalAlignment = VerticalAlignment.Center,
    };

    StackPanel BuildHomeCommandBar()
    {
        var cmdBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var searchBtn = new Button { Padding = new Thickness(4) };
        searchBtn.Classes.Add("flixTransparent");
        searchBtn.Click += OnSearchClick;
        searchBtn.Content = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Avalonia.Media.Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
        };
        cmdBar.Children.Add(searchBtn);
        cmdBar.Children.Add(new Border
        {
            Width = 30, Height = 30, CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.Parse("#333333")),
            Child = new TextBlock
            {
                Text = "JD", FontSize = 10, FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            },
        });
        return cmdBar;
    }

    async void PushDetailPage(string title)
    {
        if (_detailNav == null) return;

        var detailView = new AvaloniaFlixDetailView(title);

        var headerTitle = new TextBlock
        {
            Text = title, FontSize = 17, FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center,
        };

        var shareBtnContent = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Avalonia.Media.Geometry.Parse("M18,16.08C17.24,16.08 16.56,16.38 16.04,16.85L8.91,12.7C8.96,12.47 9,12.24 9,12C9,11.76 8.96,11.53 8.91,11.3L15.96,7.19C16.5,7.69 17.21,8 18,8A3,3 0 0,0 21,5A3,3 0 0,0 18,2A3,3 0 0,0 15,5C15,5.24 15.04,5.47 15.09,5.7L8.04,9.81C7.5,9.31 6.79,9 6,9A3,3 0 0,0 3,12A3,3 0 0,0 6,15C6.79,15 7.5,14.69 8.04,14.19L15.16,18.35C15.11,18.56 15.08,18.78 15.08,19C15.08,20.61 16.39,21.92 18,21.92C19.61,21.92 20.92,20.61 20.92,19C20.92,17.39 19.61,16.08 18,16.08Z"),
        };
        var shareBtn = new Button { Padding = new Thickness(8), Content = shareBtnContent };
        shareBtn.Classes.Add("flixTransparent");

        var bookmarkBtnContent = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Avalonia.Media.Geometry.Parse("M17,3H7A2,2 0 0,0 5,5V21L12,18L19,21V5C19,3.89 18.1,3 17,3Z"),
        };
        var bookmarkBtn = new Button { Padding = new Thickness(8), Content = bookmarkBtnContent };
        bookmarkBtn.Classes.Add("flixTransparent");

        var detailCmdBar = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        detailCmdBar.Children.Add(shareBtn);
        detailCmdBar.Children.Add(bookmarkBtn);

        var detailPage = new ContentPage
        {
            Content    = detailView,
            Background = Brushes.Transparent,
            Header     = headerTitle,
        };
        NavigationPage.SetTopCommandBar(detailPage, detailCmdBar);

        await _detailNav.PushAsync(detailPage);

        var drawer = this.FindControl<DrawerPage>("DrawerPageControl");
        if (drawer is { IsOpen: true })
            drawer.IsOpen = false;
    }

    async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        await PushSearchPageAsync();
    }

    async Task PushSearchPageAsync()
    {
        if (_detailNav == null) return;

        var searchView = new AvaloniaFlixSearchView();
        searchView.CloseRequested = async () => await (_detailNav?.PopModalAsync() ?? Task.CompletedTask);
        searchView.MovieSelected  = async title =>
        {
            if (_detailNav != null && _detailNav.ModalStack.Count > 0)
                await _detailNav.PopModalAsync();
            PushDetailPage(title);
        };

        var searchPage = new ContentPage
        {
            Content    = searchView,
            Background = new SolidColorBrush(Color.Parse("#0A0A0A")),
        };
        NavigationPage.SetHasNavigationBar(searchPage, false);

        await (_detailNav?.PushModalAsync(searchPage) ?? Task.CompletedTask);
    }

    void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        var drawer = this.FindControl<DrawerPage>("DrawerPageControl");
        if (drawer != null)
            drawer.IsOpen = false;

        _ = _detailNav?.PopToRootAsync();
    }
}
