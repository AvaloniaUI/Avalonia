using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class EcoTrackerAppPage : UserControl
{
    static readonly Color Primary     = Color.Parse("#2E7D32");
    static readonly Color Accent      = Color.Parse("#4CAF50");
    static readonly Color BgLight     = Color.Parse("#F1F8E9");
    static readonly Color TextDark    = Color.Parse("#1A2E1C");
    static readonly Color TextMuted   = Color.Parse("#90A4AE");

    const string LeafPath =
        "M12 3C9 6 6 9 6 13C6 17.4 8.7 21 12 22C15.3 21 18 17.4 18 13C18 9 15 6 12 3Z";

    NavigationPage? _navPage;
    DrawerPage? _drawerPage;
    ScrollViewer? _infoPanel;
    bool _initialized;
    Button? _selectedBtn;

    public EcoTrackerAppPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel   = this.FindControl<ScrollViewer>("InfoPanel");
        _navPage     = this.FindControl<NavigationPage>("NavPage");
        _drawerPage  = this.FindControl<DrawerPage>("DrawerPageControl");
        _selectedBtn = this.FindControl<Button>("BtnHome");

        UpdateInfoPanelVisibility();

        if (_initialized) return;
        _initialized = true;

        if (_navPage == null) return;
        var homePage = BuildHomePage();
        _navPage.Push(homePage);
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

    private void OnDrawerToggleClick(object? sender, RoutedEventArgs e)
    {
        if (_drawerPage != null)
            _drawerPage.IsOpen = !_drawerPage.IsOpen;
    }

    private async void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        if (_selectedBtn != null)
        {
            _selectedBtn.Classes.Remove("ecoNavItemSelected");
            _selectedBtn.Classes.Add("ecoNavItem");
        }
        _selectedBtn = btn;
        btn.Classes.Add("ecoNavItemSelected");

        if (_drawerPage != null)
            _drawerPage.IsOpen = false;

        if (_navPage == null) return;

        ContentPage page = tag switch
        {
            "Home"      => BuildHomePage(),
            "Stats"     => BuildStatsPage(),
            "Habits"    => BuildHabitsPage(),
            "Community" => BuildCommunityPage(),
            _           => BuildHomePage(),
        };

        NavigationPage.SetHasBackButton(page, false);
        await _navPage.PopToRootAsync(null);
        await _navPage.PushAsync(page, new CrossFade(TimeSpan.FromMilliseconds(200)));
    }

    ContentPage BuildHomePage()
    {
        var homeView = new EcoTrackerHomeView();
        homeView.TreeDetailRequested = () =>
            _navPage?.PushAsync(BuildTreeDetailPage(), new PageSlide(TimeSpan.FromMilliseconds(250)));
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = homeView,
        };
        page.Header = "Home";
        NavigationPage.SetHasBackButton(page, false);
        return page;
    }

    ContentPage BuildStatsPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = new EcoTrackerStatsView(),
        };
        page.Header = "Stats";
        return page;
    }

    ContentPage BuildHabitsPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = new EcoTrackerHabitsView(),
        };
        page.Header = "Habits";
        return page;
    }

    ContentPage BuildCommunityPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = new EcoTrackerCommunityView(),
        };
        page.Header = "Community";
        return page;
    }

    ContentPage BuildTreeDetailPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgLight) };
        page.Header = "Tree Progress";

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var summaryCard = new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = Brushes.White,
            Padding = new Thickness(20),
            Margin = new Thickness(16, 20, 16, 0),
        };
        var summaryStack = new StackPanel { Spacing = 12 };

        var progressHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        progressHeader.Children.Add(Label("Your Tree Goal", 18, FontWeight.Bold, TextDark));
        var pctLabel = Label("75%", 18, FontWeight.Black, Accent);
        Grid.SetColumn(pctLabel, 1);
        progressHeader.Children.Add(pctLabel);
        summaryStack.Children.Add(progressHeader);

        var trackBg = new Border
        {
            Height = 10, CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(Color.Parse("#E8F5E9")),
        };
        var fill = new Border
        {
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(Accent),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var progressBar = new Grid();
        progressBar.Children.Add(trackBg);
        progressBar.LayoutUpdated += (_, _) =>
        {
            if (progressBar.Bounds.Width > 0)
                fill.Width = progressBar.Bounds.Width * 0.75;
        };
        progressBar.Children.Add(fill);
        summaryStack.Children.Add(progressBar);
        summaryStack.Children.Add(Label("15 of 20 trees planted this month", 13, FontWeight.Normal, TextMuted));
        summaryCard.Child = summaryStack;
        root.Children.Add(summaryCard);

        var milestonesSection = new StackPanel { Margin = new Thickness(16, 20, 16, 0), Spacing = 10 };
        milestonesSection.Children.Add(Label("Milestones", 16, FontWeight.SemiBold, TextDark));

        milestonesSection.Children.Add(BuildMilestoneItem("First Tree",       "Plant your first tree",         true));
        milestonesSection.Children.Add(BuildMilestoneItem("Green Starter",    "Plant 5 trees",                 true));
        milestonesSection.Children.Add(BuildMilestoneItem("Forest Builder",   "Plant 10 trees",                true));
        milestonesSection.Children.Add(BuildMilestoneItem("Eco Champion",     "Plant 20 trees",                false));
        milestonesSection.Children.Add(BuildMilestoneItem("Nature Guardian",  "Plant 50 trees",                false));
        root.Children.Add(milestonesSection);

        var recentSection = new StackPanel { Margin = new Thickness(16, 20, 16, 20), Spacing = 10 };
        recentSection.Children.Add(Label("Recent Plantings", 16, FontWeight.SemiBold, TextDark));

        recentSection.Children.Add(BuildPlantingItem("Oak Tree",    "Central Park area", "2 days ago"));
        recentSection.Children.Add(BuildPlantingItem("Maple Tree",  "Riverside zone",    "5 days ago"));
        recentSection.Children.Add(BuildPlantingItem("Pine Tree",   "Mountain trail",    "1 week ago"));
        root.Children.Add(recentSection);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    static Border BuildMilestoneItem(string title, string description, bool achieved)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            Padding = new Thickness(12),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };

        var icon = new Border
        {
            Width = 36, Height = 36, CornerRadius = new CornerRadius(18),
            Background = achieved
                ? new SolidColorBrush(Color.Parse("#4CAF50"))
                : new SolidColorBrush(Color.Parse("#E0E0E0")),
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (achieved)
        {
            icon.Child = new PathIcon
            {
                Width = 18, Height = 18,
                Data = Geometry.Parse("M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        else
        {
            icon.Child = new PathIcon
            {
                Width = 18, Height = 18,
                Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"),
                Foreground = new SolidColorBrush(Color.Parse("#BDBDBD")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        grid.Children.Add(icon);

        var info = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        info.Children.Add(Label(title, 14, FontWeight.SemiBold, achieved ? Color.Parse("#1A2E1C") : Color.Parse("#9E9E9E")));
        info.Children.Add(Label(description, 12, FontWeight.Normal, Color.Parse("#90A4AE")));
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        card.Child = grid;
        return card;
    }

    static Border BuildPlantingItem(string treeName, string location, string time)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            Padding = new Thickness(12),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        var treeBg = new Border
        {
            Width = 40, Height = 40, CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(25, 76, 175, 80)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PathIcon
            {
                Width = 20, Height = 20,
                Data = Geometry.Parse(LeafPath),
                Foreground = new SolidColorBrush(Color.Parse("#4CAF50")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        grid.Children.Add(treeBg);

        var info = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        info.Children.Add(Label(treeName, 14, FontWeight.SemiBold, Color.Parse("#1A2E1C")));
        info.Children.Add(Label(location, 12, FontWeight.Normal, Color.Parse("#90A4AE")));
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        var timeLabel = Label(time, 11, FontWeight.Normal, Color.Parse("#90A4AE"));
        timeLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(timeLabel, 2);
        grid.Children.Add(timeLabel);

        card.Child = grid;
        return card;
    }

    static TextBlock Label(string text, double size, FontWeight weight, Color color)
        => new()
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = new SolidColorBrush(color),
        };
}
