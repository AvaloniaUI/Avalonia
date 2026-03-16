using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class RetroGamingAppPage : UserControl
{
    static readonly Color BgColor      = Color.Parse("#120a1f");
    static readonly Color SurfaceColor = Color.Parse("#2d1b4e");
    static readonly Color CyanColor    = Color.Parse("#00ffff");
    static readonly Color YellowColor  = Color.Parse("#ffff00");
    static readonly Color MutedColor   = Color.Parse("#7856a8");
    static readonly Color TextColor    = Color.Parse("#e0d0ff");

    NavigationPage? _nav;
    ScrollViewer?   _infoPanel;

    public RetroGamingAppPage()
    {
        InitializeComponent();

        _nav = this.FindControl<NavigationPage>("RetroNav");
        if (_nav != null)
            _ = _nav.PushAsync(BuildHomePage());
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

    void ApplyHomeNavigationBarAppearance()
    {
        if (_nav == null)
            return;

        _nav.Resources["NavigationBarBackground"] = new SolidColorBrush(SurfaceColor);
        _nav.Resources["NavigationBarForeground"] = new SolidColorBrush(CyanColor);
    }

    void ApplyDetailNavigationBarAppearance()
    {
        if (_nav == null)
            return;

        _nav.Resources["NavigationBarBackground"] = Brushes.Transparent;
        _nav.Resources["NavigationBarForeground"] = new SolidColorBrush(CyanColor);
    }

    ContentPage BuildHomePage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgColor) };
        page.Header = BuildPixelArcadeLogo();
        NavigationPage.SetTopCommandBar(page, BuildNavBarRight());
        ApplyHomeNavigationBarAppearance();

        var panel = new Panel();
        panel.Children.Add(BuildHomeTabbedPage());
        panel.Children.Add(BuildSearchFab());

        page.Content = panel;
        return page;
    }

    static Control BuildPixelArcadeLogo()
    {
        var row = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        var iconPanel = new Grid { Width = 36, Height = 30 };
        iconPanel.Children.Add(new Border
        {
            Width = 36, Height = 20, CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.Parse("#cc44dd")),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
        });
        iconPanel.Children.Add(new Border
        {
            Width = 9, Height = 9,
            Background = new SolidColorBrush(SurfaceColor),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Bottom,
            Margin = new Thickness(4, 0, 0, 6),
        });
        iconPanel.Children.Add(new Border
        {
            Width = 9, Height = 9,
            Background = new SolidColorBrush(SurfaceColor),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 4, 6),
        });
        row.Children.Add(iconPanel);

        var textStack = new StackPanel { Spacing = 1 };
        textStack.Children.Add(new TextBlock
        {
            Text = "PIXEL",
            FontFamily = new FontFamily("Courier New, monospace"),
            FontSize = 14, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor), LineHeight = 16,
        });
        textStack.Children.Add(new TextBlock
        {
            Text = "ARCADE",
            FontFamily = new FontFamily("Courier New, monospace"),
            FontSize = 14, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor), LineHeight = 16,
        });
        row.Children.Add(textStack);
        return row;
    }

    static Control BuildNavBarRight()
    {
        var row = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        row.Children.Add(new PathIcon
        {
            Width = 16, Height = 16,
            Foreground = new SolidColorBrush(TextColor),
            Data = Geometry.Parse("M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"),
        });
        var avatar = new Border
        {
            Width = 26, Height = 26,
            CornerRadius = new CornerRadius(0),
            ClipToBounds = true,
            Background = new SolidColorBrush(SurfaceColor),
            BorderBrush = new SolidColorBrush(MutedColor),
            BorderThickness = new Thickness(1),
        };
        avatar.Child = new TextBlock
        {
            Text = "P1",
            FontFamily = new FontFamily("Courier New, monospace"),
            FontSize = 7, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(CyanColor),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
        };
        row.Children.Add(avatar);
        return row;
    }

    TabbedPage BuildHomeTabbedPage()
    {
        var tp = new TabbedPage
        {
            Background         = new SolidColorBrush(BgColor),
            TabPlacement       = TabPlacement.Bottom,
            PageTransition     = new PageSlide(TimeSpan.FromMilliseconds(250)),
        };
        tp.Resources["TabItemHeaderFontSize"] = 12.0;
        tp.Resources["TabbedPageTabStripBackground"] = new SolidColorBrush(SurfaceColor);
        tp.Resources["TabbedPageTabItemHeaderForegroundSelected"] = new SolidColorBrush(Color.Parse("#ad2bee"));
        tp.Resources["TabbedPageTabItemHeaderForegroundUnselected"] = new SolidColorBrush(MutedColor);

        var homeView = new RetroGamingHomeView();
        homeView.GameSelected = PushDetailPage;

        var homeTab = new ContentPage
        {
            Header     = "Home",
            Icon       = "M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z",
            Background = new SolidColorBrush(BgColor),
            Content    = homeView,
        };

        var gamesView = new RetroGamingGamesView();
        gamesView.GameSelected = PushDetailPage;

        var gamesTab = new ContentPage
        {
            Header     = "Games",
            Icon       = "M7.97,16L5,19C4.67,19.3 4.23,19.5 3.75,19.5A1.75,1.75 0 0,1 2,17.75V17.5L3,10.12C3.21,7.81 5.14,6 7.5,6H16.5C18.86,6 20.79,7.81 21,10.12L22,17.5V17.75A1.75,1.75 0 0,1 20.25,19.5C19.77,19.5 19.33,19.3 19,19L16.03,16H7.97M7,9V11H5V13H7V15H9V13H11V11H9V9H7M14.5,12A1.5,1.5 0 0,0 13,13.5A1.5,1.5 0 0,0 14.5,15A1.5,1.5 0 0,0 16,13.5A1.5,1.5 0 0,0 14.5,12M17.5,9A1.5,1.5 0 0,0 16,10.5A1.5,1.5 0 0,0 17.5,12A1.5,1.5 0 0,0 19,10.5A1.5,1.5 0 0,0 17.5,9Z",
            Background = new SolidColorBrush(BgColor),
            Content    = gamesView,
        };

        var favTab = new ContentPage
        {
            Header     = "Favorites",
            Icon       = "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z",
            Background = new SolidColorBrush(BgColor),
            Content    = new RetroGamingFavoritesView(),
        };

        var profileTab = new ContentPage
        {
            Header     = "Profile",
            Icon       = "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
            Background = new SolidColorBrush(BgColor),
            Content    = new RetroGamingProfileView(),
        };

        tp.Pages = new ObservableCollection<Page> { homeTab, gamesTab, favTab, profileTab };
        return tp;
    }

    Control BuildSearchFab()
    {
        var fab = new Button
        {
            Width  = 50, Height = 50,
            CornerRadius = new CornerRadius(0),
            Background = new SolidColorBrush(YellowColor),
            Padding = new Thickness(0),
        };
        fab.Classes.Add("retro-fab");
        fab.Content = new PathIcon
        {
            Width = 22, Height = 22,
            Foreground = new SolidColorBrush(BgColor),
            Data = Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
        };
        fab.Click += (_, _) => _ = _nav?.PushModalAsync(BuildSearchModal());

        return new Border
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Bottom,
            Margin    = new Thickness(0, 0, 0, 35),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 10, Spread = 1,
                Color = Color.FromArgb(140, 255, 255, 0),
            }),
            Child = fab,
        };
    }

    ContentPage BuildSearchModal()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgColor) };

        var searchView = new RetroGamingSearchView();
        searchView.CloseRequested = () => _ = _nav?.PopModalAsync();
        searchView.GameSelected   = async title =>
        {
            await (_nav?.PopModalAsync() ?? System.Threading.Tasks.Task.CompletedTask);
            PushDetailPage(title);
        };

        page.Content = searchView;
        return page;
    }

    async void PushDetailPage(string gameTitle)
    {
        if (_nav == null)
            return;

        var detailView = new RetroGamingDetailView(gameTitle);

        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgColor),
            Content    = detailView,
        };

        NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
        page.Navigating += args =>
        {
            if (args.NavigationType == NavigationType.Pop)
                ApplyHomeNavigationBarAppearance();

            return Task.CompletedTask;
        };

        var cmdBar = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var heartBtn = new Button();
        heartBtn.Classes.Add("retro-icon-btn");
        heartBtn.Content = new PathIcon
        {
            Width = 16, Height = 16,
            Foreground = new SolidColorBrush(Color.Parse("#ad2bee")),
            Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
        };
        var shareBtn = new Button();
        shareBtn.Classes.Add("retro-icon-btn");
        shareBtn.Content = new PathIcon
        {
            Width = 16, Height = 16,
            Foreground = new SolidColorBrush(TextColor),
            Data = Geometry.Parse("M18,16.08C17.24,16.08 16.56,16.38 16.04,16.85L8.91,12.7C8.96,12.47 9,12.24 9,12C9,11.76 8.96,11.53 8.91,11.3L15.96,7.19C16.5,7.69 17.21,8 18,8A3,3 0 0,0 21,5A3,3 0 0,0 18,2A3,3 0 0,0 15,5C15,5.24 15.04,5.47 15.09,5.7L8.04,9.81C7.5,9.31 6.79,9 6,9A3,3 0 0,0 3,12A3,3 0 0,0 6,15C6.79,15 7.5,14.69 8.04,14.19L15.16,18.35C15.11,18.56 15.08,18.78 15.08,19C15.08,20.61 16.39,21.92 18,21.92C19.61,21.92 20.92,20.61 20.92,19C20.92,17.39 19.61,16.08 18,16.08Z"),
        };
        cmdBar.Children.Add(heartBtn);
        cmdBar.Children.Add(shareBtn);
        NavigationPage.SetTopCommandBar(page, cmdBar);

        ApplyDetailNavigationBarAppearance();
        await _nav.PushAsync(page);

        if (!ReferenceEquals(_nav.CurrentPage, page))
            ApplyHomeNavigationBarAppearance();
    }
}
