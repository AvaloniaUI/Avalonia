using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class PulseAppPage : UserControl
{
    static readonly Color Primary      = Color.Parse("#256af4");
    static readonly Color BgDark       = Color.Parse("#101622");
    static readonly Color BgDashboard  = Color.Parse("#0a0a0a");
    static readonly Color TextDimmed   = Color.Parse("#64748b");

    NavigationPage? _navPage;
    ContentPage?    _loginPage;
    ScrollViewer?   _infoPanel;

    public PulseAppPage()
    {
        InitializeComponent();

        _navPage = this.FindControl<NavigationPage>("NavPage");
        if (_navPage != null)
        {
            _loginPage = BuildLoginPage();
            _ = _navPage.PushAsync(_loginPage);
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

    ContentPage BuildLoginPage()
    {
        var loginView = new PulseLoginView();
        loginView.LoginRequested = OnLoginRequested;

        var page = new ContentPage
        {
            Content    = loginView,
            Background = new SolidColorBrush(BgDark),
        };
        NavigationPage.SetHasNavigationBar(page, false);
        return page;
    }

    async void OnLoginRequested()
    {
        if (_navPage == null || _loginPage == null) return;

        var dashboard = BuildDashboardPage();
        await _navPage.PushAsync(dashboard);
        _navPage.RemovePage(_loginPage);
        _loginPage = null;
    }

    TabbedPage BuildDashboardPage()
    {
        var tp = new TabbedPage
        {
            Background         = new SolidColorBrush(BgDashboard),
            TabPlacement       = TabPlacement.Bottom,
            PageTransition     = new PageSlide(TimeSpan.FromMilliseconds(200)),
        };
        tp.Resources["TabItemHeaderFontSize"] = 12.0;
        tp.Resources["TabbedPageTabStripBackground"] = new SolidColorBrush(BgDashboard);
        tp.Resources["TabbedPageTabItemHeaderForegroundSelected"] = new SolidColorBrush(Primary);
        tp.Resources["TabbedPageTabItemHeaderForegroundUnselected"] = new SolidColorBrush(TextDimmed);
        NavigationPage.SetHasNavigationBar(tp, false);

        var homeView = new PulseHomeView();
        homeView.WorkoutDetailRequested = PushWorkoutDetail;

        var homePage = new ContentPage
        {
            Content    = homeView,
            Background = new SolidColorBrush(BgDashboard),
            Header     = "Home",
            Icon       = new PathIcon { Data = Geometry.Parse("M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z") },
        };

        var workoutsPage = new ContentPage
        {
            Content    = new PulseWorkoutsView(),
            Background = new SolidColorBrush(BgDashboard),
            Header     = "Workouts",
            Icon       = new PathIcon { Data = Geometry.Parse("M20.57 14.86L22 13.43 20.57 12 17 15.57 8.43 7 12 3.43 10.57 2 9.14 3.43 7.71 2 5.57 4.14 4.14 2.71 2.71 4.14l1.43 1.43L2 7.71l1.43 1.43L2 10.57 3.43 12 7 8.43 15.57 17 12 20.57 13.43 22l1.43-1.43L16.29 22l2.14-2.14 1.43 1.43 1.43-1.43-1.43-1.43L22 16.29z") },
        };

        var profilePage = new ContentPage
        {
            Content    = new PulseProfileView(),
            Background = new SolidColorBrush(BgDashboard),
            Header     = "Profile",
            Icon       = new PathIcon { Data = Geometry.Parse("M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z") },
        };

        tp.Pages = new ObservableCollection<Page> { homePage, workoutsPage, profilePage };
        return tp;
    }

    async void PushWorkoutDetail()
    {
        if (_navPage == null) return;

        var detailView = new PulseWorkoutDetailView();
        detailView.BackRequested = async () => { if (_navPage != null) await _navPage.PopAsync(); };

        var page = new ContentPage
        {
            Content    = detailView,
            Background = new SolidColorBrush(BgDark),
        };
        NavigationPage.SetHasNavigationBar(page, false);
        await _navPage.PushAsync(page);
    }
}
