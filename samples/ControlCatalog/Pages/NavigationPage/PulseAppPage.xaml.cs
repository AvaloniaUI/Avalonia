using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace ControlCatalog.Pages;

public partial class PulseAppPage : UserControl
{
    // Design tokens
    static readonly Color Primary = Color.Parse("#256af4");
    static readonly Color BgDark = Color.Parse("#101622");
    static readonly Color BgDashboard = Color.Parse("#0a0a0a");
    static readonly Color Surface = Color.Parse("#1a1a1a");
    static readonly Color TextWhite = Colors.White;
    static readonly Color TextMuted = Color.Parse("#94a3b8");
    static readonly Color TextDimmed = Color.Parse("#64748b");
    static readonly Color BorderDark = Color.Parse("#1e293b");

    NavigationPage? _navPage;
    ContentPage? _loginPage;
    ScrollViewer? _infoPanel;

    public PulseAppPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoPanelVisibility();

        _navPage = this.FindControl<NavigationPage>("NavPage");
        if (_navPage == null) return;

        _loginPage = BuildLoginPage();
        _navPage.Push(_loginPage);
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


    static Bitmap? LoadAsset(string fileName)
    {
        try
        {
            var uri = new Uri($"avares://ControlCatalog/Assets/Pulse/{fileName}");
            return new Bitmap(AssetLoader.Open(uri));
        }
        catch { return null; }
    }

