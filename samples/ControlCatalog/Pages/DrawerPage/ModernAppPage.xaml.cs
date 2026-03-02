
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages;

public partial class ModernAppPage : UserControl
{
    // Palette
    static readonly Color Primary   = Color.Parse("#0dccf2");
    static readonly Color BgLight   = Color.Parse("#f5f8f8");
    static readonly Color Dark      = Color.Parse("#101f22");
    static readonly Color Card      = Colors.White;
    static readonly Color Muted     = Color.Parse("#8a9ba3");
    static readonly Color ModBorder = Color.Parse("#dde6e9");
    static readonly Color Text      = Color.Parse("#0d1f22");

    static IBrush PrimaryBrush   => new SolidColorBrush(Primary);
    static IBrush BgBrush        => new SolidColorBrush(BgLight);
    static IBrush DarkBrush      => new SolidColorBrush(Dark);
    static IBrush CardBrush      => new SolidColorBrush(Card);
    static IBrush MutedBrush     => new SolidColorBrush(Muted);
    static IBrush BorderFgBrush  => new SolidColorBrush(ModBorder);
    static IBrush TextBrush      => new SolidColorBrush(Text);
    static IBrush WhiteBrush     => new SolidColorBrush(Colors.White);
    record Destination(string Name, string Location, string Asset, bool Favorited = false);
    record Experience(string Name, string Price, string Desc, double Rating, int Reviews, string Asset);
    record TripItem(string Name, string Date, string Location, string Asset);
    record StoryItem(string Name, string Asset);

    static readonly Destination[] Destinations =
    [
        new("Mount Peak Adventure",  "Swiss Alps",         "dest_alps.jpg",   true),
        new("The Whispering Woods",  "Pacific Northwest",  "dest_forest.jpg", false),
        new("Nordic Cabin Escape",   "Norway Highlands",   "dest_norway.jpg", false),
    ];

    static readonly Experience[] Experiences =
    [
        new("Tokyo Night Life",    "$120",
            "Experience the neon lights and hidden bars of Shinjuku with a local guide.",
            4.9, 1200, "exp_tokyo.jpg"),
        new("Angkor Wat Sunrise",  "$85",
            "Witness the breathtaking sunrise over the world's largest religious monument.",
            4.8, 850, "exp_angkor.jpg"),
    ];

    static readonly TripItem[] Trips =
    [
        new("Tokyo Night Life",   "Mar 2025",  "Tokyo, JP",     "exp_tokyo.jpg"),
        new("Angkor Wat Sunrise", "Jan 2025",  "Siem Reap, KH", "exp_angkor.jpg"),
        new("Mount Peak Hike",    "Dec 2024",  "Swiss Alps, CH","dest_alps.jpg"),
        new("Forest Trek",        "Oct 2024",  "Oregon, US",    "dest_forest.jpg"),
    ];

    static readonly StoryItem[] Stories =
    [
        new("Sarah",  "story1.jpg"),
        new("Mike",   "story2.jpg"),
        new("Elena",  "story3.jpg"),
        new("You",    "avatar.jpg"),
    ];

    static readonly string[] GalleryAssets =
    [
        "gallery_city.jpg", "gallery_alpine.jpg",
        "gallery_tropical.jpg", "gallery_bay.jpg",
        "gallery_paris.jpg", "gallery_venice.jpg",
    ];
    DrawerPage?     _drawerPage;
    NavigationPage? _navPage;
    ScrollViewer?   _infoPanel;
    TextBlock?      _pageTitle;
    Button?         _selectedNavBtn;
    bool            _initialized;

    public ModernAppPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel  = this.FindControl<ScrollViewer>("InfoPanel");
        _drawerPage = this.FindControl<DrawerPage>("DrawerPageControl");
        _navPage    = this.FindControl<NavigationPage>("NavPage");
        _pageTitle  = this.FindControl<TextBlock>("PageTitle");

        UpdateInfoPanelVisibility();

        if (_initialized) return;
        _initialized = true;

        if (_navPage == null) return;
        NavigateToDiscover();
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

    // Highlight the active nav item with a light cyan tint; reset the previous one.
    void SelectNavButton(Button btn)
    {
        if (_selectedNavBtn != null)
            _selectedNavBtn.Background = Brushes.Transparent;
        _selectedNavBtn = btn;
        btn.Background = new SolidColorBrush(Color.Parse("#1A0dccf2")); // 10 % cyan tint
    }

