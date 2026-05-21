using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AvaCarouselPage = Avalonia.Controls.CarouselPage;

namespace ControlCatalog.Pages;

public partial class CareCompanionAppPage : UserControl
{
    static readonly Color Primary = Color.Parse("#137fec");
    static readonly Color PrimaryDark = Color.Parse("#0a5bb5");
    static readonly Color PrimaryLight = Color.Parse("#e0f0ff");
    static readonly Color BgLight = Color.Parse("#f6f7f8");
    static readonly Color TextDark = Color.Parse("#111827");
    static readonly Color TextMuted = Color.Parse("#64748b");
    static readonly Color CardBg = Colors.White;
    static readonly Color SuccessGreen = Color.Parse("#10b981");
    static readonly Color WarningAmber = Color.Parse("#f59e0b");

    NavigationPage? _navPage;
    AvaCarouselPage? _onboarding;
    ScrollViewer? _infoPanel;

    public CareCompanionAppPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoVisibility();

        _navPage = this.FindControl<NavigationPage>("NavPage");
        if (_navPage == null) return;

        _onboarding = BuildOnboardingCarousel();
        _ = _navPage.PushAsync(_onboarding);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty)
            UpdateInfoVisibility();
    }

    void UpdateInfoVisibility()
    {
        if (_infoPanel != null)
            _infoPanel.IsVisible = Bounds.Width >= 650;
    }

    static TextBlock Txt(string text, double size, FontWeight weight, Color color,
        double opacity = 1, TextAlignment align = TextAlignment.Left,
        TextWrapping wrap = TextWrapping.NoWrap)
        => new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = new SolidColorBrush(color),
            Opacity = opacity,
            TextAlignment = align,
            TextWrapping = wrap,
        };

    static Button StyledButton(object content, IBrush bg, IBrush fg, double height,
        CornerRadius radius, Thickness margin = default, double fontSize = 14,
        FontWeight fontWeight = FontWeight.SemiBold,
        IBrush? border = null, Thickness borderThick = default)
    {
        var btn = new Button
        {
            Content = content,
            Background = bg,
            Foreground = fg,
            Height = height,
            CornerRadius = radius,
            Margin = margin,
            Padding = new Thickness(16, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderBrush = border,
            BorderThickness = borderThick,
        };

        var over = new Style(x => x.OfType<Button>().Class(":pointerover").Descendant().OfType<ContentPresenter>());
        over.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, bg));
        over.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, fg));
        btn.Styles.Add(over);

        var press = new Style(x => x.OfType<Button>().Class(":pressed").Descendant().OfType<ContentPresenter>());
        press.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, bg));
        press.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, fg));
        btn.Styles.Add(press);

        return btn;
    }

    static PathIcon SvgIcon(string data, double size, Color color)
        => new PathIcon
        {
            Data = Geometry.Parse(data),
            Width = size,
            Height = size,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

    static PipsPager MakePipsPager(int count, AvaCarouselPage carousel)
    {
        var pager = new PipsPager
        {
            NumberOfPages = count,
            IsPreviousButtonVisible = false,
            IsNextButtonVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        pager.Bind(PipsPager.SelectedPageIndexProperty,
            new Avalonia.Data.Binding("SelectedIndex") { Source = carousel, Mode = Avalonia.Data.BindingMode.TwoWay });

        return pager;
    }

    static Border ShadowWrap(Control ctrl, Color shadowColor)
        => new Border
        {
            CornerRadius = new CornerRadius(999),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 8,
                Blur = 24,
                Color = Color.FromArgb(55, shadowColor.R, shadowColor.G, shadowColor.B),
            }),
            Child = ctrl,
        };

    AvaCarouselPage BuildOnboardingCarousel()
    {
        var carousel = new AvaCarouselPage
        {
            Background = new SolidColorBrush(BgLight),
            PageTransition = new CrossFade(TimeSpan.FromMilliseconds(300)),
        };
        NavigationPage.SetHasNavigationBar(carousel, false);

        var pips1 = MakePipsPager(3, carousel);
        var pips2 = MakePipsPager(3, carousel);
        var pips3 = MakePipsPager(3, carousel);

        var p1 = BuildWelcomePage(carousel, pips1);
        var p2 = BuildTrackPage(carousel, pips2);
        var p3 = BuildResourcesPage(carousel, pips3);

        carousel.Pages = new ObservableCollection<Page> { p1, p2, p3 };

        return carousel;
    }

    ContentPage BuildWelcomePage(AvaCarouselPage carousel, PipsPager dots)
    {
        var page = new ContentPage { Background = new SolidColorBrush(CardBg) };

        var skipBtn = StyledButton("Skip", Brushes.Transparent, new SolidColorBrush(TextMuted),
            32, new CornerRadius(999));
        skipBtn.HorizontalAlignment = HorizontalAlignment.Right;
        skipBtn.Margin = new Thickness(0, 4, 8, 0);
        skipBtn.Click += (_, _) => CompleteOnboarding();

        var illGrad1 = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        illGrad1.GradientStops.Add(new GradientStop(Color.Parse("#dbeafe"), 0));
        illGrad1.GradientStops.Add(new GradientStop(Color.Parse("#93c5fd"), 0.5));
        illGrad1.GradientStops.Add(new GradientStop(Color.Parse("#3b82f6"), 1));

        var illPanel1 = new Panel { Background = illGrad1 };

        illPanel1.Children.Add(new Border
        {
            Width = 160,
            Height = 160,
            CornerRadius = new CornerRadius(80),
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });

        illPanel1.Children.Add(new Border
        {
            Width = 80,
            Height = 80,
            CornerRadius = new CornerRadius(20),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            BoxShadow = BoxShadows.Parse("0 8 24 0 #0000001a"),
            Child = SvgIcon(
                "M12 21.593c-5.63-5.539-11-10.297-11-14.402 0-3.791 3.068-5.191 5.281-5.191 1.312 0 4.151.501 5.719 4.457 1.59-3.968 4.464-4.447 5.726-4.447 2.54 0 5.274 1.621 5.274 5.181 0 4.069-5.136 8.625-11 14.402z",
                38, Color.Parse("#3b82f6")),
        });

        illPanel1.Children.Add(new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(24, 0, 0, 24),
            BoxShadow = BoxShadows.Parse("0 4 12 0 #0000001a"),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new Border
                    {
                        Width = 8,
                        Height = 8,
                        CornerRadius = new CornerRadius(4),
                        Background = new SolidColorBrush(SuccessGreen),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    Txt("Your health, simplified", 10, FontWeight.SemiBold, TextDark),
                },
            },
        });

        illPanel1.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(22),
            Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 20, 28, 0),
            Child = SvgIcon("M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z", 20, Colors.White),
        });

        var imgCard1 = new Border
        {
            Height = 210,
            CornerRadius = new CornerRadius(20),
            ClipToBounds = true,
            Margin = new Thickness(20, 6, 20, 0),
            Child = illPanel1,
        };

        var textArea = new StackPanel { Margin = new Thickness(28, 20, 28, 0), Spacing = 10 };
        var titleStack1 = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
        titleStack1.Children.Add(Txt("Welcome to Your", 26, FontWeight.Bold, TextDark, align: TextAlignment.Center));
        titleStack1.Children.Add(Txt("Care Companion", 28, FontWeight.ExtraBold, Primary, align: TextAlignment.Center));
        textArea.Children.Add(titleStack1);
        textArea.Children.Add(Txt(
            "We are here to support you through every step of your treatment journey. Track symptoms, manage appointments, and stay connected.",
            13, FontWeight.Normal, TextMuted, align: TextAlignment.Center, wrap: TextWrapping.Wrap));

        var nextRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6,
        };
        nextRow.Children.Add(Txt("Next", 15, FontWeight.SemiBold, Colors.White));
        nextRow.Children.Add(SvgIcon("M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8z", 12, Colors.White));

        var nextBtn = StyledButton(nextRow, new SolidColorBrush(Primary), Brushes.White, 52, new CornerRadius(999));
        nextBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        nextBtn.Click += (_, _) => carousel.SelectedIndex = 1;

        var nextBtnWrap = ShadowWrap(nextBtn, Primary);
        nextBtnWrap.HorizontalAlignment = HorizontalAlignment.Stretch;

        var bottomArea = new StackPanel { Margin = new Thickness(24, 16, 24, 36), Spacing = 20 };
        bottomArea.Children.Add(dots);
        bottomArea.Children.Add(nextBtnWrap);

        var middleStack = new StackPanel { Spacing = 0 };
        middleStack.Children.Add(imgCard1);
        middleStack.Children.Add(textArea);

        var middleScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Content = middleStack,
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetRow(skipBtn, 0);
        Grid.SetRow(middleScroll, 1);
        Grid.SetRow(bottomArea, 2);
        grid.Children.Add(skipBtn);
        grid.Children.Add(middleScroll);
        grid.Children.Add(bottomArea);

        page.Content = grid;
        return page;
    }

    ContentPage BuildTrackPage(AvaCarouselPage carousel, PipsPager dots)
    {
        var page = new ContentPage { Background = new SolidColorBrush(CardBg) };

        var skipBtn = StyledButton("Skip", Brushes.Transparent, new SolidColorBrush(TextMuted),
            32, new CornerRadius(999));
        skipBtn.HorizontalAlignment = HorizontalAlignment.Right;
        skipBtn.Margin = new Thickness(0, 4, 8, 0);
        skipBtn.Click += (_, _) => CompleteOnboarding();

        var illGrad2 = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        illGrad2.GradientStops.Add(new GradientStop(Color.Parse("#0ea5e9"), 0));
        illGrad2.GradientStops.Add(new GradientStop(Color.Parse("#6366f1"), 1));

        var illPanel2 = new Panel { Background = illGrad2 };

        int[] barH = { 48, 72, 40, 96, 64, 80, 56 };
        string[] barD = { "M", "T", "W", "T", "F", "S", "S" };
        var chartInner = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 10),
        };
        for (int ci = 0; ci < barH.Length; ci++)
        {
            var barCol = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Bottom };
            barCol.Children.Add(new Border
            {
                Width = 20,
                Height = barH[ci],
                CornerRadius = new CornerRadius(5, 5, 0, 0),
                Background = new SolidColorBrush(ci == 3 ? Colors.White : Color.FromArgb(160, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Bottom,
            });
            barCol.Children.Add(Txt(barD[ci], 9, FontWeight.Medium, Colors.White, 0.7, align: TextAlignment.Center));
            chartInner.Children.Add(barCol);
        }

        illPanel2.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14, 14, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = chartInner,
        });

        illPanel2.Children.Add(new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 7),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(22, 20, 0, 0),
            BoxShadow = BoxShadows.Parse("0 4 12 0 #0000001a"),
            Child = new StackPanel
            {
                Spacing = 1,
                Children =
                {
                    Txt("Weekly Score", 9, FontWeight.SemiBold, TextMuted),
                    Txt("\u2191 18%", 13, FontWeight.Bold, Color.Parse("#0ea5e9")),
                },
            },
        });

        illPanel2.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 22, 24, 0),
            Child = SvgIcon(
                "M16 6l2.29 2.29-4.88 4.88-4-4L2 16.59 3.41 18l6-6 4 4 6.3-6.29L22 12V6z",
                16, Colors.White),
        });

        var imgCard2 = new Border
        {
            Height = 210,
            CornerRadius = new CornerRadius(20),
            ClipToBounds = true,
            Margin = new Thickness(20, 6, 20, 0),
            Child = illPanel2,
        };

        var iconBadge = new Border
        {
            Width = 52,
            Height = 52,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.Parse("#eff6ff")),
            Margin = new Thickness(0, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = SvgIcon(
                "M16 6l2.29 2.29-4.88 4.88-4-4L2 16.59 3.41 18l6-6 4 4 6.3-6.29L22 12V6z",
                24, Primary),
        };

        var textArea = new StackPanel { Margin = new Thickness(28, 10, 28, 0), Spacing = 10 };
        textArea.Children.Add(Txt("Track and Understand", 24, FontWeight.Bold, TextDark,
            align: TextAlignment.Center, wrap: TextWrapping.Wrap));
        textArea.Children.Add(Txt(
            "Easily log your symptoms and side effects to share with your medical team for better care.",
            13, FontWeight.Normal, TextMuted, align: TextAlignment.Center, wrap: TextWrapping.Wrap));

        var backRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6,
        };
        backRow.Children.Add(SvgIcon("M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z", 12, TextDark));
        backRow.Children.Add(Txt("Back", 15, FontWeight.SemiBold, TextDark));

        var nextRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6,
        };
        nextRow.Children.Add(Txt("Next", 15, FontWeight.SemiBold, Colors.White));
        nextRow.Children.Add(SvgIcon("M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8z", 12, Colors.White));

        var backBtn = StyledButton(backRow, new SolidColorBrush(Color.Parse("#f3f4f6")),
            new SolidColorBrush(TextDark), 52, new CornerRadius(999));
        backBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        backBtn.Click += (_, _) => carousel.SelectedIndex = 0;

        var nextBtn = StyledButton(nextRow, new SolidColorBrush(Primary), Brushes.White, 52, new CornerRadius(999));
        nextBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        nextBtn.Click += (_, _) => carousel.SelectedIndex = 2;

        var nextBtnWrap2 = ShadowWrap(nextBtn, Primary);
        nextBtnWrap2.HorizontalAlignment = HorizontalAlignment.Stretch;

        var navGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,16,*") };
        Grid.SetColumn(backBtn, 0);
        Grid.SetColumn(nextBtnWrap2, 2);
        navGrid.Children.Add(backBtn);
        navGrid.Children.Add(nextBtnWrap2);

        var bottomArea = new StackPanel { Margin = new Thickness(24, 16, 24, 36), Spacing = 20 };
        bottomArea.Children.Add(dots);
        bottomArea.Children.Add(navGrid);

        var middleStack = new StackPanel { Spacing = 0 };
        middleStack.Children.Add(imgCard2);
        middleStack.Children.Add(iconBadge);
        middleStack.Children.Add(textArea);

        var middleScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Content = middleStack,
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetRow(skipBtn, 0);
        Grid.SetRow(middleScroll, 1);
        Grid.SetRow(bottomArea, 2);
        grid.Children.Add(skipBtn);
        grid.Children.Add(middleScroll);
        grid.Children.Add(bottomArea);

        page.Content = grid;
        return page;
    }

    ContentPage BuildResourcesPage(AvaCarouselPage carousel, PipsPager dots)
    {
        var page = new ContentPage { Background = new SolidColorBrush(CardBg) };

        var skipBtn = StyledButton("Skip", Brushes.Transparent, new SolidColorBrush(TextMuted),
            32, new CornerRadius(999));
        skipBtn.HorizontalAlignment = HorizontalAlignment.Right;
        skipBtn.Margin = new Thickness(0, 4, 8, 0);
        skipBtn.Click += (_, _) => CompleteOnboarding();

        var illPanel = new Panel { Background = new SolidColorBrush(Color.Parse("#eef4ff")) };

        illPanel.Children.Add(new Border
        {
            Width = 72,
            Height = 72,
            CornerRadius = new CornerRadius(36),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 32),
            BoxShadow = BoxShadows.Parse("0 4 16 0 #0000001a"),
            Child = SvgIcon(
                "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z",
                32, Primary),
        });

        illPanel.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.Parse("#10b981")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 22, 44, 0),
            Child = SvgIcon("M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z", 17, Colors.White),
        });

        illPanel.Children.Add(new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            Background = new SolidColorBrush(Color.Parse("#8b5cf6")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 20, 0),
            Child = SvgIcon(
                "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z",
                15, Colors.White),
        });

        var avatarGrad = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        avatarGrad.GradientStops.Add(new GradientStop(Color.Parse("#93c5fd"), 0));
        avatarGrad.GradientStops.Add(new GradientStop(Primary, 1));
        illPanel.Children.Add(new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = avatarGrad,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(32, -20, 0, 0),
            Child = SvgIcon(
                "M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z",
                22, Colors.White),
        });

        var csIconBorder = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.Parse("#eff6ff")),
            VerticalAlignment = VerticalAlignment.Center,
            Child = SvgIcon(
                "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z",
                14, Primary),
        };
        var csText = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        csText.Children.Add(Txt("Community Support", 10, FontWeight.SemiBold, TextDark));
        csText.Children.Add(Txt("Connect with others and experts.", 9, FontWeight.Normal, TextMuted,
            wrap: TextWrapping.Wrap));
        var csInner = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        csInner.Children.Add(csIconBorder);
        csInner.Children.Add(csText);
        illPanel.Children.Add(new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 7),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 64, 14),
            BoxShadow = BoxShadows.Parse("0 2 8 0 #0000001a"),
            Child = csInner,
        });

        var illCard = new Border
        {
            Height = 210,
            CornerRadius = new CornerRadius(20),
            ClipToBounds = true,
            Margin = new Thickness(20, 6, 20, 0),
            Child = illPanel,
        };

        var textArea = new StackPanel { Margin = new Thickness(28, 20, 28, 0), Spacing = 10 };
        textArea.Children.Add(Txt("Stay Informed and Connected", 24, FontWeight.Bold, TextDark,
            align: TextAlignment.Center, wrap: TextWrapping.Wrap));
        textArea.Children.Add(Txt(
            "Access expert resources and manage your appointments all in one place.",
            13, FontWeight.Normal, TextMuted, align: TextAlignment.Center, wrap: TextWrapping.Wrap));

        var gsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6,
        };
        gsRow.Children.Add(Txt("Get Started", 15, FontWeight.SemiBold, Colors.White));
        gsRow.Children.Add(SvgIcon("M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8z", 12, Colors.White));

        var getStartedBtn = StyledButton(gsRow, new SolidColorBrush(Primary), Brushes.White, 52, new CornerRadius(999));
        getStartedBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        getStartedBtn.Click += (_, _) => CompleteOnboarding();

        var getStartedWrap = ShadowWrap(getStartedBtn, Primary);
        getStartedWrap.HorizontalAlignment = HorizontalAlignment.Stretch;

        var loginRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 0,
        };
        loginRow.Children.Add(new TextBlock
        {
            Text = "Already have an account? ",
            FontSize = 13,
            Foreground = new SolidColorBrush(TextMuted),
            VerticalAlignment = VerticalAlignment.Center,
        });
        var loginLink = new TextBlock
        {
            Text = "Log In",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Primary),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        loginLink.PointerReleased += (_, _) => CompleteOnboarding();
        loginRow.Children.Add(loginLink);

        var bottomArea = new StackPanel { Margin = new Thickness(24, 16, 24, 28), Spacing = 16 };
        bottomArea.Children.Add(dots);
        bottomArea.Children.Add(getStartedWrap);
        bottomArea.Children.Add(loginRow);

        var middleStack = new StackPanel { Spacing = 0 };
        middleStack.Children.Add(illCard);
        middleStack.Children.Add(textArea);

        var middleScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Content = middleStack,
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetRow(skipBtn, 0);
        Grid.SetRow(middleScroll, 1);
        Grid.SetRow(bottomArea, 2);
        grid.Children.Add(skipBtn);
        grid.Children.Add(middleScroll);
        grid.Children.Add(bottomArea);

        page.Content = grid;
        return page;
    }

    async void CompleteOnboarding()
    {
        if (_navPage == null || _onboarding == null) return;
        var dashboard = BuildDashboard();
        await _navPage.PushAsync(dashboard);
        _navPage.RemovePage(_onboarding);
        _onboarding = null;
    }

    TabbedPage BuildDashboard()
    {
        var tp = new TabbedPage { Background = new SolidColorBrush(BgLight), TabPlacement = TabPlacement.Bottom, };
        tp.Resources["TabbedPageTabStripBackground"] = Brushes.White;
        tp.Resources["TabbedPageTabItemHeaderForegroundSelected"] = new SolidColorBrush(Primary);
        tp.Resources["TabbedPageTabItemHeaderForegroundUnselected"] = new SolidColorBrush(TextMuted);
        NavigationPage.SetHasNavigationBar(tp, false);

        var home = BuildHomeTab();
        home.Header = "Home";
        home.Icon = new PathIcon { Data = Geometry.Parse("M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z") };

        tp.Pages = new ObservableCollection<Page>
        {
            home,
            PlaceholderTab("Care Plan",
                "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z",
                "Your personalized care plan will appear here."),
            PlaceholderTab("Messages",
                "M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2z",
                "Messages from your care team will appear here."),
            PlaceholderTab("Library",
                "M21 5c-1.11-.35-2.33-.5-3.5-.5-1.95 0-4.05.4-5.5 1.5-1.45-1.1-3.55-1.5-5.5-1.5S2.45 4.9 1 6v14.65c0 .25.25.5.5.5.1 0 .15-.05.25-.05C3.1 20.45 5.05 20 6.5 20c1.95 0 4.05.4 5.5 1.5 1.35-.85 3.8-1.5 5.5-1.5 1.65 0 3.35.3 4.75 1.05.1.05.15.05.25.05.25 0 .5-.25.5-.5V6c-.6-.45-1.25-.75-2-1z",
                "Educational resources and guides will appear here."),
            PlaceholderTab("Profile",
                "M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z",
                "Your profile and settings will appear here."),
        };

        return tp;
    }

    static ContentPage PlaceholderTab(string header, string iconData, string message)
        => new ContentPage
        {
            Header = header,
            Icon = new PathIcon { Data = Geometry.Parse(iconData) },
            Background = new SolidColorBrush(BgLight),
            Content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12,
                Margin = new Thickness(32),
                Children =
                {
                    SvgIcon(iconData, 48, Color.Parse("#d1d5db")),
                    Txt(header, 20, FontWeight.Bold, TextDark, align: TextAlignment.Center),
                    Txt(message, 13, FontWeight.Normal, TextMuted,
                        align: TextAlignment.Center, wrap: TextWrapping.Wrap),
                },
            },
        };

    ContentPage BuildHomeTab()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgLight) };
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var headerBorder = new Border { Background = Brushes.White, Padding = new Thickness(16, 20, 16, 16) };
        var hGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        var greetStack = new StackPanel { Spacing = 2 };
        greetStack.Children.Add(Txt("Tuesday, Oct 24", 12, FontWeight.Normal, TextMuted));
        greetStack.Children.Add(Txt("Good Morning, Sarah", 20, FontWeight.Bold, TextDark));
        hGrid.Children.Add(greetStack);

        var bellContainer = new Panel { Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center };
        bellContainer.Children.Add(new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(Color.Parse("#f3f4f6")),
            Child = SvgIcon(
                "M12 22c1.1 0 2-.9 2-2h-4c0 1.1.89 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z",
                20, TextDark),
        });
        bellContainer.Children.Add(new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = Brushes.Red,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.5),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 1, 1, 0),
        });
        Grid.SetColumn(bellContainer, 1);
        hGrid.Children.Add(bellContainer);
        headerBorder.Child = hGrid;
        root.Children.Add(headerBorder);
        root.Children.Add(new Border { Height = 12 });

        var weeklyGrad = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
        };
        weeklyGrad.GradientStops.Add(new GradientStop(Primary, 0));
        weeklyGrad.GradientStops.Add(new GradientStop(PrimaryDark, 1));

        var weeklyCard = new Border
        {
            Background = weeklyGrad,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0),
        };
        var weeklyInner = new StackPanel { Spacing = 14 };

        var weeklyTitleRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var weeklyTitleStack = new StackPanel { Spacing = 2 };
        weeklyTitleStack.Children.Add(Txt("Weekly Progress", 16, FontWeight.Bold, Colors.White));
        weeklyTitleStack.Children.Add(Txt("You're on a 5-day streak!", 12, FontWeight.Normal, Colors.White, 0.8));
        weeklyTitleRow.Children.Add(weeklyTitleStack);

        var trendBadge = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Child = SvgIcon(
                "M16 6l2.29 2.29-4.88 4.88-4-4L2 16.59 3.41 18l6-6 4 4 6.3-6.29L22 12V6z",
                20, Primary),
        };
        Grid.SetColumn(trendBadge, 1);
        weeklyTitleRow.Children.Add(trendBadge);
        weeklyInner.Children.Add(weeklyTitleRow);

        string[] dayLabels = { "M", "T", "W", "T", "F", "S", "S" };
        var dayGrid = new UniformGrid { Rows = 1 };
        for (int i = 0; i < 7; i++)
        {
            bool isCurrent = i == 4;
            bool isPast = i < 4;

            Border innerCircle;
            if (isCurrent)
            {
                innerCircle = new Border
                {
                    Width = 38,
                    Height = 38,
                    CornerRadius = new CornerRadius(19),
                    Background = Brushes.White,
                    Child = SvgIcon("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z", 18, Primary),
                };
            }
            else if (isPast)
            {
                innerCircle = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                    Child = SvgIcon("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z", 13, Colors.White),
                };
            }
            else
            {
                innerCircle = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                };
            }

            var circleWrap = new Border
            {
                Width = 40, Height = 40, HorizontalAlignment = HorizontalAlignment.Center, Child = innerCircle,
            };
            innerCircle.HorizontalAlignment = HorizontalAlignment.Center;
            innerCircle.VerticalAlignment = VerticalAlignment.Center;

            var dayCol = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
            dayCol.Children.Add(circleWrap);
            dayCol.Children.Add(Txt(dayLabels[i], 10, FontWeight.Medium, Colors.White, 0.75,
                align: TextAlignment.Center));
            dayGrid.Children.Add(dayCol);
        }

        weeklyInner.Children.Add(dayGrid);
        weeklyCard.Child = weeklyInner;
        root.Children.Add(weeklyCard);
        root.Children.Add(new Border { Height = 12 });

        var symptomCard = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0),
            BoxShadow = BoxShadows.Parse("0 1 4 0 #0000000a"),
        };
        var sInner = new StackPanel { Spacing = 12 };
        var sTopRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center
        };
        sTopRow.Children.Add(new Border
        {
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.Parse("#fff7ed")),
            Child = SvgIcon(
                "M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm3.5-9c.83 0 1.5-.67 1.5-1.5S16.33 8 15.5 8 14 8.67 14 9.5s.67 1.5 1.5 1.5zm-7 0c.83 0 1.5-.67 1.5-1.5S9.33 8 8.5 8 7 8.67 7 9.5 7.67 11 8.5 11zm3.5 6.5c2.33 0 4.31-1.46 5.11-3.5H6.89c.8 2.04 2.78 3.5 5.11 3.5z",
                24, Color.Parse("#f97316")),
        });
        var sTextStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        sTextStack.Children.Add(Txt("How are you feeling?", 15, FontWeight.Bold, TextDark));
        sTextStack.Children.Add(Txt("Track your symptoms daily.", 12, FontWeight.Normal, TextMuted));
        sTopRow.Children.Add(sTextStack);
        sInner.Children.Add(sTopRow);

        var logBtn = StyledButton("Log Symptoms", new SolidColorBrush(Primary), Brushes.White, 44,
            new CornerRadius(10));
        logBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        sInner.Children.Add(logBtn);
        symptomCard.Child = sInner;
        root.Children.Add(symptomCard);

        root.Children.Add(new Border { Height = 16 });
        var schedHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(16, 0, 16, 8),
        };
        schedHeader.Children.Add(Txt("Today's Schedule", 16, FontWeight.Bold, TextDark));
        var seeAllTxt = new TextBlock
        {
            Text = "See All",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Primary),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        Grid.SetColumn(seeAllTxt, 1);
        schedHeader.Children.Add(seeAllTxt);
        root.Children.Add(schedHeader);

        root.Children.Add(BuildScheduleItem(
            bulletColor: WarningAmber,
            iconData:
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 3c1.93 0 3.5 1.57 3.5 3.5S13.93 13 12 13s-3.5-1.57-3.5-3.5S10.07 6 12 6zm7 13H5v-.23c0-.62.28-1.2.76-1.58C7.47 15.82 9.64 15 12 15s4.53.82 6.24 2.19c.48.38.76.97.76 1.58V19z",
            iconBg: Color.Parse("#fef3c7"),
            iconFg: WarningAmber,
            title: "Tamoxifen (20mg)",
            time: "09:00 AM",
            subtitle: "Take with food",
            actionLabel: "Mark as Done",
            isCheck: true));

        root.Children.Add(new Border { Height = 8 });

        root.Children.Add(BuildScheduleItem(
            bulletColor: Primary,
            iconData:
            "M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z",
            iconBg: PrimaryLight,
            iconFg: Primary,
            title: "Dr. Emily Chen",
            time: "02:30 PM",
            subtitle: "Oncologist \u2022 Video Consultation",
            actionLabel: "Join Video Call",
            isCheck: false));

        root.Children.Add(new Border { Height = 12 });
        root.Children.Add(new TextBlock
        {
            Text = "No more events for today. Rest well.",
            FontSize = 12,
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(TextMuted),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 0, 16, 0),
        });
        root.Children.Add(new Border { Height = 24 });

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    Border BuildScheduleItem(Color bulletColor, string iconData, Color iconBg, Color iconFg,
        string title, string time, string subtitle, string actionLabel, bool isCheck)
    {
        var card = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0),
            BoxShadow = BoxShadows.Parse("0 1 4 0 #0000000a"),
        };

        var outerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("8,*") };
        outerRow.Children.Add(new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(bulletColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 0, 0),
        });

        var rightStack = new StackPanel { Spacing = 8, Margin = new Thickness(10, 0, 0, 0) };
        Grid.SetColumn(rightStack, 1);

        var topRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        topRow.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(iconBg),
            VerticalAlignment = VerticalAlignment.Center,
            Child = SvgIcon(iconData, 18, iconFg),
        });

        var titleTxt = Txt(title, 14, FontWeight.SemiBold, TextDark);
        titleTxt.VerticalAlignment = VerticalAlignment.Center;
        titleTxt.Margin = new Thickness(10, 0, 6, 0);
        Grid.SetColumn(titleTxt, 1);
        topRow.Children.Add(titleTxt);

        var timeBadge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#f3f4f6")),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(8, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = Txt(time, 10, FontWeight.Medium, TextMuted),
        };
        Grid.SetColumn(timeBadge, 2);
        topRow.Children.Add(timeBadge);
        rightStack.Children.Add(topRow);
        rightStack.Children.Add(Txt(subtitle, 12, FontWeight.Normal, TextMuted));

        Button actionBtn;
        if (isCheck)
        {
            actionBtn = StyledButton(actionLabel,
                new SolidColorBrush(Color.Parse("#f0fdf4")),
                new SolidColorBrush(SuccessGreen),
                36, new CornerRadius(8),
                border: new SolidColorBrush(Color.Parse("#bbf7d0")),
                borderThick: new Thickness(1));
        }
        else
        {
            actionBtn = StyledButton(actionLabel, new SolidColorBrush(Primary), Brushes.White, 36, new CornerRadius(8));
        }

        actionBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        rightStack.Children.Add(actionBtn);

        outerRow.Children.Add(rightStack);
        card.Child = outerRow;
        return card;
    }
}