    static Border ImageBorder(string fileName, double width, double height, double radius)
    {
        var border = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(radius),
            ClipToBounds = true,
        };
        var bmp = LoadAsset(fileName);
        if (bmp != null)
            border.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
        else
            border.Background = new SolidColorBrush(Surface);
        return border;
    }

    static TextBlock Label(string text, double size, FontWeight weight, Color color, double opacity = 1)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = new SolidColorBrush(color),
            Opacity = opacity,
        };
    }

    static StackPanel BuildMetaItem(string iconData, string text)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        row.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(iconData),
            Fill = new SolidColorBrush(TextDimmed),
            Stretch = Stretch.Uniform,
            Width = 12,
            Height = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });
        row.Children.Add(Label(text, 11, FontWeight.Normal, TextMuted));
        return row;
    }

    Button PrimaryButton(string text, double height, double fontSize)
    {
        var btn = new Button
        {
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Height = height,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Primary),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = fontSize,
        };
        btn.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Color.Parse("#1d5ad4"));
        btn.Resources["ButtonForegroundPointerOver"] = Brushes.White;
        btn.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Color.Parse("#1a4fbf"));
        btn.Resources["ButtonForegroundPressed"] = Brushes.White;
        return btn;
    }

    TextBox DarkTextBox(string watermark, char? passwordChar = null)
    {
        var tb = new TextBox
        {
            PlaceholderText = watermark,
            Height = 48,
            FontSize = 13,
            Padding = new Thickness(14, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
        };
        if (passwordChar.HasValue)
            tb.PasswordChar = passwordChar.Value;

        // Override every state that the Fluent theme targets via template-part selectors.
        // Resource overrides are resolved locally before the app-level theme values,
        // so they reliably win over /template/ Border#PART_BorderElement rules.
        tb.Resources["TextControlForeground"]              = new SolidColorBrush(TextWhite);
        tb.Resources["TextControlForegroundPointerOver"]   = new SolidColorBrush(TextWhite);
        tb.Resources["TextControlForegroundFocused"]       = new SolidColorBrush(TextWhite);
        tb.Resources["TextControlPlaceholderForeground"]   = new SolidColorBrush(TextDimmed);
        tb.Resources["TextControlBackground"]              = new SolidColorBrush(Color.Parse("#1e293b"));
        tb.Resources["TextControlBackgroundPointerOver"]   = new SolidColorBrush(Color.Parse("#253352"));
        tb.Resources["TextControlBackgroundFocused"]       = new SolidColorBrush(Color.Parse("#1e293b"));
        tb.Resources["TextControlBorderBrush"]             = new SolidColorBrush(BorderDark);
        tb.Resources["TextControlBorderBrushPointerOver"]  = new SolidColorBrush(BorderDark);
        tb.Resources["TextControlBorderBrushFocused"]      = new SolidColorBrush(Primary);
        return tb;
    }

    Button GhostButton(object content, double width, double height, CornerRadius cornerRadius, IBrush background, IBrush foreground, IBrush? borderBrush = null)
    {
        var hoverBg = background is SolidColorBrush scb
            ? new SolidColorBrush(Color.FromArgb(
                (byte)Math.Min(255, scb.Color.A + 40),
                (byte)Math.Min(255, scb.Color.R + 16),
                (byte)Math.Min(255, scb.Color.G + 16),
                (byte)Math.Min(255, scb.Color.B + 16)))
            : background;

        var btn = new Button
        {
            Content = content,
            Width = width,
            Height = height,
            CornerRadius = cornerRadius,
            Background = background,
            Foreground = foreground,
            BorderThickness = borderBrush != null ? new Thickness(1) : new Thickness(0),
            BorderBrush = borderBrush,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        btn.Resources["ButtonBackgroundPointerOver"]  = hoverBg;
        btn.Resources["ButtonForegroundPointerOver"]  = foreground;
        btn.Resources["ButtonBorderBrushPointerOver"] = borderBrush ?? Brushes.Transparent;
        btn.Resources["ButtonBackgroundPressed"]      = background;
        btn.Resources["ButtonForegroundPressed"]      = foreground;
        btn.Resources["ButtonBorderBrushPressed"]     = borderBrush ?? Brushes.Transparent;
        return btn;
    }

    static Border Chip(string text, bool active)
    {
        var chip = new Border
        {
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(18, 10),
            Background = new SolidColorBrush(active ? Primary : Surface),
            BorderThickness = new Thickness(active ? 0 : 1),
            BorderBrush = active ? null : new SolidColorBrush(BorderDark),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = active ? FontWeight.SemiBold : FontWeight.Medium,
                Foreground = new SolidColorBrush(active ? TextWhite : TextMuted),
            },
        };
        return chip;
    }


    ContentPage BuildLoginPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgDark),
        };
        NavigationPage.SetHasNavigationBar(page, false);

        var root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = BuildLoginContent(),
        };
        page.Content = root;
        return page;
    }

    Control BuildLoginContent()
    {
        var stack = new StackPanel
        {
            Spacing = 20,
            Margin = new Thickness(32, 48, 32, 32),
            MaxWidth = 400,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var logoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
        var logoBg = new Border
        {
            Width = 36, Height = 36,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Primary),
            Child = new PathIcon
            {
                Data = Geometry.Parse("M7 2v11h3v9l7-12h-4l4-8z"),
                Width = 16, Height = 16,
                Foreground = Brushes.White,
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        logoRow.Children.Add(logoBg);
        var pulseLabel = Label("PULSE.", 20, FontWeight.Bold, TextWhite);
        pulseLabel.VerticalAlignment = VerticalAlignment.Center;
        logoRow.Children.Add(pulseLabel);
        stack.Children.Add(logoRow);

        stack.Children.Add(new Border { Height = 16 }); // spacer
        stack.Children.Add(new TextBlock
        {
            Text = "Welcome Back",
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextWhite),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Train harder than yesterday.",
            FontSize = 13,
            Foreground = new SolidColorBrush(TextMuted),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, -12, 0, 0),
        });

        stack.Children.Add(new Border { Height = 8 });
        stack.Children.Add(Label("Email Address", 12, FontWeight.Medium, TextMuted));
        stack.Children.Add(DarkTextBox("name@example.com"));
        var pwdHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        pwdHeader.Children.Add(Label("Password", 12, FontWeight.Medium, TextMuted));
        var forgot = Label("Forgot?", 11, FontWeight.SemiBold, Primary);
        Grid.SetColumn(forgot, 1);
        pwdHeader.Children.Add(forgot);
        stack.Children.Add(pwdHeader);

        stack.Children.Add(DarkTextBox("Password", '\u2022'));
        var loginBtn = PrimaryButton("Login", 48, 14);
        loginBtn.Click += OnLoginClicked;
        stack.Children.Add(loginBtn);
        var dividerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Margin = new Thickness(0, 4) };
        dividerGrid.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(BorderDark), VerticalAlignment = VerticalAlignment.Center });
        var orText = new TextBlock
        {
            Text = "OR CONTINUE WITH",
            FontSize = 10,
            FontWeight = FontWeight.Medium,
            Foreground = new SolidColorBrush(TextDimmed),
            LetterSpacing = 2,
            Margin = new Thickness(12, 0),
        };
        Grid.SetColumn(orText, 1);
        dividerGrid.Children.Add(orText);
        var line2 = new Border { Height = 1, Background = new SolidColorBrush(BorderDark), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(line2, 2);
        dividerGrid.Children.Add(line2);
        stack.Children.Add(dividerGrid);
        var socials = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), HorizontalAlignment = HorizontalAlignment.Stretch };
        string[] socialLabels = { "G", "\uf8ff", "f" };
        for (int i = 0; i < 3; i++)
        {
            var btn = new Button
            {
                Content = socialLabels[i],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Height = 48,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.Parse("#1e293b")),
                Foreground = new SolidColorBrush(TextMuted),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(BorderDark),
                Margin = new Thickness(i == 0 ? 0 : 6, 0, i == 2 ? 0 : 6, 0),
                FontSize = 16,
                Padding = new Thickness(0),
            };
            btn.Resources["ButtonBackgroundPointerOver"]  = new SolidColorBrush(Color.Parse("#253352"));
            btn.Resources["ButtonForegroundPointerOver"]  = new SolidColorBrush(TextWhite);
            btn.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(BorderDark);
            btn.Resources["ButtonBackgroundPressed"]      = new SolidColorBrush(Color.Parse("#1e293b"));
            btn.Resources["ButtonForegroundPressed"]      = new SolidColorBrush(TextWhite);
            btn.Resources["ButtonBorderBrushPressed"]     = new SolidColorBrush(BorderDark);
            Grid.SetColumn(btn, i);
            socials.Children.Add(btn);
        }
        stack.Children.Add(socials);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
        footer.Children.Add(Label("New here?", 12, FontWeight.Normal, TextMuted));
        footer.Children.Add(Label("Create an account", 12, FontWeight.Bold, Primary));
        stack.Children.Add(footer);

        return stack;
    }


    void OnLoginClicked(object? sender, RoutedEventArgs e)
    {
        if (_navPage == null || _loginPage == null) return;

        var dashboard = BuildDashboardPage();
        _navPage.Push(dashboard);
        _navPage.RemovePage(_loginPage);
        _loginPage = null;
    }

    TabbedPage BuildDashboardPage()
    {
        var tp = new TabbedPage
        {
            Background = new SolidColorBrush(BgDashboard),
            BarBackground = new SolidColorBrush(BgDashboard),
            SelectedTabBrush = new SolidColorBrush(Primary),
            UnselectedTabBrush = new SolidColorBrush(TextDimmed),
            TabPlacement = TabPlacement.Bottom,
            PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200)),
        };
        tp.Resources.Add("TabItemHeaderFontSize", 12.0);
        NavigationPage.SetHasNavigationBar(tp, false);

        var homePage = BuildHomePage();
        homePage.Header = "Home";
        homePage.Icon = "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z";

        var workoutsPage = BuildWorkoutsPage();
        workoutsPage.Icon = "M20.57 14.86L22 13.43 20.57 12 17 15.57 8.43 7 12 3.43 10.57 2 9.14 3.43 7.71 2 5.57 4.14 4.14 2.71 2.71 4.14l1.43 1.43L2 7.71l1.43 1.43L2 10.57 3.43 12 7 8.43 15.57 17 12 20.57 13.43 22l1.43-1.43L16.29 22l2.14-2.14 1.43 1.43 1.43-1.43-1.43-1.43L22 16.29z";

        var profilePage = BuildProfilePage();
        profilePage.Icon = "M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z";

        tp.Pages = new ObservableCollection<object?> { homePage, workoutsPage, profilePage };

        return tp;
    }


    ContentPage BuildWorkoutsPage()
    {
        var page = new ContentPage
        {
            Header = "Workouts",
            Background = new SolidColorBrush(BgDashboard),
        };

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };
        var title = Label("My Workouts", 24, FontWeight.Bold, TextWhite);
        title.Margin = new Thickness(16, 20, 16, 4);
        root.Children.Add(title);
        var subtitle = Label("Stay consistent, stay strong", 13, FontWeight.Normal, TextMuted);
        subtitle.Margin = new Thickness(16, 0, 16, 20);
        root.Children.Add(subtitle);
        var weekCard = new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Primary, 0),
                    new GradientStop(Color.Parse("#1d3fd4"), 1),
                },
            },
            Padding = new Thickness(20),
            Margin = new Thickness(16, 0, 16, 20),
        };
        var weekStack = new StackPanel { Spacing = 16 };
        var weekHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        weekHeader.Children.Add(Label("This Week", 16, FontWeight.Bold, TextWhite));
        var streakRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        streakRow.Children.Add(new PathIcon
        {
            Data = Geometry.Parse("M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67z"),
            Width = 14, Height = 14,
            Foreground = new SolidColorBrush(Color.Parse("#f97316")),
        });
        streakRow.Children.Add(Label("5 Day Streak", 12, FontWeight.SemiBold, TextWhite, 0.85));
        Grid.SetColumn(streakRow, 1);
        weekHeader.Children.Add(streakRow);
        weekStack.Children.Add(weekHeader);
        var daysRow = new UniformGrid { Columns = 7 };
        string[] dayNames = { "M", "T", "W", "T", "F", "S", "S" };
        bool[] completed = { true, true, true, true, true, false, false };
        for (int i = 0; i < 7; i++)
        {
            var dayStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6 };
            var circle = new Border
            {
                Width = 32, Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = completed[i]
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = completed[i]
                    ? new PathIcon
                    {
                        Data = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"),
                        Width = 14, Height = 14,
                        Foreground = new SolidColorBrush(Primary),
                    }
                    : (Control)new TextBlock(),
            };
            dayStack.Children.Add(circle);
            dayStack.Children.Add(new TextBlock
            {
                Text = dayNames[i],
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(completed[i] ? (byte)255 : (byte)153, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            daysRow.Children.Add(dayStack);
        }
        weekStack.Children.Add(daysRow);
        var statsRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), Margin = new Thickness(0, 4, 0, 0) };
        statsRow.Children.Add(BuildWeekStat("5", "Workouts", 0));
        statsRow.Children.Add(BuildWeekStat("3.5h", "Duration", 1));
        statsRow.Children.Add(BuildWeekStat("2,150", "Calories", 2));
        weekStack.Children.Add(statsRow);

        weekCard.Child = weekStack;
        root.Children.Add(weekCard);
        var planHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 0, 16, 12),
        };
        planHeader.Children.Add(Label("Today's Plan", 18, FontWeight.Bold, TextWhite));
        var editLabel = Label("Edit", 12, FontWeight.SemiBold, Primary);
        Grid.SetColumn(editLabel, 1);
        editLabel.VerticalAlignment = VerticalAlignment.Center;
        planHeader.Children.Add(editLabel);
        root.Children.Add(planHeader);
        const string iconDumbbell = "M20.57 14.86L22 13.43 20.57 12 17 15.57 8.43 7 12 3.43 10.57 2 9.14 3.43 7.71 2 5.57 4.14 4.14 2.71 2.71 4.14l1.43 1.43L2 7.71l1.43 1.43L2 10.57 3.43 12 7 8.43 15.57 17 12 20.57 13.43 22l1.43-1.43L16.29 22l2.14-2.14 1.43 1.43 1.43-1.43-1.43-1.43L22 16.29z";
        const string iconFlame = "M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67z";
        const string iconYoga = "M12 2c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2zm9 7h-6v13h-2v-6h-2v6H9V9H3V7h18v2z";
        const string iconRun = "M13.49 5.48c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm-3.6 13.9l1-4.4 2.1 2v6h2v-7.5l-2.1-2 .6-3c1.3 1.5 3.3 2.5 5.5 2.5v-2c-1.9 0-3.5-1-4.3-2.4l-1-1.6c-.4-.6-1-1-1.7-1-.3 0-.5.1-.8.1l-5.2 2.2v4.7h2v-3.4l1.8-.7-1.6 8.1-4.9-1-.4 2 7 1.4z";
        const string iconCore = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z";

        root.Children.Add(BuildScheduleCard("Upper Body Strength", "Chest, Shoulders, Triceps", "45 min", "9:00 AM", Primary, iconDumbbell, true));
        root.Children.Add(BuildScheduleCard("HIIT Cardio", "Full Body Burn", "25 min", "5:30 PM", Color.Parse("#f97316"), iconFlame, false));
        var upcomingLabel = Label("Upcoming", 18, FontWeight.Bold, TextWhite);
        upcomingLabel.Margin = new Thickness(16, 20, 16, 12);
        root.Children.Add(upcomingLabel);

        root.Children.Add(BuildScheduleCard("Yoga & Recovery", "Flexibility, Core", "30 min", "Tomorrow", Color.Parse("#10b981"), iconYoga, false));
        root.Children.Add(BuildScheduleCard("Leg Day", "Quads, Glutes, Hamstrings", "50 min", "Wednesday", Color.Parse("#a855f7"), iconRun, false));
        root.Children.Add(BuildScheduleCard("Core Destroyer", "Abs, Obliques, Lower Back", "20 min", "Thursday", Color.Parse("#ef4444"), iconCore, false));

        root.Children.Add(new Border { Height = 16 });

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    static Grid BuildWeekStat(string value, string label, int col)
    {
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 2 };
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(178, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var wrapper = new Grid();
        wrapper.Children.Add(stack);
        Grid.SetColumn(wrapper, col);
        return wrapper;
    }

    Border BuildScheduleCard(string title, string muscles, string duration, string time, Color accentColor, string iconData, bool active)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Surface),
            Padding = new Thickness(14),
            Margin = new Thickness(16, 0, 16, 10),
        };
        if (active)
        {
            card.BorderBrush = new SolidColorBrush(Color.FromArgb(51, accentColor.R, accentColor.G, accentColor.B));
            card.BorderThickness = new Thickness(1);
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var iconCircle = new Border
        {
            Width = 44, Height = 44,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(26, accentColor.R, accentColor.G, accentColor.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Child = new PathIcon
            {
                Data = Geometry.Parse(iconData),
                Width = 20, Height = 20,
                Foreground = new SolidColorBrush(accentColor),
            },
        };
        grid.Children.Add(iconCircle);
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        info.Children.Add(Label(title, 14, FontWeight.Bold, TextWhite));
        info.Children.Add(Label(muscles, 11, FontWeight.Normal, TextMuted));

        var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 4, 0, 0) };
        metaRow.Children.Add(BuildMetaItem("M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13H11v6l5.2 3.2.8-1.3-4.5-2.7V7z", duration));
        metaRow.Children.Add(BuildMetaItem("M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z", time));
        info.Children.Add(metaRow);

        Grid.SetColumn(info, 1);
        grid.Children.Add(info);
        if (active)
        {
            var startBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(accentColor),
                Padding = new Thickness(12, 8),
                VerticalAlignment = VerticalAlignment.Center,
                Child = Label("Start", 11, FontWeight.Bold, TextWhite),
            };
            Grid.SetColumn(startBtn, 2);
            grid.Children.Add(startBtn);
        }
        else
        {
            var chevron = new PathIcon
            {
                Data = Geometry.Parse("M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"),
                Width = 16, Height = 16,
                Foreground = new SolidColorBrush(TextDimmed),
            };
            chevron.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(chevron, 2);
            grid.Children.Add(chevron);
        }

        card.Child = grid;
        return card;
    }


    ContentPage BuildProfilePage()
    {
        var page = new ContentPage
        {
            Header = "Profile",
            Background = new SolidColorBrush(BgDashboard),
        };

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };
        var headerPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
            Margin = new Thickness(16, 28, 16, 24),
        };

        var avatarBorder = new Border
        {
            Width = 80, Height = 80,
            CornerRadius = new CornerRadius(40),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            BorderBrush = new SolidColorBrush(Primary),
            BorderThickness = new Thickness(3),
        };
        var bmp = LoadAsset("profile_avatar.jpg");
        if (bmp != null)
            avatarBorder.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
        else
            avatarBorder.Background = new SolidColorBrush(Surface);
        headerPanel.Children.Add(avatarBorder);

        var nameLabel = Label("Alex Johnson", 20, FontWeight.Bold, TextWhite);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        headerPanel.Children.Add(nameLabel);
        var memberLabel = Label("Member since Jan 2023", 12, FontWeight.Normal, TextMuted);
        memberLabel.HorizontalAlignment = HorizontalAlignment.Center;
        headerPanel.Children.Add(memberLabel);
        root.Children.Add(headerPanel);
        var statsGrid = new UniformGrid
        {
            Columns = 3,
            Margin = new Thickness(16, 0, 16, 24),
        };
        statsGrid.Children.Add(BuildProfileStat("148", "Workouts", Primary));
        statsGrid.Children.Add(BuildProfileStat("62h", "Total Time", Color.Parse("#10b981")));
        statsGrid.Children.Add(BuildProfileStat("45.2K", "Calories", Color.Parse("#f97316")));
        root.Children.Add(statsGrid);
        var achieveLabel = Label("Achievements", 18, FontWeight.Bold, TextWhite);
        achieveLabel.Margin = new Thickness(16, 0, 16, 12);
        root.Children.Add(achieveLabel);

        var achieveScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, 24),
        };
        var achieveRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(16, 0) };
        achieveRow.Children.Add(BuildAchievementCard("Early Bird", "30 morning\nworkouts", "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z", Color.Parse("#f59e0b"), true));
        achieveRow.Children.Add(BuildAchievementCard("Iron Will", "100 workout\nmilestone", "M20.57 14.86L22 13.43 20.57 12 17 15.57 8.43 7 12 3.43 10.57 2 9.14 3.43 7.71 2 5.57 4.14 4.14 2.71 2.71 4.14l1.43 1.43L2 7.71l1.43 1.43L2 10.57 3.43 12 7 8.43 15.57 17 12 20.57 13.43 22l1.43-1.43L16.29 22l2.14-2.14 1.43 1.43 1.43-1.43-1.43-1.43L22 16.29z", Primary, true));
        achieveRow.Children.Add(BuildAchievementCard("Consistent", "30-day\nstreak", "M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67z", Color.Parse("#ef4444"), true));
        achieveRow.Children.Add(BuildAchievementCard("Marathon", "10 hours in\na week", "M13.49 5.48c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm-3.6 13.9l1-4.4 2.1 2v6h2v-7.5l-2.1-2 .6-3c1.3 1.5 3.3 2.5 5.5 2.5v-2c-1.9 0-3.5-1-4.3-2.4l-1-1.6c-.4-.6-1-1-1.7-1-.3 0-.5.1-.8.1l-5.2 2.2v4.7h2v-3.4l1.8-.7-1.6 8.1-4.9-1-.4 2 7 1.4z", Color.Parse("#a855f7"), false));
        achieveScroll.Content = achieveRow;
        root.Children.Add(achieveScroll);
        var settingsLabel = Label("Settings", 18, FontWeight.Bold, TextWhite);
        settingsLabel.Margin = new Thickness(16, 0, 16, 12);
        root.Children.Add(settingsLabel);

        root.Children.Add(BuildSettingsItem("Personal Info", "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"));
        root.Children.Add(BuildSettingsItem("Workout Preferences", "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.07.62-.07.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"));
        root.Children.Add(BuildSettingsItem("Notifications", "M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"));
        root.Children.Add(BuildSettingsItem("Connected Apps", "M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z"));
        root.Children.Add(BuildSettingsItem("Help & Support", "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm2.07-7.75l-.9.92C13.45 12.9 13 13.5 13 15h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H8c0-2.21 1.79-4 4-4s4 1.79 4 4c0 .88-.36 1.68-.93 2.25z"));

        root.Children.Add(new Border { Height = 16 });

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    static Border BuildProfileStat(string value, string label, Color accentColor)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Surface),
            Padding = new Thickness(12, 16),
            Margin = new Thickness(4),
        };
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(accentColor),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = new SolidColorBrush(TextMuted),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        card.Child = stack;
        return card;
    }

    static Border BuildAchievementCard(string title, string description, string iconData, Color color, bool unlocked)
    {
        var card = new Border
        {
            Width = 120,
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Surface),
            Padding = new Thickness(12, 16),
            Opacity = unlocked ? 1.0 : 0.5,
        };
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 8 };

        var iconBg = new Border
        {
            Width = 44, Height = 44,
            CornerRadius = new CornerRadius(22),
            Background = new SolidColorBrush(Color.FromArgb(26, color.R, color.G, color.B)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new PathIcon
            {
                Data = Geometry.Parse(iconData),
                Width = 20, Height = 20,
                Foreground = new SolidColorBrush(color),
            },
        };
        stack.Children.Add(iconBg);

        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextWhite),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 10,
            Foreground = new SolidColorBrush(TextMuted),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        });
        card.Child = stack;
        return card;
    }

    Border BuildSettingsItem(string title, string iconData)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Surface),
            Padding = new Thickness(14, 12),
            Margin = new Thickness(16, 0, 16, 8),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        var iconBg = new Border
        {
            Width = 36, Height = 36,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            Child = new PathIcon
            {
                Data = Geometry.Parse(iconData),
                Width = 16, Height = 16,
                Foreground = new SolidColorBrush(Primary),
            },
            VerticalAlignment = VerticalAlignment.Center,
        };
        grid.Children.Add(iconBg);

        var label = Label(title, 14, FontWeight.Medium, TextWhite);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(label, 1);
        grid.Children.Add(label);

        var chevron = new PathIcon
        {
            Data = Geometry.Parse("M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"),
            Width = 14, Height = 14,
            Foreground = new SolidColorBrush(TextDimmed),
        };
        chevron.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(chevron, 2);
        grid.Children.Add(chevron);

        card.Child = grid;
        return card;
    }

    ContentPage BuildHomePage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgDashboard),
        };

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(16, 20, 16, 8),
        };

        var avatar = ImageBorder("profile_avatar.jpg", 44, 44, 22);
        avatar.BorderBrush = new SolidColorBrush(Color.FromArgb(77, Primary.R, Primary.G, Primary.B));
        avatar.BorderThickness = new Thickness(2);
        headerGrid.Children.Add(avatar);

        var greetCol = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var welcomeLabel = Label("WELCOME BACK", 10, FontWeight.Medium, TextDimmed);
        welcomeLabel.LetterSpacing = 2;
        greetCol.Children.Add(welcomeLabel);
        greetCol.Children.Add(Label("Alex Johnson", 18, FontWeight.Bold, TextWhite));
        Grid.SetColumn(greetCol, 1);
        headerGrid.Children.Add(greetCol);

        var bellBtn = new Button
        {
            Width = 40, Height = 40,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Surface),
            Foreground = new SolidColorBrush(TextMuted),
            Padding = new Thickness(8),
            BorderThickness = new Thickness(0),
            Content = new PathIcon
            {
                Data = Geometry.Parse("M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"),
                Width = 18, Height = 18,
            },
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(bellBtn, 2);
        headerGrid.Children.Add(bellBtn);
        root.Children.Add(headerGrid);

        var chipsScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 8, 0, 16),
        };
        var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(16, 0) };
        chips.Children.Add(Chip("All Workouts", true));
        chips.Children.Add(Chip("Beginner", false));
        chips.Children.Add(Chip("15-30 min", false));
        chips.Children.Add(Chip("Equipment", false));
        chipsScroll.Content = chips;
        root.Children.Add(chipsScroll);

        var catHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 0, 16, 12),
        };
        catHeader.Children.Add(Label("Categories", 18, FontWeight.Bold, TextWhite));
        var seeAll = Label("See All", 12, FontWeight.SemiBold, Primary);
        Grid.SetColumn(seeAll, 1);
        seeAll.VerticalAlignment = VerticalAlignment.Center;
        catHeader.Children.Add(seeAll);
        root.Children.Add(catHeader);

        var catScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var catRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(16, 0) };
        catRow.Children.Add(BuildCategoryCard("HIIT", "12 Sessions", "cat_hiit.jpg"));
        catRow.Children.Add(BuildCategoryCard("Strength", "18 Sessions", "cat_strength.jpg"));
        catRow.Children.Add(BuildCategoryCard("Yoga", "8 Sessions", "cat_yoga.jpg"));
        catScroll.Content = catRow;
        root.Children.Add(catScroll);

        root.Children.Add(new Border { Height = 20 });
        root.Children.Add(new TextBlock
        {
            Text = "Recommended for You",
            FontSize = 18, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextWhite),
            Margin = new Thickness(16, 0, 16, 12),
        });

        root.Children.Add(BuildRecommendedCard("Full Body Ignite", "Intermediate", "32 min", "450 kcal", "rec_fullbody.jpg", Primary, true));
        root.Children.Add(BuildRecommendedCard("Sunrise Mobility", "Beginner", "15 min", "120 kcal", "rec_mobility.jpg", Color.Parse("#10b981"), true));
        root.Children.Add(BuildRecommendedCard("Power Core 2.0", "Advanced", "45 min", "600 kcal", "rec_powercore.jpg", Color.Parse("#f97316"), true));

        root.Children.Add(new Border { Height = 16 }); // bottom padding

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    Border BuildCategoryCard(string title, string subtitle, string imageFile)
    {
        var card = new Border
        {
            Width = 140,
            Height = 190,
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
        };
        var bmp = LoadAsset(imageFile);
        if (bmp != null)
            card.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
        else
            card.Background = new SolidColorBrush(Surface);

        // Gradient overlay + text
        var overlay = new Panel();
        overlay.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(51, 0, 0, 0), 0.4),
                    new GradientStop(Color.FromArgb(204, 0, 0, 0), 1),
                },
            },
        });
        var textStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(12, 0, 12, 12),
        };
        textStack.Children.Add(Label(title, 16, FontWeight.Bold, TextWhite));
        textStack.Children.Add(Label(subtitle, 11, FontWeight.Light, TextMuted));
        overlay.Children.Add(textStack);

        card.Child = overlay;
        return card;
    }

    Border BuildRecommendedCard(string title, string level, string duration, string calories, string imageFile, Color levelColor, bool navigateOnTap = false)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Surface),
            Padding = new Thickness(12),
            Margin = new Thickness(16, 0, 16, 12),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };
        if (navigateOnTap)
            card.PointerPressed += (_, _) => PushWorkoutDetail();

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var thumb = ImageBorder(imageFile, 80, 80, 12);
        grid.Children.Add(thumb);
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) };
        info.Children.Add(new TextBlock
        {
            Text = level.ToUpperInvariant(),
            FontSize = 9,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(levelColor),
            LetterSpacing = 1,
            Margin = new Thickness(0, 0, 0, 2),
        });
        info.Children.Add(Label(title, 14, FontWeight.Bold, TextWhite));

        var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 4, 0, 0) };
        meta.Children.Add(BuildMetaItem("M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13H11v6l5.2 3.2.8-1.3-4.5-2.7V7z", duration));
        meta.Children.Add(BuildMetaItem("M13.5.67s.74 2.65.74 4.8c0 2.06-1.35 3.73-3.41 3.73-2.07 0-3.63-1.67-3.63-3.73l.03-.36C5.21 7.51 4 10.62 4 14c0 4.42 3.58 8 8 8s8-3.58 8-8C20 8.61 17.41 3.8 13.5.67zM11.71 19c-1.78 0-3.22-1.4-3.22-3.14 0-1.62 1.05-2.76 2.81-3.12 1.77-.36 3.6-1.21 4.62-2.58.39 1.29.59 2.65.59 4.04 0 2.65-2.15 4.8-4.8 4.8z", calories));
        info.Children.Add(meta);

        Grid.SetColumn(info, 1);
        grid.Children.Add(info);
        var playBtn = new Button
        {
            Width = 40, Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            Foreground = new SolidColorBrush(Primary),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = new PathIcon
            {
                Data = Geometry.Parse("M8 5v14l11-7z"),
                Width = 16, Height = 16,
            },
            VerticalAlignment = VerticalAlignment.Center,
        };
        playBtn.Click += (_, _) => PushWorkoutDetail();
        Grid.SetColumn(playBtn, 2);
        grid.Children.Add(playBtn);

        card.Child = grid;
        return card;
    }


    void PushWorkoutDetail()
    {
        if (_navPage == null) return;
        var detail = BuildWorkoutDetailPage();
        _navPage.Push(detail);
    }

    ContentPage BuildWorkoutDetailPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgDark),
        };
        NavigationPage.SetHasNavigationBar(page, false);

        var root = new Panel();

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        var content = new StackPanel { Spacing = 0 };

        var hero = new Panel { Height = 280 };
        var heroBg = new Border { ClipToBounds = true };
        var bmp = LoadAsset("workout_hero.jpg");
        if (bmp != null)
            heroBg.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
        else
            heroBg.Background = new SolidColorBrush(Surface);
        hero.Children.Add(heroBg);

        // Gradient overlay
        hero.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(102, BgDark.R, BgDark.G, BgDark.B), 0),
                    new GradientStop(Color.FromArgb(230, BgDark.R, BgDark.G, BgDark.B), 1),
                },
            },
        });
        var backBtn = GhostButton(
            new PathIcon
            {
                Data = Geometry.Parse("M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"),
                Width = 18, Height = 18,
                Foreground = Brushes.White,
            },
            40, 40,
            new CornerRadius(8),
            new SolidColorBrush(Color.FromArgb(153, BgDark.R, BgDark.G, BgDark.B)),
            Brushes.White,
            new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)));
        backBtn.VerticalAlignment = VerticalAlignment.Top;
        backBtn.HorizontalAlignment = HorizontalAlignment.Left;
        backBtn.Margin = new Thickness(16, 16, 0, 0);
        backBtn.Click += (_, _) => _navPage?.Pop();
        hero.Children.Add(backBtn);
        var heroText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 16, 16),
        };
        heroText.Children.Add(Label("Full Body Strength", 26, FontWeight.Bold, TextWhite));
        heroText.Children.Add(new Border { Height = 8 });

        var badges = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        badges.Children.Add(BuildBadge("45 min"));
        badges.Children.Add(BuildBadge("Intermediate"));
        heroText.Children.Add(badges);
        hero.Children.Add(heroText);

        content.Children.Add(hero);

        var exHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 20, 16, 12),
        };
        exHeader.Children.Add(Label("Exercises", 18, FontWeight.Bold, TextWhite));
        var movCount = Label("5 Movements", 12, FontWeight.Medium, Primary);
        Grid.SetColumn(movCount, 1);
        movCount.VerticalAlignment = VerticalAlignment.Center;
        exHeader.Children.Add(movCount);
        content.Children.Add(exHeader);

        content.Children.Add(BuildExerciseCard("Bench Press", "Chest, Triceps", "4 Sets", "10 Reps", "ex_bench.jpg"));
        content.Children.Add(BuildExerciseCard("Barbell Squats", "Quads, Glutes", "3 Sets", "12 Reps", "ex_squats.jpg"));
        content.Children.Add(BuildExerciseCard("Deadlifts", "Back, Hamstrings", "3 Sets", "8 Reps", "ex_deadlifts.jpg"));
        content.Children.Add(BuildExerciseCard("Overhead Press", "Shoulders, Triceps", "3 Sets", "10 Reps", "ex_overhead.jpg"));
        content.Children.Add(BuildExerciseCard("Pull Ups", "Back, Biceps", "3 Sets", "Failure", "ex_pullups.jpg"));

        content.Children.Add(new Border { Height = 80 }); // space for floating button

        scroll.Content = content;
        root.Children.Add(scroll);

        var startBtn = PrimaryButton("Start Workout", 52, 15);
        startBtn.Margin = new Thickness(16, 0, 16, 16);
        startBtn.VerticalAlignment = VerticalAlignment.Bottom;
        root.Children.Add(startBtn);

        page.Content = root;
        return page;
    }

    static Border BuildBadge(string text)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(51, Primary.R, Primary.G, Primary.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(77, Primary.R, Primary.G, Primary.B)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 4),
            Child = Label(text, 12, FontWeight.Medium, Color.Parse("#e2e8f0")),
        };
    }

    Border BuildExerciseCard(string name, string muscles, string sets, string reps, string imageFile)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(BorderDark),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.Parse("#0f172a")),
            Padding = new Thickness(10),
            Margin = new Thickness(16, 0, 16, 10),
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var thumb = ImageBorder(imageFile, 56, 56, 8);
        thumb.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(thumb);
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        info.Children.Add(Label(name, 14, FontWeight.Bold, TextWhite));
        info.Children.Add(Label(muscles, 11, FontWeight.Normal, TextMuted));
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);
        var setsCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 4 };
        setsCol.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(999),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            Padding = new Thickness(8, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new TextBlock
            {
                Text = sets.ToUpperInvariant(),
                FontSize = 9,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Primary),
            },
        });
        setsCol.Children.Add(new TextBlock
        {
            Text = reps,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(TextMuted),
            HorizontalAlignment = HorizontalAlignment.Right,
        });
        Grid.SetColumn(setsCol, 2);
        grid.Children.Add(setsCol);

        card.Child = grid;
        return card;
    }
}
