using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    static readonly Color Primary    = Color.Parse("#137fec");
    static readonly Color BgLight    = Color.Parse("#f6f7f8");
    static readonly Color TextDark   = Color.Parse("#111827");
    static readonly Color TextMuted  = Color.Parse("#64748b");
    static readonly Color CardBg     = Colors.White;

    // DomeH: how many pixels the dome tip dips below the flat header area
    const double DomeH = 32.0;
    const double HomeHeaderFlatH    = 130.0;
    const double ProfileHeaderFlatH = 110.0;

    const double AvatarHomeSize    = 72.0;
    const double AvatarProfileSize = 88.0;

    NavigationPage? _navPage;
    ScrollViewer? _infoPanel;

    public NavigationPageCurvedHeaderPage() => InitializeComponent();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoPanelVisibility();

        _navPage = this.FindControl<NavigationPage>("NavPage");
        if (_navPage == null) return;
        _navPage.Push(BuildHomePage());
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
        var bgPath = new Path { Fill = new SolidColorBrush(CardBg) };

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

        // Avatar ring centered at dome tip
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

        // Scroll content: spacer pushes items below the header + avatar overlap
        double spacerH = HomeHeaderFlatH + DomeH + AvatarHomeSize / 2 + 16;

        var shopNowBtn = new Button
        {
            Content = "Shop Now →",
            Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Height = 40,
            Padding = new Thickness(24, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        shopNowBtn.Click += (_, _) => _navPage?.Push(BuildProfilePage());

        var scrollStack = new StackPanel { Spacing = 0 };
        scrollStack.Children.Add(new Border { Height = spacerH });
        scrollStack.Children.Add(BuildFeaturedSection(shopNowBtn));
        scrollStack.Children.Add(BuildRecommendedSection());
        scrollStack.Children.Add(BuildUpdatesSection());
        scrollStack.Children.Add(new Border { Height = 24 });

        var sv = new ScrollViewer { Content = scrollStack };

        // Update dome + avatar whenever the panel width changes
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
        avatarCanvas.PointerReleased += (_, _) => _navPage?.Push(BuildProfilePage());

        headerPanel.Children.Add(bgPath);
        headerPanel.Children.Add(headerContent);
        headerPanel.Children.Add(avatarCanvas);

        var root = new Panel();
        root.Children.Add(sv);
        root.Children.Add(headerPanel);

        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = root,
        };
        NavigationPage.SetHasNavigationBar(page, false);
        return page;
    }

    Control BuildFeaturedSection(Button shopNowButton)
    {
        var featuredCard = new Border
        {
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
            Child = new Panel
            {
                Children =
                {
                    // Background gradient (mimics fashion photo)
                    new Border
                    {
                        Height = 200,
                        Background = ImgBrush("featured.jpg", new SolidColorBrush(Color.Parse("#0ea5e9"))),
                    },
                    // Scrim for legibility
                    new Border
                    {
                        Background = new LinearGradientBrush
                        {
                            StartPoint = new RelativePoint(0, 0.3, RelativeUnit.Relative),
                            EndPoint   = new RelativePoint(0, 1,   RelativeUnit.Relative),
                            GradientStops =
                            {
                                new GradientStop(Color.FromArgb(0,   0, 0, 0), 0),
                                new GradientStop(Color.FromArgb(160, 0, 0, 0), 1),
                            }
                        }
                    },
                    new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(16, 0, 16, 16),
                        Spacing = 4,
                        Children =
                        {
                            new Border
                            {
                                CornerRadius = new CornerRadius(999),
                                Background = new SolidColorBrush(Primary),
                                Padding = new Thickness(10, 3),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Child = new TextBlock
                                {
                                    Text = "NEW",
                                    FontSize = 10,
                                    FontWeight = FontWeight.SemiBold,
                                    Foreground = Brushes.White,
                                }
                            },
                            new TextBlock
                            {
                                Text = "Summer Collection 2025",
                                FontSize = 18,
                                FontWeight = FontWeight.Bold,
                                Foreground = Brushes.White,
                                TextAlignment = TextAlignment.Center,
                            },
                            new TextBlock
                            {
                                Text = "Discover the latest trends in outdoor fashion.",
                                FontSize = 13,
                                Foreground = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
                                Margin = new Thickness(0, 0, 0, 6),
                                TextAlignment = TextAlignment.Center,
                            },
                            shopNowButton,
                        }
                    }
                }
            }
        };

        var header = new Grid { Margin = new Thickness(16, 16, 16, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var titleBlock = new TextBlock
        {
            Text = "Featured",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextDark),
        };
        Grid.SetColumn(titleBlock, 0);

        var viewAll = new TextBlock
        {
            Text = "View All",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Primary),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(viewAll, 1);

        header.Children.Add(titleBlock);
        header.Children.Add(viewAll);

        var section = new StackPanel { Spacing = 0 };
        section.Children.Add(header);
        section.Children.Add(new Border { Margin = new Thickness(16, 0), Child = featuredCard });
        return section;
    }

    Control BuildRecommendedSection()
    {
        var titleArea = new StackPanel
        {
            Spacing = 2,
            Margin  = new Thickness(16, 20, 16, 12),
            Children =
            {
                new TextBlock
                {
                    Text = "Recommended for you",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(TextDark),
                },
                new TextBlock
                {
                    Text = "Curated items based on your style",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(TextMuted),
                },
            }
        };

        string[] names    = { "Modern Living", "Workspace Zen", "Cozy Vibes" };
        string[] cats     = { "Interior Design", "Productivity", "Home Decor" };
        string[] prices   = { "$120.00", "$85.50", "$45.00" };
        string[] imgFiles = { "product1.jpg", "product2.jpg", "product3.jpg" };
        Color[]  fallbacks =
        {
            Color.Parse("#137fec"),
            Color.Parse("#10b981"),
            Color.Parse("#f59e0b"),
        };

        var cardsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing     = 12,
            Margin      = new Thickness(16, 0, 16, 0),
        };

        for (int i = 0; i < names.Length; i++)
        {
            int idx = i;
            var cardBtn = new Button
            {
                Padding    = new Thickness(0),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(12),
            };
            cardBtn.Click += (_, _) => _navPage?.Push(BuildProfilePage());

            cardBtn.Content = new Border
            {
                Width        = 148,
                CornerRadius = new CornerRadius(12),
                Background   = new SolidColorBrush(CardBg),
                ClipToBounds = true,
                Child = new StackPanel
                {
                    Spacing = 0,
                    Children =
                    {
                        new Border
                        {
                            Height = 115,
                            Background = ImgBrush(imgFiles[idx], new SolidColorBrush(fallbacks[idx])),
                        },
                        new StackPanel
                        {
                            Margin  = new Thickness(10, 8, 10, 12),
                            Spacing = 2,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text       = names[idx],
                                    FontSize   = 13,
                                    FontWeight = FontWeight.SemiBold,
                                    Foreground = new SolidColorBrush(TextDark),
                                },
                                new TextBlock
                                {
                                    Text       = cats[idx],
                                    FontSize   = 11,
                                    Foreground = new SolidColorBrush(TextMuted),
                                },
                                new TextBlock
                                {
                                    Text       = prices[idx],
                                    FontSize   = 13,
                                    FontWeight = FontWeight.Bold,
                                    Foreground = new SolidColorBrush(Primary),
                                    Margin     = new Thickness(0, 4, 0, 0),
                                },
                            }
                        },
                    }
                }
            };
            cardsPanel.Children.Add(cardBtn);
        }

        var sv = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Disabled,
            Content = cardsPanel,
            Padding = new Thickness(0, 0, 0, 8),
        };

        var section = new StackPanel { Spacing = 0 };
        section.Children.Add(titleArea);
        section.Children.Add(sv);
        return section;
    }

    Control BuildUpdatesSection()
    {
        (string title, string subtitle, string tag, Color dot, string img)[] items =
        {
            ("Order #2458 Shipped", "Your package is on its way.",      "In Transit", Color.Parse("#22c55e"), "update1.jpg"),
            ("Price Drop Alert",    "The chair you liked is 20% off.",  "Just Now",   Primary,               "update2.jpg"),
            ("New Store Opening",   "Visit our new downtown location.", "Events",     Color.Parse("#93c5fd"), "update3.jpg"),
        };

        var list = new StackPanel { Spacing = 10, Margin = new Thickness(16, 0, 16, 0) };

        foreach (var (title, subtitle, tag, dot, img) in items)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var icon = new Border
            {
                Width        = 52,
                Height       = 52,
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Background   = ImgBrush(img, new SolidColorBrush(Color.FromArgb(40, dot.R, dot.G, dot.B))),
                Margin       = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(icon, 0);

            var tagRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing     = 5,
                Margin      = new Thickness(0, 4, 0, 0),
                Children    =
                {
                    new Ellipse
                    {
                        Width  = 7,
                        Height = 7,
                        Fill   = new SolidColorBrush(dot),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text       = tag,
                        FontSize   = 11,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(TextMuted),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                }
            };

            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 1,
                Children =
                {
                    new TextBlock
                    {
                        Text       = title,
                        FontSize   = 14,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(TextDark),
                    },
                    new TextBlock
                    {
                        Text       = subtitle,
                        FontSize   = 12,
                        Foreground = new SolidColorBrush(TextMuted),
                    },
                    tagRow,
                }
            };
            Grid.SetColumn(textStack, 1);

            var chevron = new TextBlock
            {
                Text = "›",
                FontSize = 22,
                Foreground = new SolidColorBrush(TextMuted),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(chevron, 2);

            g.Children.Add(icon);
            g.Children.Add(textStack);
            g.Children.Add(chevron);

            list.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(12),
                Background   = new SolidColorBrush(CardBg),
                Padding      = new Thickness(14),
                Child        = g,
            });
        }

        var section = new StackPanel { Spacing = 0 };
        section.Children.Add(new TextBlock
        {
            Text       = "Recent Updates",
            FontSize   = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextDark),
            Margin     = new Thickness(16, 20, 16, 12),
        });
        section.Children.Add(list);
        return section;
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

        // Header text lives BELOW the nav-bar overlay (48px top margin)
        var profileContent = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 3,
            Margin  = new Thickness(24, 52, 24, 0),
            Children =
            {
                new TextBlock
                {
                    Text      = "Alex Johnson",
                    FontSize  = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                },
                new TextBlock
                {
                    Text      = "UI/UX Designer · San Francisco",
                    FontSize  = 13,
                    Foreground = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
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

        double domeTipY   = ProfileHeaderFlatH + DomeH;
        double profileSpacerH = domeTipY + AvatarProfileSize / 2 + 16;
        var statsGrid = new Grid { Margin = new Thickness(24, 8, 24, 16) };
        for (int i = 0; i < 3; i++)
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        (string val, string label)[] stats =
        {
            ("128",   "Posts"),
            ("24.5K", "Followers"),
            ("312",   "Following"),
        };
        for (int i = 0; i < stats.Length; i++)
        {
            var cell = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 1,
                Children =
                {
                    new TextBlock
                    {
                        Text       = stats[i].val,
                        FontSize   = 18,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(TextDark),
                        TextAlignment = TextAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text       = stats[i].label,
                        FontSize   = 12,
                        Foreground = new SolidColorBrush(TextMuted),
                        TextAlignment = TextAlignment.Center,
                    },
                }
            };
            Grid.SetColumn(cell, i);
            statsGrid.Children.Add(cell);
        }

        // Divider between stat columns
        var statsContainer = new Border
        {
            BorderBrush     = new SolidColorBrush(Color.Parse("#e5e7eb")),
            BorderThickness = new Thickness(0, 1, 0, 1),
            Padding         = new Thickness(0, 12),
            Child           = statsGrid,
        };
        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing             = 12,
            Margin              = new Thickness(0, 16, 0, 16),
            Children =
            {
                new Button
                {
                    Content  = "Follow",
                    Background = new SolidColorBrush(Primary),
                    Foreground = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Width  = 120,
                    Height = 40,
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment   = VerticalAlignment.Center,
                },
                new Button
                {
                    Content  = "Message",
                    Background = new SolidColorBrush(Color.Parse("#dbeafe")),
                    Foreground = new SolidColorBrush(Primary),
                    CornerRadius = new CornerRadius(8),
                    Width  = 120,
                    Height = 40,
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment   = VerticalAlignment.Center,
                },
            }
        };

        var bio = new TextBlock
        {
            Text = "Crafting beautiful digital experiences. Passionate about design systems, " +
                   "accessibility, and great coffee. Open to collaborations!",
            TextWrapping  = TextWrapping.Wrap,
            Margin        = new Thickness(24, 0, 24, 20),
            FontSize      = 13,
            Foreground    = new SolidColorBrush(TextMuted),
            TextAlignment = TextAlignment.Center,
        };

        var scrollStack = new StackPanel { Spacing = 0 };
        scrollStack.Children.Add(new Border { Height = profileSpacerH });
        scrollStack.Children.Add(statsContainer);
        scrollStack.Children.Add(btnRow);
        scrollStack.Children.Add(bio);
        scrollStack.Children.Add(BuildPostsGrid());
        scrollStack.Children.Add(new Border { Height = 24 });

        var sv = new ScrollViewer { Content = scrollStack };

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
        root.Children.Add(sv);
        root.Children.Add(headerPanel);

        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
            Content    = root,
        };
        NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
        return page;
    }

    Control BuildPostsGrid()
    {
        string[] postImages =
        {
            "featured.jpg", "product1.jpg", "product2.jpg",
            "product3.jpg", "update1.jpg",  "update2.jpg",
        };
        Color[] fallbacks =
        {
            Color.Parse("#dbeafe"), Color.Parse("#fce7f3"), Color.Parse("#d1fae5"),
            Color.Parse("#fef3c7"), Color.Parse("#ede9fe"), Color.Parse("#fee2e2"),
        };

        var grid = new Grid { Margin = new Thickness(14, 0, 14, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                var cell = new Border
                {
                    Margin       = new Thickness(2),
                    Height       = 100,
                    CornerRadius = new CornerRadius(6),
                    ClipToBounds = true,
                    Background   = ImgBrush(postImages[idx], new SolidColorBrush(fallbacks[idx])),
                };
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                grid.Children.Add(cell);
            }
        }

        var section = new StackPanel { Spacing = 0 };
        section.Children.Add(new TextBlock
        {
            Text       = "Posts",
            FontSize   = 16,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextDark),
            Margin     = new Thickness(16, 0, 16, 12),
        });
        section.Children.Add(grid);
        return section;
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