    void Navigate(ContentPage page)
    {
        if (_navPage == null) return;
        NavigationPage.SetHasBackButton(page, false);
        NavigationPage.SetHasNavigationBar(page, false);
        _ = _navPage.PopToRootAsync();
        _navPage.Push(page);
    }

    void NavigateToDiscover()
    {
        if (_pageTitle != null) _pageTitle.Text = "Discover";
        SelectNavButton(this.FindControl<Button>("BtnDiscover")!);
        Navigate(new ContentPage { Content = BuildDiscoverContent() });
    }

    void NavigateToMyTrips()
    {
        if (_pageTitle != null) _pageTitle.Text = "My Trips";
        SelectNavButton(this.FindControl<Button>("BtnMyTrips")!);
        Navigate(new ContentPage { Content = BuildMyTripsContent() });
    }

    void NavigateToProfile()
    {
        if (_pageTitle != null) _pageTitle.Text = "Profile";
        SelectNavButton(this.FindControl<Button>("BtnProfile")!);
        Navigate(new ContentPage { Content = BuildProfileContent() });
    }

    void NavigateToSettings()
    {
        if (_pageTitle != null) _pageTitle.Text = "Settings";
        SelectNavButton(this.FindControl<Button>("BtnSettings")!);
        Navigate(new ContentPage { Content = BuildSettingsContent() });
    }

    // DISCOVER

    Control BuildDiscoverContent()
    {
        var stack = new StackPanel { Spacing = 0, Background = BgBrush };
        stack.Children.Add(new Border
        {
            Background = WhiteBrush,
            Padding = new Thickness(16, 8, 16, 14),
            Child = new Border
            {
                Background = BgBrush,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 10,
                    Children =
                    {
                        new PathIcon { Width = 16, Height = 16, Foreground = MutedBrush,
                            Data = Geometry.Parse("M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z") },
                        new TextBlock { Text = "Search for destinations",
                                        Foreground = MutedBrush, FontSize = 14,
                                        VerticalAlignment = VerticalAlignment.Center }
                    }
                }
            }
        });
        stack.Children.Add(new Border
        {
            Background = WhiteBrush,
            Padding = new Thickness(16, 0, 16, 4),
            Child = MakeSectionHeader("Featured Stories", "View all")
        });
        stack.Children.Add(new Border
        {
            Background = WhiteBrush,
            Padding = new Thickness(0, 0, 0, 16),
            Child = BuildStoriesRow()
        });
        stack.Children.Add(new Border
        {
            Background = WhiteBrush,
            Padding = new Thickness(16, 0, 16, 16),
            Child = BuildFilterTabs()
        });
        stack.Children.Add(new Border
        {
            Padding = new Thickness(16, 16, 16, 8),
            Child = new TextBlock { Text = "Featured Destinations",
                                    FontSize = 16, FontWeight = FontWeight.Bold,
                                    Foreground = TextBrush }
        });

        foreach (var d in Destinations)
            stack.Children.Add(new Border
            {
                Padding = new Thickness(16, 0, 16, 12),
                Child = BuildDestinationCard(d)
            });
        stack.Children.Add(new Border
        {
            Padding = new Thickness(16, 8, 16, 8),
            Child = MakeSectionHeader("Popular Experiences", "View all")
        });
        foreach (var exp in Experiences)
            stack.Children.Add(new Border
            {
                Padding = new Thickness(16, 0, 16, 12),
                Child = BuildExperienceCard(exp)
            });

