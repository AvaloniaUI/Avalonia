using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class EcoTrackerAppPage : UserControl
{
    static readonly Color Primary      = Color.Parse("#2E7D32");
    static readonly Color Accent       = Color.Parse("#4CAF50");
    static readonly Color AccentLight  = Color.Parse("#A5D6A7");
    static readonly Color BgLight      = Color.Parse("#F1F8E9");
    static readonly Color TextDark     = Color.Parse("#1A2E1C");
    static readonly Color TextWhite    = Colors.White;
    static readonly Color TextMuted    = Color.Parse("#90A4AE");
    static readonly Color ChartMuted   = Color.Parse("#A5D6A7");

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

        _infoPanel  = this.FindControl<ScrollViewer>("InfoPanel");
        _navPage    = this.FindControl<NavigationPage>("NavPage");
        _drawerPage = this.FindControl<DrawerPage>("DrawerPageControl");
        _selectedBtn = this.FindControl<Button>("BtnHome");

        UpdateInfoPanelVisibility();

        if (_initialized) return;
        _initialized = true;

        if (_navPage == null) return;
        var homePage = BuildHomePage();
        NavigationPage.SetHasBackButton(homePage, false);
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
        var page = new ContentPage { Background = new SolidColorBrush(BgLight) };
        page.Header = "Home";

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };
        var heroCard = new Border
        {
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
            Margin = new Thickness(16, 20, 16, 0),
        };
        var heroPanel = new Panel();

        heroPanel.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#2E7D32"), 0),
                    new GradientStop(Color.Parse("#1B5E20"), 1),
                },
            },
        });

        heroPanel.Children.Add(new Border
        {
            Width = 140, Height = 140,
            CornerRadius = new CornerRadius(70),
            Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -40, -40, 0),
        });

        var heroContent = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(20),
        };

        var heroLeft = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        heroLeft.Children.Add(Label("TREES SAVED", 10, FontWeight.Bold, AccentLight));
        var treesRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        treesRow.Children.Add(Label("15", 48, FontWeight.Black, TextWhite));
        treesRow.Children.Add(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { Label("/20", 16, FontWeight.Medium, AccentLight) },
        });
        heroLeft.Children.Add(treesRow);
        heroLeft.Children.Add(Label("Keep going! 5 more to reach your goal", 12, FontWeight.Normal, AccentLight));
        heroContent.Children.Add(heroLeft);

        var ringPanel = new Panel
        {
            Width = 72, Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ringPanel.Children.Add(new Border
        {
            Width = 72, Height = 72,
            CornerRadius = new CornerRadius(36),
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 165, 214, 167)),
            BorderThickness = new Thickness(5),
        });
        ringPanel.Children.Add(new PathIcon
        {
            Width = 32, Height = 32,
            Data = Geometry.Parse(LeafPath),
            Foreground = new SolidColorBrush(Accent),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(ringPanel, 1);
        heroContent.Children.Add(ringPanel);
        heroPanel.Children.Add(heroContent);
        heroCard.Child = heroPanel;

        // Tapping hero card pushes a detail page (demonstrates back button)
        var heroButton = new Button
        {
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(16, 20, 16, 0),
            Content = heroCard,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };
        heroCard.Margin = new Thickness(0);
        heroButton.Click += (_, _) => _navPage?.PushAsync(BuildTreeDetailPage(), new PageSlide(TimeSpan.FromMilliseconds(250)));
        root.Children.Add(heroButton);
        var impactSection = new StackPanel { Margin = new Thickness(16, 20, 16, 0), Spacing = 10 };
        var impactHeader = Label("WEEKLY IMPACT", 11, FontWeight.Bold, TextMuted);
        impactHeader.LetterSpacing = 1.5;
        impactSection.Children.Add(impactHeader);

        var impactRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        impactRow.Children.Add(BuildImpactCard("Water Saved", "120 L", "#2196F3", "M12,20A6,6 0 0,1 6,14C6,10 12,3.25 12,3.25C12,3.25 18,10 18,14A6,6 0 0,1 12,20Z"));
        impactRow.Children.Add(BuildImpactCard("Carbon Reduced", "15 kg", "#4CAF50", "M19.35 10.04A7.49 7.49 0 0 0 12 4C9.11 4 6.6 5.64 5.35 8.04A5.994 5.994 0 0 0 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96z"));
        impactSection.Children.Add(impactRow);
        root.Children.Add(impactSection);
        var tasksSection = new StackPanel { Margin = new Thickness(16, 20, 16, 16), Spacing = 10 };
        var tasksHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        tasksHeader.Children.Add(Label("DAILY TASKS", 11, FontWeight.Bold, TextMuted));
        var countLabel = Label("3 of 5 completed", 11, FontWeight.Normal, Accent);
        Grid.SetColumn(countLabel, 1);
        tasksHeader.Children.Add(countLabel);
        tasksSection.Children.Add(tasksHeader);

        tasksSection.Children.Add(BuildTaskItem("Use reusable water bottle", true));
        tasksSection.Children.Add(BuildTaskItem("Walk or bike to work", true));
        tasksSection.Children.Add(BuildTaskItem("Meatless meal", true));
        tasksSection.Children.Add(BuildTaskItem("30-min shorter shower", false));
        tasksSection.Children.Add(BuildTaskItem("Sort recycling", false));
        root.Children.Add(tasksSection);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    ContentPage BuildStatsPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgLight) };
        page.Header = "Stats";

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var statsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(16, 20, 16, 0),
        };
        statsRow.Children.Add(BuildStatChip("CO₂",   "12.4 kg", "↑8%",  "#4CAF50"));
        statsRow.Children.Add(BuildStatChip("Energy", "45 kWh",  "↑12%", "#FFC107"));
        statsRow.Children.Add(BuildStatChip("Water",  "87 L",    "↑5%",  "#2196F3"));
        root.Children.Add(statsRow);

        var weekSection = new StackPanel { Margin = new Thickness(16, 20, 16, 0), Spacing = 12 };
        weekSection.Children.Add(Label("This Week's Activity", 16, FontWeight.SemiBold, TextDark));

        var chart = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        string[] days   = { "M", "T", "W", "T", "F", "S", "S" };
        double[] values = { 0.55, 0.70, 0.45, 0.88, 0.60, 0.92, 0.75 };
        for (int i = 0; i < days.Length; i++)
            chart.Children.Add(BuildBarItem(days[i], values[i], i == 6));
        weekSection.Children.Add(chart);
        root.Children.Add(weekSection);

        var breakdownSection = new StackPanel { Margin = new Thickness(16, 20, 16, 20), Spacing = 12 };
        breakdownSection.Children.Add(Label("Carbon Breakdown", 16, FontWeight.SemiBold, TextDark));
        breakdownSection.Children.Add(BuildBreakdownRow("Transportation", "4.1 kg", 0.33, Accent));
        breakdownSection.Children.Add(BuildBreakdownRow("Food",           "3.8 kg", 0.31, Accent));
        breakdownSection.Children.Add(BuildBreakdownRow("Home Energy",    "2.6 kg", 0.21, Accent));
        breakdownSection.Children.Add(BuildBreakdownRow("Shopping",       "1.9 kg", 0.15, Accent));
        root.Children.Add(breakdownSection);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    ContentPage BuildHabitsPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgLight) };
        page.Header = "Habits";

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var activeSection = new StackPanel { Margin = new Thickness(16, 20, 16, 0), Spacing = 10 };
        activeSection.Children.Add(Label("Active Habits", 16, FontWeight.SemiBold, TextDark));

        activeSection.Children.Add(BuildHabitCard("Reusable Bottle", "12 day streak", "#2196F3",
            "M12,20A6,6 0 0,1 6,14C6,10 12,3.25 12,3.25C12,3.25 18,10 18,14A6,6 0 0,1 12,20Z"));
        activeSection.Children.Add(BuildHabitCard("Walk to Work", "8 day streak", "#4CAF50",
            "M13.5 5.5c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zM9.8 8.9L7 23h2.1l1.8-8 2.1 2v6h2v-7.5l-2.1-2 .6-3C14.8 12 16.8 13 19 13v-2c-1.9 0-3.5-1-4.3-2.4l-1-1.6c-.4-.6-1-1-1.7-1-.3 0-.5.1-.8.1L6 8.3V13h2V9.6l1.8-.7"));
        activeSection.Children.Add(BuildHabitCard("Meatless Mondays", "4 week streak", "#8BC34A",
            LeafPath));
        activeSection.Children.Add(BuildHabitCard("Composting", "6 day streak", "#FF9800",
            "M17.65 6.35A7.958 7.958 0 0 0 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08A5.99 5.99 0 0 1 12 18c-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"));
        root.Children.Add(activeSection);

        var suggestedSection = new StackPanel { Margin = new Thickness(16, 20, 16, 20), Spacing = 10 };
        suggestedSection.Children.Add(Label("Suggested Habits", 16, FontWeight.SemiBold, TextMuted));

        suggestedSection.Children.Add(BuildSuggestedHabit("Shorter Showers", "Save up to 50L per day"));
        suggestedSection.Children.Add(BuildSuggestedHabit("Unplug Devices", "Reduce standby energy usage"));
        root.Children.Add(suggestedSection);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    ContentPage BuildCommunityPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgLight) };
        page.Header = "Community";

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var leaderSection = new StackPanel { Margin = new Thickness(16, 20, 16, 0), Spacing = 10 };
        leaderSection.Children.Add(Label("Leaderboard", 16, FontWeight.SemiBold, TextDark));

        leaderSection.Children.Add(BuildLeaderboardItem(1, "Alex Green",   "1,240 pts", "#FFD700"));
        leaderSection.Children.Add(BuildLeaderboardItem(2, "Sam Rivers",   "1,180 pts", "#C0C0C0"));
        leaderSection.Children.Add(BuildLeaderboardItem(3, "Jordan Leaf",  "1,050 pts", "#CD7F32"));
        leaderSection.Children.Add(BuildLeaderboardItem(4, "Casey Woods",  "980 pts",   null));
        leaderSection.Children.Add(BuildLeaderboardItem(5, "Morgan Sky",   "920 pts",   null));
        root.Children.Add(leaderSection);

        var challengeSection = new StackPanel { Margin = new Thickness(16, 20, 16, 20), Spacing = 10 };
        challengeSection.Children.Add(Label("Active Challenges", 16, FontWeight.SemiBold, TextDark));

        challengeSection.Children.Add(BuildChallengeCard("Zero Waste Week", "142 participants", "3 days left"));
        challengeSection.Children.Add(BuildChallengeCard("Bike to Work", "89 participants", "5 days left"));
        root.Children.Add(challengeSection);

        scroll.Content = root;
        page.Content = scroll;
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

    Border BuildImpactCard(string label, string value, string colorHex, string iconPath)
    {
        var color = Color.Parse(colorHex);
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(14, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };

        var iconBg = new Border
        {
            Width = 40, Height = 40, CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PathIcon
            {
                Width = 20, Height = 20,
                Data = Geometry.Parse(iconPath),
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        grid.Children.Add(iconBg);

        var info = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        info.Children.Add(Label(label, 11, FontWeight.Medium, TextMuted));
        info.Children.Add(Label(value, 18, FontWeight.Bold, color));
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        card.Child = grid;
        return card;
    }

    static Border BuildTaskItem(string text, bool completed)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            Padding = new Thickness(12, 10),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };

        var checkBorder = new Border
        {
            Width = 24, Height = 24,
            CornerRadius = new CornerRadius(6),
            Background = completed
                ? new SolidColorBrush(Color.Parse("#4CAF50"))
                : new SolidColorBrush(Color.Parse("#E0E0E0")),
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (completed)
        {
            checkBorder.Child = new PathIcon
            {
                Width = 14, Height = 14,
                Data = Geometry.Parse("M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        grid.Children.Add(checkBorder);

        var label = Label(text, 14, FontWeight.Normal, completed ? Color.Parse("#90A4AE") : Color.Parse("#1A2E1C"));
        label.Margin = new Thickness(10, 0, 0, 0);
        label.VerticalAlignment = VerticalAlignment.Center;
        if (completed)
            label.TextDecorations = TextDecorations.Strikethrough;
        Grid.SetColumn(label, 1);
        grid.Children.Add(label);

        card.Child = grid;
        return card;
    }

    Border BuildStatChip(string label, string value, string trend, string colorHex)
    {
        var chip = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(12, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(Label(label, 10, FontWeight.Medium, TextMuted));
        stack.Children.Add(Label(value, 16, FontWeight.Bold, Color.Parse(colorHex)));

        var trendRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        trendRow.Children.Add(Label(trend, 10, FontWeight.SemiBold, Color.Parse(colorHex)));
        trendRow.Children.Add(Label("wk",  10, FontWeight.Normal,  TextMuted));
        stack.Children.Add(trendRow);

        chip.Child = stack;
        return chip;
    }

    Border BuildBarItem(string day, double pct, bool isToday)
    {
        var bar = new Border { HorizontalAlignment = HorizontalAlignment.Center };
        var col = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };

        var barFill = new Border
        {
            Width = 24,
            Height = 80 * pct,
            CornerRadius = new CornerRadius(4, 4, 2, 2),
            Background = new SolidColorBrush(isToday ? Accent : ChartMuted),
            VerticalAlignment = VerticalAlignment.Bottom,
        };

        col.Children.Add(new Border { Height = 80 - (80 * pct), Width = 24 });
        col.Children.Add(barFill);

        var dayLabel = Label(day, 10, isToday ? FontWeight.Bold : FontWeight.Normal,
            isToday ? Primary : TextMuted);
        dayLabel.HorizontalAlignment = HorizontalAlignment.Center;
        col.Children.Add(dayLabel);

        bar.Child = col;
        return bar;
    }

    static Border BuildBreakdownRow(string name, string value, double weight, Color accent)
    {
        var row = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            Padding = new Thickness(12),
        };
        var stack = new StackPanel { Spacing = 6 };

        var topRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        topRow.Children.Add(Label(name, 13, FontWeight.Medium, Color.Parse("#1A2E1C")));
        var valLabel = Label(value, 13, FontWeight.Bold, accent);
        Grid.SetColumn(valLabel, 1);
        topRow.Children.Add(valLabel);
        stack.Children.Add(topRow);

        var trackBg = new Border
        {
            Height = 6, CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.Parse("#E8F5E9")),
        };
        var fill = new Border
        {
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(accent),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var progress = new Grid();
        progress.Children.Add(trackBg);
        progress.LayoutUpdated += (_, _) =>
        {
            if (progress.Bounds.Width > 0)
                fill.Width = progress.Bounds.Width * weight;
        };
        progress.Children.Add(fill);
        stack.Children.Add(progress);

        row.Child = stack;
        return row;
    }

    Border BuildHabitCard(string title, string streak, string colorHex, string iconPath)
    {
        var color = Color.Parse(colorHex);
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(12),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        var iconBg = new Border
        {
            Width = 44, Height = 44, CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PathIcon
            {
                Width = 22, Height = 22,
                Data = Geometry.Parse(iconPath),
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        grid.Children.Add(iconBg);

        var info = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        info.Children.Add(Label(title, 14, FontWeight.SemiBold, TextDark));
        info.Children.Add(Label(streak, 12, FontWeight.Normal, color));
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        var chevron = new PathIcon
        {
            Width = 16, Height = 16,
            Data = Geometry.Parse("M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6-1.41-1.41z"),
            Foreground = new SolidColorBrush(TextMuted),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(chevron, 2);
        grid.Children.Add(chevron);

        card.Child = grid;
        return card;
    }

    static Border BuildSuggestedHabit(string title, string description)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 76, 175, 80)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 10),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        var info = new StackPanel { Spacing = 2 };
        info.Children.Add(Label(title, 14, FontWeight.SemiBold, Color.Parse("#1B5E20")));
        info.Children.Add(Label(description, 12, FontWeight.Normal, Color.Parse("#2E7D32")));
        grid.Children.Add(info);

        var addLabel = Label("+ Add", 12, FontWeight.Bold, Color.Parse("#2E7D32"));
        addLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(addLabel, 1);
        grid.Children.Add(addLabel);

        card.Child = grid;
        return card;
    }

    static Border BuildLeaderboardItem(int rank, string name, string points, string? medalHex)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            Padding = new Thickness(12),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto") };

        var rankLabel = Label($"#{rank}", 14, FontWeight.Bold,
            medalHex != null ? Color.Parse(medalHex) : Color.Parse("#90A4AE"));
        rankLabel.VerticalAlignment = VerticalAlignment.Center;
        rankLabel.Width = 30;
        grid.Children.Add(rankLabel);

        var avatar = new Border
        {
            Width = 36, Height = 36, CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromArgb(30, 46, 125, 50)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = name[..2].ToUpperInvariant(),
                FontSize = 12, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#2E7D32")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        Grid.SetColumn(avatar, 1);
        grid.Children.Add(avatar);

        var nameLabel = Label(name, 14, FontWeight.SemiBold, Color.Parse("#1A2E1C"));
        nameLabel.Margin = new Thickness(10, 0, 0, 0);
        nameLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(nameLabel, 2);
        grid.Children.Add(nameLabel);

        var ptsLabel = Label(points, 13, FontWeight.Bold, Color.Parse("#4CAF50"));
        ptsLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(ptsLabel, 3);
        grid.Children.Add(ptsLabel);

        card.Child = grid;
        return card;
    }

    static Border BuildChallengeCard(string title, string participants, string timeLeft)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(14),
        };
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Label(title, 15, FontWeight.SemiBold, Color.Parse("#1A2E1C")));

        var detailRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        detailRow.Children.Add(Label(participants, 12, FontWeight.Normal, Color.Parse("#90A4AE")));
        detailRow.Children.Add(Label(timeLeft, 12, FontWeight.SemiBold, Color.Parse("#FF9800")));
        stack.Children.Add(detailRow);

        var joinBorder = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(20, 46, 125, 50)),
            Padding = new Thickness(16, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0),
            Child = Label("Join Challenge", 12, FontWeight.SemiBold, Color.Parse("#2E7D32")),
        };
        stack.Children.Add(joinBorder);

        card.Child = stack;
        return card;
    }
}
