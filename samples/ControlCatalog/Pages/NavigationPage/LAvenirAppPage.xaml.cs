using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace ControlCatalog.Pages;

public partial class LAvenirAppPage : UserControl
{
    static readonly Color Primary    = Color.Parse("#4b2bee");
    static readonly Color BgDark     = Color.Parse("#131022");
    static readonly Color BgLight    = Color.Parse("#f6f6f8");
    static readonly Color TextDark   = Color.Parse("#1e293b");
    static readonly Color TextMuted  = Color.Parse("#94a3b8");
    static readonly Color BorderLight = Color.Parse("#e2e8f0");

    NavigationPage? _navPage;
    DrawerPage?     _drawerPage;
    bool            _initialized;
    ScrollViewer?   _infoPanel;

    public LAvenirAppPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoPanelVisibility();

        if (_initialized) return;
        _initialized = true;

        _navPage     = this.FindControl<NavigationPage>("NavPage");
        _drawerPage  = this.FindControl<DrawerPage>("DrawerPageControl");

        if (_navPage == null) return;

        _navPage.Push(BuildMenuTabbedPage());
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

    TabbedPage BuildMenuTabbedPage()
    {
        var tp = new TabbedPage
        {
            Background        = new SolidColorBrush(BgLight),
            BarBackground     = new SolidColorBrush(Colors.White),
            SelectedTabBrush  = new SolidColorBrush(Primary),
            UnselectedTabBrush = new SolidColorBrush(TextMuted),
            TabPlacement      = TabPlacement.Bottom,
            PageTransition    = new PageSlide(TimeSpan.FromMilliseconds(200)),
        };
        tp.Resources["TabItemHeaderFontSize"]           = 12.0;
        tp.Resources["TabbedPageTabStripBorderThickness"] = new Thickness(0, 1, 0, 0);
        tp.Resources["TabbedPageTabStripBorderBrush"]   = new SolidColorBrush(BorderLight);

        tp.IndicatorTemplate = new FuncDataTemplate<object>((_, _) =>
            new Ellipse
            {
                Width  = 5, Height = 5,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Fill   = new SolidColorBrush(Primary),
            });

        tp.Header = new TextBlock
        {
            Text              = "L'Avenir",
            FontSize          = 18,
            FontWeight        = FontWeight.Bold,
            Foreground        = new SolidColorBrush(TextDark),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment     = TextAlignment.Center,
        };

        NavigationPage.SetTopCommandBar(tp, new Button
        {
            Width           = 40,
            Height          = 40,
            CornerRadius    = new CornerRadius(12),
            Background      = Brushes.Transparent,
            Foreground      = new SolidColorBrush(TextDark),
            Padding         = new Thickness(8),
            BorderThickness = new Thickness(0),
            Content         = new PathIcon
            {
                Data   = Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
                Width  = 18,
                Height = 18,
            },
            VerticalAlignment = VerticalAlignment.Center,
        });

        var menuView = new LAvenirMenuView();
        menuView.DishSelected = PushDishDetail;

        var menuPage = new ContentPage
        {
            Content    = menuView,
            Background = new SolidColorBrush(BgLight),
            Header     = "Menu",
            Icon       = "M11 9H9V2H7v7H5V2H3v7c0 2.12 1.66 3.84 3.75 3.97V22h2.5v-9.03C11.34 12.84 13 11.12 13 9V2h-2v7zm5-3v8h2.5v8H21V2c-2.76 0-5 2.24-5 4z",
        };

        var reservationsPage = new ContentPage
        {
            Content    = new LAvenirReservationsView(),
            Background = new SolidColorBrush(BgLight),
            Header     = "Reservations",
            Icon       = "M19 3h-1V1h-2v2H8V1H6v2H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11zM9 10H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2z",
        };

        var profilePage = new ContentPage
        {
            Content    = new LAvenirProfileView(),
            Background = new SolidColorBrush(BgLight),
            Header     = "Profile",
            Icon       = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z",
        };

        tp.Pages = new ObservableCollection<object?> { menuPage, reservationsPage, profilePage };
        return tp;
    }

    void PushDishDetail(string name, string price, string description, string imageFile)
    {
        if (_navPage == null) return;

        var detail = new ContentPage
        {
            Content    = new LAvenirDishDetailView(name, price, description, imageFile),
            Background = new SolidColorBrush(BgDark),
            Header     = name,
        };
        NavigationPage.SetBottomCommandBar(detail, BuildFloatingBar(price));

        _navPage.Background    = new SolidColorBrush(BgDark);
        _navPage.BarBackground = new SolidColorBrush(BgDark);
        _navPage.BarForeground = Brushes.White;

        detail.NavigatedFrom += (_, _) =>
        {
            if (_navPage != null)
            {
                _navPage.Background    = new SolidColorBrush(BgLight);
                _navPage.BarBackground = new SolidColorBrush(BgLight);
                _navPage.BarForeground = new SolidColorBrush(TextDark);
            }
        };

        _navPage.Push(detail);
    }

    Border BuildFloatingBar(string price)
    {
        var bar = new Border
        {
            CornerRadius    = new CornerRadius(16),
            Background      = new SolidColorBrush(Color.FromArgb(178, BgDark.R, BgDark.G, BgDark.B)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(16, 12),
            Margin          = new Thickness(16, 8, 16, 8),
        };

        var barGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock
        {
            Text       = "Add to Order",
            FontSize   = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
        });
        info.Children.Add(new TextBlock
        {
            Text       = price,
            FontSize   = 12,
            FontWeight = FontWeight.Medium,
            Foreground = new SolidColorBrush(TextMuted),
        });
        barGrid.Children.Add(info);

        var addBtn = new Button
        {
            Content                  = "Add",
            Width                    = 80,
            Height                   = 40,
            CornerRadius             = new CornerRadius(10),
            Background               = new SolidColorBrush(Primary),
            Foreground               = Brushes.White,
            FontWeight               = FontWeight.Bold,
            FontSize                 = 14,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        var hoverStyle = new Style(x => x.OfType<Button>().Class(":pointerover").Descendant().OfType<ContentPresenter>());
        hoverStyle.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, new SolidColorBrush(Color.Parse("#3d22cc"))));
        hoverStyle.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, Brushes.White));
        addBtn.Styles.Add(hoverStyle);

        var pressStyle = new Style(x => x.OfType<Button>().Class(":pressed").Descendant().OfType<ContentPresenter>());
        pressStyle.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, new SolidColorBrush(Color.Parse("#3518b0"))));
        pressStyle.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, Brushes.White));
        addBtn.Styles.Add(pressStyle);

        Grid.SetColumn(addBtn, 1);
        barGrid.Children.Add(addBtn);
        bar.Child = barGrid;
        return bar;
    }

    void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string) return;

        if (_drawerPage != null)
            _drawerPage.IsOpen = false;

        _ = _navPage?.PopToRootAsync();
    }
}
