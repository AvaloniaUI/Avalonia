using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace ControlCatalog.Pages;

public partial class NavigationPageCurvedHeaderPage : UserControl
{
    static readonly Color Primary   = Color.Parse("#137fec");
    static readonly Color BgLight   = Color.Parse("#f6f7f8");
    static readonly Color TextDark  = Color.Parse("#111827");
    static readonly Color TextMuted = Color.Parse("#64748b");

    const double DomeH              = 32.0;
    const double HomeHeaderFlatH    = 130.0;
    const double ProfileHeaderFlatH = 110.0;
    const double AvatarHomeSize     = 72.0;
    const double AvatarProfileSize  = 88.0;

    NavigationPage? _navPage;
    ScrollViewer?   _infoPanel;

    public NavigationPageCurvedHeaderPage()
    {
        InitializeComponent();

        _navPage = this.FindControl<NavigationPage>("NavPage");
        if (_navPage != null)
            _ = _navPage.PushAsync(BuildHomePage());
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

    const string AssetBase = "avares://ControlCatalog/Assets/CurvedHeader/";

    static Bitmap? LoadImg(string name)
    {
        try { return new Bitmap(AssetLoader.Open(new Uri(AssetBase + name))); }
        catch { return null; }
    }

    static IBrush ImgBrush(string name, IBrush fallback)
    {
        var bmp = LoadImg(name);
        return bmp != null ? new ImageBrush(bmp) { Stretch = Stretch.UniformToFill } : fallback;
    }

    ContentPage BuildHomePage()
    {
        var bgPath = new Path { Fill = new SolidColorBrush(Colors.White) };

        var headerContent = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 4,
            Margin = new Thickness(24, 20, 24, 0),
            Children =
            {
                new TextBlock
                {
                    Text = "Welcome back, Alex",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(TextDark),
                    TextAlignment = TextAlignment.Center,
                },
                new TextBlock
                {
                    Text = "Ready to explore?",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(TextMuted),
                    TextAlignment = TextAlignment.Center,
                },
                new TextBox
                {
                    Margin = new Thickness(0, 10, 0, 0),
                    PlaceholderText = "Search for products...",
                    CornerRadius = new CornerRadius(999),
                    Height = 40,
                    Padding = new Thickness(16, 10),
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(Color.Parse("#f1f5f9")),
                    BorderThickness = new Thickness(0),
                    Styles =
                    {
                        new Style(x => x.OfType<TextBox>().Template().OfType<TextBlock>().Name("PART_PlaceholderText"))
                        {
                            Setters =
                            {
                                new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                                new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                            }
                        }
                    }
                },
            }
        };

        var avatar = new Ellipse
        {
            Width  = AvatarHomeSize,
            Height = AvatarHomeSize,
            Fill   = ImgBrush("avatar.jpg", new SolidColorBrush(Color.Parse("#93c5fd"))),
        };
        var avatarRing = new Ellipse
        {
            Width           = AvatarHomeSize + 6,
            Height          = AvatarHomeSize + 6,
            Stroke          = Brushes.White,
            StrokeThickness = 3,
            Fill            = Brushes.Transparent,
        };

        var headerPanel = new Panel { VerticalAlignment = VerticalAlignment.Top };

        var homeScroll = new CurvedHeaderHomeScrollView
        {
            NavigateRequested = async () => { if (_navPage != null) await _navPage.PushAsync(BuildProfilePage()); },
        };

        headerPanel.SizeChanged += (_, args) =>
        {
            double w = args.NewSize.Width;
            if (w <= 1) return;

            bgPath.Data = BuildDomeGeometry(w, HomeHeaderFlatH, DomeH);

            double domeTipY = HomeHeaderFlatH + DomeH;
            Canvas.SetLeft(avatar,     (w - AvatarHomeSize) / 2);
            Canvas.SetTop(avatar,       domeTipY - AvatarHomeSize / 2);
            Canvas.SetLeft(avatarRing, (w - (AvatarHomeSize + 6)) / 2);
            Canvas.SetTop(avatarRing,   domeTipY - (AvatarHomeSize + 6) / 2);
        };

        var avatarCanvas = new Canvas { IsHitTestVisible = true, Cursor = new Cursor(StandardCursorType.Hand) };
        avatarCanvas.Children.Add(avatarRing);
        avatarCanvas.Children.Add(avatar);
        avatarCanvas.PointerReleased += async (_, _) => { if (_navPage != null) await _navPage.PushAsync(BuildProfilePage()); };

        headerPanel.Children.Add(bgPath);
        headerPanel.Children.Add(headerContent);
        headerPanel.Children.Add(avatarCanvas);

        var root = new Panel();
        root.Children.Add(homeScroll);
        root.Children.Add(headerPanel);

        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = root,
        };
        NavigationPage.SetHasNavigationBar(page, false);
        return page;
    }

    ContentPage BuildProfilePage()
    {
        var bgPath = new Path
        {
            Fill = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#137fec"), 0),
                    new GradientStop(Color.Parse("#0a4fa8"), 1),
                }
            }
        };

        var profileContent = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 3,
            Margin  = new Thickness(24, 52, 24, 0),
            Children =
            {
                new TextBlock
                {
                    Text          = "Alex Johnson",
                    FontSize      = 20,
                    FontWeight    = FontWeight.Bold,
                    Foreground    = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                },
                new TextBlock
                {
                    Text          = "UI/UX Designer · San Francisco",
                    FontSize      = 13,
                    Foreground    = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
                    TextAlignment = TextAlignment.Center,
                },
            }
        };

        var avatar = new Ellipse
        {
            Width  = AvatarProfileSize,
            Height = AvatarProfileSize,
            Fill   = ImgBrush("avatar.jpg", new SolidColorBrush(Color.Parse("#60a5fa"))),
        };
        var avatarRing = new Ellipse
        {
            Width           = AvatarProfileSize + 6,
            Height          = AvatarProfileSize + 6,
            Stroke          = Brushes.White,
            StrokeThickness = 3,
            Fill            = Brushes.Transparent,
        };

        var headerPanel = new Panel { VerticalAlignment = VerticalAlignment.Top };

        var profileScroll = new CurvedHeaderProfileScrollView();

        headerPanel.SizeChanged += (_, args) =>
        {
            double w = args.NewSize.Width;
            if (w <= 1) return;

            bgPath.Data = BuildDomeGeometry(w, ProfileHeaderFlatH, DomeH);

            double tipY = ProfileHeaderFlatH + DomeH;
            Canvas.SetLeft(avatar,     (w - AvatarProfileSize) / 2);
            Canvas.SetTop(avatar,       tipY - AvatarProfileSize / 2);
            Canvas.SetLeft(avatarRing, (w - (AvatarProfileSize + 6)) / 2);
            Canvas.SetTop(avatarRing,   tipY - (AvatarProfileSize + 6) / 2);
        };

        var avatarCanvas = new Canvas { IsHitTestVisible = false };
        avatarCanvas.Children.Add(avatarRing);
        avatarCanvas.Children.Add(avatar);

        headerPanel.Children.Add(bgPath);
        headerPanel.Children.Add(profileContent);
        headerPanel.Children.Add(avatarCanvas);

        var root = new Panel();
        root.Children.Add(profileScroll);
        root.Children.Add(headerPanel);

        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = root,
        };
        NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
        return page;
    }

    static StreamGeometry BuildDomeGeometry(double w, double flatH, double domeH)
    {
        var sg = new StreamGeometry();
        using var ctx = sg.Open();
        ctx.BeginFigure(new Point(0, 0), isFilled: true);
        ctx.LineTo(new Point(w, 0));
        ctx.LineTo(new Point(w, flatH));
        ctx.ArcTo(
            new Point(0, flatH),
            new Size(w / 2, domeH),
            rotationAngle: 0,
            isLargeArc: false,
            sweepDirection: SweepDirection.Clockwise);
        ctx.EndFigure(true);
        return sg;
    }
}
