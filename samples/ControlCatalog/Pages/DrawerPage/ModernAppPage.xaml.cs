using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class ModernAppPage : UserControl
{
    // Palette
    static readonly Color Primary   = Color.Parse("#0dccf2");
    static readonly Color BgLight   = Color.Parse("#f5f8f8");

    static IBrush BgBrush => new SolidColorBrush(BgLight);

    DrawerPage?     _drawerPage;
    NavigationPage? _navPage;
    ScrollViewer?   _infoPanel;
    TextBlock?      _pageTitle;
    Button?         _selectedNavBtn;

    public ModernAppPage()
    {
        InitializeComponent();

        _infoPanel  = this.FindControl<ScrollViewer>("InfoPanel");
        _drawerPage = this.FindControl<DrawerPage>("DrawerPageControl");
        _navPage    = this.FindControl<NavigationPage>("NavPage");
        _pageTitle  = this.FindControl<TextBlock>("PageTitle");

        if (_navPage != null)
            NavigateToDiscover();
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
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
            _infoPanel.IsVisible = Bounds.Width >= 640;
    }

    void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        SelectNavButton(btn);
        _drawerPage!.IsOpen = false;
        switch (btn.Tag?.ToString())
        {
            case "Discover": NavigateToDiscover();  break;
            case "MyTrips":  NavigateToMyTrips();   break;
            case "Profile":  NavigateToProfile();   break;
            case "Settings": NavigateToSettings();  break;
        }
    }

    void OnCloseDrawer(object? sender, RoutedEventArgs e)
    {
        if (_drawerPage != null) _drawerPage.IsOpen = false;
    }

    void SelectNavButton(Button btn)
    {
        if (_selectedNavBtn != null)
            _selectedNavBtn.Background = Brushes.Transparent;
        _selectedNavBtn = btn;
        btn.Background = new SolidColorBrush(Color.Parse("#1A0dccf2"));
    }

    async Task Navigate(ContentPage page)
    {
        if (_navPage == null) return;
        NavigationPage.SetHasBackButton(page, false);
        NavigationPage.SetHasNavigationBar(page, false);
        await _navPage.PopToRootAsync();
        await _navPage.PushAsync(page);
    }

    async void NavigateToDiscover()
    {
        if (_pageTitle != null) _pageTitle.Text = "Discover";
        SelectNavButton(this.FindControl<Button>("BtnDiscover")!);
        await Navigate(new ContentPage { Content = new ModernDiscoverView(), Background = BgBrush });
    }

    async void NavigateToMyTrips()
    {
        if (_pageTitle != null) _pageTitle.Text = "My Trips";
        SelectNavButton(this.FindControl<Button>("BtnMyTrips")!);
        await Navigate(new ContentPage { Content = new ModernMyTripsView(), Background = BgBrush });
    }

    async void NavigateToProfile()
    {
        if (_pageTitle != null) _pageTitle.Text = "Profile";
        SelectNavButton(this.FindControl<Button>("BtnProfile")!);
        await Navigate(new ContentPage { Content = new ModernProfileView(), Background = BgBrush });
    }

    async void NavigateToSettings()
    {
        if (_pageTitle != null) _pageTitle.Text = "Settings";
        SelectNavButton(this.FindControl<Button>("BtnSettings")!);
        await Navigate(new ContentPage { Content = new ModernSettingsView(), Background = BgBrush });
    }
}