        stack.Children.Add(new Border { Height = 24 });

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = stack
        };
    }

    Control MakeSectionHeader(string title, string? action = null)
    {
        var dp = new DockPanel();
        if (action != null)
            dp.Children.Add(new TextBlock
            {
                Text = action, FontSize = 13, Foreground = PrimaryBrush,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                [DockPanel.DockProperty] = Dock.Right
            });
        dp.Children.Add(new TextBlock
        {
            Text = title, FontSize = 15, FontWeight = FontWeight.Bold,
            Foreground = TextBrush, VerticalAlignment = VerticalAlignment.Center
        });
        return dp;
    }

    Control BuildStoriesRow()
    {
        var items = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Margin = new Thickness(16, 0)
        };

        foreach (var s in Stories)
        {
            var img = Img(s.Asset);
            items.Children.Add(new StackPanel
            {
                Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    CircleAvatar(img, 62, 2.5),
                    new TextBlock { Text = s.Name, FontSize = 11,
                                    TextAlignment = TextAlignment.Center,
                                    Foreground = TextBrush, MaxWidth = 62,
                                    TextTrimming = TextTrimming.CharacterEllipsis }
                }
            });
        }
        items.Children.Add(new StackPanel
        {
            Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new Border
                {
                    Width = 62, Height = 62, CornerRadius = new CornerRadius(999),
                    BorderBrush = PrimaryBrush, BorderThickness = new Thickness(2.5),
                    Child = new TextBlock { Text = "+", FontSize = 28, Foreground = PrimaryBrush,
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            VerticalAlignment = VerticalAlignment.Center }
                },
                new TextBlock { Text = "You", FontSize = 11, TextAlignment = TextAlignment.Center,
                                Foreground = TextBrush }
            }
        });

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility   = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = items
        };
    }

    Control BuildFilterTabs()
    {
        string[] tabs = ["Trending", "Popular", "Near Me"];
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        bool first = true;
        foreach (var t in tabs)
        {
            bool active = first;
            row.Children.Add(new Border
            {
                Background      = active ? PrimaryBrush : Brushes.Transparent,
                CornerRadius    = new CornerRadius(999),
                BorderBrush     = active ? PrimaryBrush : BorderFgBrush,
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(18, 8),
                Child = new TextBlock
                {
                    Text       = t,
                    FontSize   = 13,
                    FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = active ? WhiteBrush : MutedBrush
                }
            });
            first = false;
        }
        return row;
    }

    // Full-width stacked destination card
    Control BuildDestinationCard(Destination d)
    {
        var img   = Img(d.Asset);
        var heart = new Border
        {
            Width = 32, Height = 32, CornerRadius = new CornerRadius(999),
            Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Top,
            Margin = new Thickness(0, 10, 10, 0),
            Child = new PathIcon
            {
                Width = 16, Height = 16,
                Foreground = d.Favorited
                    ? new SolidColorBrush(Color.Parse("#ff5c8a"))
                    : WhiteBrush,
                Data = Geometry.Parse("M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z")
            }
        };

        var overlay = new Grid();
        overlay.Children.Add(img != null
            ? (Control)new Image { Source = img, Stretch = Stretch.UniformToFill }
            : new Border { Background = MutedBrush });
        overlay.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0,   0, 0, 0), 0.4),
                    new GradientStop(Color.FromArgb(200, 0, 0, 0), 1.0),
                }
            }
        });
        overlay.Children.Add(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(14, 0, 14, 14), Spacing = 3,
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 4,
                    Children =
                    {
                        new PathIcon { Width = 12, Height = 12, Foreground = WhiteBrush,
                            Data = Geometry.Parse("M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z") },
                        new TextBlock { Text = d.Location, Foreground = WhiteBrush,
                                        FontSize = 12, Opacity = 0.9 }
                    }
                },
                new TextBlock { Text = d.Name, Foreground = WhiteBrush,
                                FontSize = 16, FontWeight = FontWeight.Bold }
            }
        });
        overlay.Children.Add(heart);

        return new Border
        {
            Height = 190, CornerRadius = new CornerRadius(12),
            ClipToBounds = true, Background = MutedBrush,
            Child = overlay
        };
    }

    Control BuildExperienceCard(Experience exp)
    {
        var img   = Img(exp.Asset);
        var stars = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        for (int i = 0; i < 5; i++)
            stars.Children.Add(new PathIcon
            {
                Width = 13, Height = 13,
                Foreground = i < (int)Math.Round(exp.Rating)
                    ? new SolidColorBrush(Color.Parse("#ffb300")) : MutedBrush,
                Data = Geometry.Parse("M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z")
            });

        return new Border
        {
            Background = CardBrush, CornerRadius = new CornerRadius(12), ClipToBounds = true,
            Child = new DockPanel
            {
                Children =
                {
                    new Border
                    {
                        Width = 105, Height = 110, Background = MutedBrush, ClipToBounds = true,
                        [DockPanel.DockProperty] = Dock.Left,
                        Child = img != null
                            ? (Control)new Image { Source = img, Stretch = Stretch.UniformToFill }
                            : new Border { Background = MutedBrush }
                    },
                    new StackPanel
                    {
                        Margin = new Thickness(12, 10, 12, 10), Spacing = 4,
                        Children =
                        {
                            new DockPanel
                            {
                                Children =
                                {
                                    new TextBlock { Text = exp.Price, FontSize = 16,
                                                    FontWeight = FontWeight.Bold,
                                                    Foreground = PrimaryBrush,
                                                    [DockPanel.DockProperty] = Dock.Right,
                                                    VerticalAlignment = VerticalAlignment.Top },
                                    new TextBlock { Text = exp.Name, FontSize = 14,
                                                    FontWeight = FontWeight.SemiBold,
                                                    Foreground = TextBrush,
                                                    TextWrapping = TextWrapping.Wrap }
                                }
                            },
                            new TextBlock { Text = exp.Desc, FontSize = 12,
                                            Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap,
                                            MaxLines = 2 },
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal, Spacing = 6,
                                Children =
                                {
                                    stars,
                                    new TextBlock
                                    {
                                        Text = $"{exp.Rating} ({FormatCount(exp.Reviews)})",
                                        FontSize = 12, Foreground = MutedBrush,
                                        VerticalAlignment = VerticalAlignment.Center
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    // MY TRIPS

    Control BuildMyTripsContent()
    {
        var header = new Border
        {
            Background = DarkBrush,
            Padding = new Thickness(16, 20, 16, 20),
            Child = new StackPanel { Spacing = 4, Children =
            {
                new TextBlock { Text = "My Trips", FontSize = 24, FontWeight = FontWeight.Bold,
                                Foreground = WhiteBrush },
                new TextBlock { Text = $"{Trips.Length} adventures · 4 countries",
                                FontSize = 13, Foreground = PrimaryBrush }
            }}
        };

        var grid = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var trip in Trips)
            grid.Children.Add(BuildTripCard(trip));

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = new StackPanel
            {
                Children = { header, new Border { Padding = new Thickness(10, 10, 2, 10), Child = grid } }
            }
        };
    }

    Control BuildTripCard(TripItem trip)
    {
        var img = Img(trip.Asset);
        var overlay = new Grid();
        overlay.Children.Add(img != null
            ? (Control)new Image { Source = img, Stretch = Stretch.UniformToFill }
            : new Border { Background = MutedBrush });
        overlay.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(0, 1,   RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0,   0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(200, 0, 0, 0), 1),
                }
            }
        });
        overlay.Children.Add(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10, 0, 10, 10), Spacing = 1,
            Children =
            {
                new TextBlock { Text = trip.Name, Foreground = WhiteBrush,
                                FontSize = 13, FontWeight = FontWeight.SemiBold,
                                TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"{trip.Location} · {trip.Date}",
                                Foreground = WhiteBrush, FontSize = 10, Opacity = 0.75 }
            }
        });
        return new Border
        {
            Width = 170, Height = 180, CornerRadius = new CornerRadius(10),
            ClipToBounds = true, Background = MutedBrush,
            Margin = new Thickness(0, 0, 8, 8), Child = overlay
        };
    }

    // PROFILE

    Control BuildProfileContent()
    {
        var avatar = Img("avatar.jpg");
        var profileSection = new Border
        {
            Background = WhiteBrush,
            Padding = new Thickness(20, 24, 20, 20),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    MakeAvatarWithBadge(avatar),
                    new TextBlock { Text = "Alex Johnson", FontSize = 22,
                                    FontWeight = FontWeight.Bold, Foreground = TextBrush,
                                    HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock { Text = "Traveler & Photographer",
                                    FontSize = 14, Foreground = PrimaryBrush,
                                    FontWeight = FontWeight.SemiBold,
                                    HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock
                    {
                        Text = "Exploring the world one lens at a time.\nBased in Vancouver, wandering everywhere.",
                        FontSize = 12, TextAlignment = TextAlignment.Center,
                        Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap, MaxWidth = 280
                    },
                }
            }
        };
        var statsRow1 = new Grid { ColumnDefinitions = new ColumnDefinitions("*, 10, *") };
        var destCard   = StatCard("42",   "explore",       "DESTINATIONS");
        var photosCard = StatCard("1.2k", "photo_library", "PHOTOS");
        Grid.SetColumn(destCard,   0);
        Grid.SetColumn(photosCard, 2);
        statsRow1.Children.Add(destCard);
        statsRow1.Children.Add(photosCard);

        var statsSection = new Border
        {
            Background = BgBrush,
            Padding = new Thickness(16, 12, 16, 0),
            Child = new StackPanel { Spacing = 10, Children =
            {
                statsRow1,
                StatCard("85", "star", "REVIEWS"),
            }}
        };
        var gallerySection = new Border
        {
            Background = BgBrush,
            Padding = new Thickness(16, 16, 16, 0),
            Child = new StackPanel { Spacing = 10, Children =
            {
                MakeSectionHeader("Travel Gallery", "View All"),
                BuildGalleryGrid()
            }}
        };
        var addBtn = new Border
        {
            Background = BgBrush,
            Padding = new Thickness(16, 12, 16, 24),
            Child = new Button
            {
                Background = PrimaryBrush,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(0, 14),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 8,
                    Children =
                    {
                        new PathIcon { Width = 18, Height = 18, Foreground = WhiteBrush,
                            Data = Geometry.Parse("M12 2a10 10 0 1 0 0 20A10 10 0 0 0 12 2zm5 11h-4v4h-2v-4H7v-2h4V7h2v4h4v2z") },
                        new TextBlock { Text = "Add New Experience", Foreground = WhiteBrush,
                                        FontSize = 15, FontWeight = FontWeight.SemiBold,
                                        VerticalAlignment = VerticalAlignment.Center }
                    }
                }
            }
        };

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = new StackPanel
            {
                Children = { profileSection, statsSection, gallerySection, addBtn }
            }
        };
    }

    static Control MakeAvatarWithBadge(Bitmap? avatar)
    {
        var circle = CircleAvatar(avatar, 96, 0);

        var badge = new Border
        {
            Width = 28, Height = 28, CornerRadius = new CornerRadius(999),
            Background = PrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Bottom,
            Child = new PathIcon { Width = 14, Height = 14, Foreground = new SolidColorBrush(Colors.White),
                Data = Geometry.Parse("M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4zm7-12H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zm-7 14a5 5 0 1 1 0-10 5 5 0 0 1 0 10zm5-12.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3z") }
        };

        return new Grid
        {
            Width = 96, Height = 96,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { circle, badge }
        };
    }

    static Control StatCard(string value, string iconHint, string label)
    {
        var iconPath = iconHint switch
        {
            "explore"       => "M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z",
            "photo_library" => "M22 16V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2zM11 12l2.03 2.71L16 11l4 5H8l3-4zM2 6v14a2 2 0 0 0 2 2h14v-2H4V6H2z",
            "star"          => "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z",
            _               => "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20z"
        };

        var iconColor = iconHint == "star"
            ? new SolidColorBrush(Color.Parse("#ffb300"))
            : new SolidColorBrush(Color.Parse("#0dccf2"));

        return new Border
        {
            Background = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center, Spacing = 4,
                Children =
                {
                    new TextBlock { Text = value, FontSize = 22, FontWeight = FontWeight.Bold,
                                    Foreground = new SolidColorBrush(Color.Parse("#0d1f22")),
                                    HorizontalAlignment = HorizontalAlignment.Center },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4,
                                     HorizontalAlignment = HorizontalAlignment.Center,
                                     Children =
                                     {
                                         new PathIcon { Width = 14, Height = 14, Foreground = iconColor,
                                                        Data = Geometry.Parse(iconPath) },
                                         new TextBlock { Text = label, FontSize = 11,
                                                         FontWeight = FontWeight.SemiBold,
                                                         Foreground = new SolidColorBrush(Color.Parse("#8a9ba3")),
                                                         VerticalAlignment = VerticalAlignment.Center }
                                     }}
                }
            }
        };
    }

    Control BuildGalleryGrid()
    {
        // 2-column uniform grid
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, 8, *"),
            RowDefinitions    = new RowDefinitions("120, 8, 120, 8, 120")
        };

        for (int i = 0; i < GalleryAssets.Length; i++)
        {
            var bmp = Img(GalleryAssets[i]);
            var cell = new Border
            {
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Background = MutedBrush,
                Child = bmp != null
                    ? (Control)new Image { Source = bmp, Stretch = Stretch.UniformToFill }
                    : new Border { Background = MutedBrush }
            };
            int col = (i % 2) * 2;   // 0 or 2 (skip the 8px gap column)
            int row = (i / 2) * 2;   // 0, 2, or 4 (skip the 8px gap rows)
            Grid.SetColumn(cell, col);
            Grid.SetRow(cell, row);
            grid.Children.Add(cell);
        }
        return grid;
    }

    // SETTINGS

    Control BuildSettingsContent()
    {
        var avatar = Img("avatar.jpg");

        var profileRow = new Border
        {
            Background = CardBrush, Padding = new Thickness(16),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 14,
                Children =
                {
                    new Border
                    {
                        Width = 52, Height = 52, CornerRadius = new CornerRadius(999),
                        ClipToBounds = true, Background = MutedBrush,
                        Child = avatar != null
                            ? (Control)new Image { Source = avatar, Stretch = Stretch.UniformToFill }
                            : new Border { Background = MutedBrush }
                    },
                    new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2, Children =
                    {
                        new TextBlock { Text = "Alex Johnson", FontSize = 15,
                                        FontWeight = FontWeight.SemiBold, Foreground = TextBrush },
                        new TextBlock { Text = "alex.j@example.com", FontSize = 12, Foreground = MutedBrush }
                    }}
                }
            }
        };

        Control Section(string title, (string Label, string? Value, bool Toggle)[] items)
        {
            var s = new StackPanel { Spacing = 0 };
            s.Children.Add(new Border
            {
                Padding = new Thickness(16, 16, 16, 6),
                Child = new TextBlock { Text = title.ToUpperInvariant(), FontSize = 11,
                                        FontWeight = FontWeight.SemiBold, Foreground = MutedBrush }
            });
            foreach (var (lbl, val, toggle) in items)
                s.Children.Add(MakeSettingsRow(lbl, val, toggle));
            return s;
        }

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = new StackPanel { Background = BgBrush, Children =
            {
                profileRow,
                new Border { Height = 8 },
                Section("Account",
                [
                    ("Notifications", null, false),
                    ("Privacy",       null, false),
                    ("Security",      null, false),
                ]),
                new Border { Height = 8 },
                Section("Preferences",
                [
                    ("Dark Mode", null,      true),
                    ("Language",  "English", false),
                ]),
                new Border { Height = 8 },
                Section("Support",
                [
                    ("About",       null, false),
                    ("Help Center", "↗",  false),
                    ("Logout",      null, false),
                ]),
                new Border
                {
                    Padding = new Thickness(16, 16),
                    Child = new TextBlock { Text = "Version 2.4.1 (Build 402)",
                                            FontSize = 11, Foreground = MutedBrush,
                                            HorizontalAlignment = HorizontalAlignment.Center }
                }
            }}
        };
    }

    Control MakeSettingsRow(string label, string? value, bool toggle)
    {
        var row = new DockPanel { Background = CardBrush, LastChildFill = true };

        if (toggle)
        {
            row.Children.Add(new CheckBox
            {
                IsChecked = false,
                [DockPanel.DockProperty] = Dock.Right,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        else
        {
            var trail = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
                [DockPanel.DockProperty] = Dock.Right
            };
            if (value != null)
                trail.Children.Add(new TextBlock { Text = value, FontSize = 12,
                                                   Foreground = MutedBrush,
                                                   VerticalAlignment = VerticalAlignment.Center });
            trail.Children.Add(new PathIcon { Width = 16, Height = 16, Foreground = MutedBrush,
                Data = Geometry.Parse("M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z") });
            row.Children.Add(trail);
        }

        row.Children.Add(new Border
        {
            Padding = new Thickness(16, 14),
            Child = new TextBlock { Text = label, FontSize = 14, Foreground = TextBrush }
        });

        return new Border
        {
            BorderBrush = BorderFgBrush, BorderThickness = new Thickness(0, 0, 0, 1),
            Child = row
        };
    }

    static Bitmap? Img(string filename)
    {
        try { return new Bitmap(AssetLoader.Open(new Uri($"avares://ControlCatalog/Assets/ModernApp/{filename}"))); }
        catch { return null; }
    }

    static string FormatCount(int n) => n >= 1000 ? $"{n / 1000.0:0.#}k" : n.ToString();

    /// <summary>
    /// Circular avatar: inner image clipped to circle, outer ring drawn on top
    /// as a separate overlay so the border ring never interferes with the clip rect.
    /// </summary>
    static Control CircleAvatar(Bitmap? img, double size, double ringThickness)
    {
        // Image clipped to the INNER circle (size minus the ring on each side)
        double innerSize = ringThickness > 0 ? size - ringThickness * 2 : size;
        var clip = new Border
        {
            Width  = innerSize,
            Height = innerSize,
            CornerRadius = new CornerRadius(999),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            Child = img != null
                ? (Control)new Image { Source = img, Stretch = Stretch.UniformToFill }
                : new Border { Background = MutedBrush }
        };

        if (ringThickness <= 0) return clip;

        // Ring drawn on top; Background=Transparent lets the clipped image show through
        var ring = new Border
        {
            Width  = size,
            Height = size,
            CornerRadius    = new CornerRadius(999),
            BorderBrush     = PrimaryBrush,
            BorderThickness = new Thickness(ringThickness),
            Background      = Brushes.Transparent
        };

        var grid = new Grid { Width = size, Height = size };
        grid.Children.Add(clip);
        grid.Children.Add(ring);
        return grid;
    }
}
