using System;
using System.Collections.Generic;
using Avalonia;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace ControlCatalog.Pages;

public partial class RetroGamingAppPage : UserControl
{

    static readonly Color BgColor      = Color.Parse("#120a1f");
    static readonly Color SurfaceColor = Color.Parse("#2d1b4e");
    static readonly Color PrimaryColor = Color.Parse("#ad2bee");
    static readonly Color CyanColor    = Color.Parse("#00ffff");
    static readonly Color YellowColor  = Color.Parse("#ffff00");
    static readonly Color TextColor    = Color.Parse("#e0d0ff");
    static readonly Color MutedColor   = Color.Parse("#7856a8");
    static readonly Color GreenColor   = Color.Parse("#00cc88");
    static readonly Color OrangeColor  = Color.Parse("#ff9900");
    static readonly Color MagentaColor = Color.Parse("#ff00ff");
    static readonly FontFamily RetroFont = new FontFamily("Courier New, monospace");
    const double GameCardWidth = 145;

    record ContinueGame(string Title, string Genre, string Status, int Progress, Color Accent);
    record GameEntry(string Title, string Genre, Color Accent, bool IsHot = false);

    static readonly ContinueGame[] ContinueGames =
    {
        new("Pixel Quest", "PLATFORMER", "LVL 4 - DUNGEON", 65, CyanColor),
        new("Space Voids", "SHOOTER",    "SECTOR 7",        32, YellowColor),
    };

    static readonly GameEntry[] NewReleases =
    {
        new("Neon Racer",    "Racing",    PrimaryColor, true),
        new("Dungeon Bit",   "Adventure", GreenColor),
        new("Forest Spirit", "Platform",  OrangeColor),
        new("Cyber City",    "Strategy",  CyanColor),
    };

    static readonly GameEntry[] AllGames =
    {
        new("Cyber Ninja 2084", "ACTION RPG",  PrimaryColor, true),
        new("Neon Racer",       "RACING",      PrimaryColor, true),
        new("Dungeon Bit",      "RPG",         GreenColor),
        new("Forest Spirit",    "PUZZLE",      OrangeColor),
        new("Pixel Quest",      "PLATFORMER",  CyanColor),
        new("Space Voids",      "SHOOTER",     YellowColor),
        new("Cyber City",       "STRATEGY",    CyanColor),
        new("Dragon Realm",     "RPG",         Color.Parse("#ff6633")),
    };


    static readonly Dictionary<string, string> GameAssets = new()
    {
        { "Cyber Ninja 2084", "hero.jpg"         },
        { "Pixel Quest",      "pixel_quest.jpg"  },
        { "Neon Racer",       "neon_racer.jpg"   },
        { "Dungeon Bit",      "dungeon_bit.jpg"  },
        { "Forest Spirit",    "forest_spirit.jpg"},
        { "Cyber City",       "cyber_city.jpg"   },
        { "Neon Ninja",       "neon_ninja.jpg"   },
        { "Space Voids",      "space_voids.jpg"  },
    };

    static Bitmap? LoadAsset(string filename)
    {
        try
        {
            var uri = new Uri($"avares://ControlCatalog/Assets/RetroGaming/{filename}");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch { return null; }
    }

    static Image? MakeGameImage(string gameTitle, Stretch stretch = Stretch.UniformToFill)
    {
        if (!GameAssets.TryGetValue(gameTitle, out var filename)) return null;
        var bmp = LoadAsset(filename);
        if (bmp == null) return null;
        return new Image { Source = bmp, Stretch = stretch };
    }


    NavigationPage? _nav;
    bool _initialized;
    ScrollViewer? _infoPanel;


    public RetroGamingAppPage() => InitializeComponent();


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoPanelVisibility();

        if (_initialized) return;
        _initialized = true;

        _nav = this.FindControl<NavigationPage>("RetroNav");
        _nav?.Push(BuildHomePage());
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


    ContentPage BuildHomePage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgColor) };
        page.Header = BuildPixelArcadeLogo();
        NavigationPage.SetTopCommandBar(page, BuildNavBarRight());

        // Content: Panel with TabbedPage + floating Search FAB
        var panel = new Panel();
        panel.Children.Add(BuildHomeTabbedPage());
        panel.Children.Add(BuildSearchFab());

        page.Content = panel;
        return page;
    }


    static Control BuildPixelArcadeLogo()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var iconPanel = new Grid { Width = 36, Height = 30 };
        iconPanel.Children.Add(new Border
        {
            Width = 36, Height = 20, CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.Parse("#cc44dd")),
            VerticalAlignment = VerticalAlignment.Bottom,
        });
        iconPanel.Children.Add(new Border
        {
            Width = 9, Height = 9,
            Background = new SolidColorBrush(SurfaceColor),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Bottom,
            Margin = new Thickness(4, 0, 0, 6),
        });
        iconPanel.Children.Add(new Border
        {
            Width = 9, Height = 9,
            Background = new SolidColorBrush(SurfaceColor),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 4, 6),
        });
        row.Children.Add(iconPanel);
        var textStack = new StackPanel { Spacing = 1 };
        textStack.Children.Add(new TextBlock
        {
            Text = "PIXEL",
            FontFamily = RetroFont, FontSize = 14, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor), LineHeight = 16,
        });
        textStack.Children.Add(new TextBlock
        {
            Text = "ARCADE",
            FontFamily = RetroFont, FontSize = 14, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor), LineHeight = 16,
        });
        row.Children.Add(textStack);
        return row;
    }


    static Control BuildNavBarRight()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        row.Children.Add(new PathIcon
        {
            Width = 16, Height = 16,
            Foreground = new SolidColorBrush(TextColor),
            Data = Geometry.Parse("M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z"),
        });
        var avatarBmp = LoadAsset("hero.jpg");
        var avatar = new Border
        {
            Width = 26, Height = 26,
            CornerRadius = new CornerRadius(0),
            ClipToBounds = true,
            Background = new SolidColorBrush(SurfaceColor),
            BorderBrush = new SolidColorBrush(MutedColor),
            BorderThickness = new Thickness(1),
        };
        avatar.Child = avatarBmp != null
            ? (Control)new Image { Source = avatarBmp, Stretch = Stretch.UniformToFill }
            : new TextBlock
              {
                  Text = "P1", FontFamily = RetroFont, FontSize = 7, FontWeight = FontWeight.Bold,
                  Foreground = new SolidColorBrush(CyanColor),
                  HorizontalAlignment = HorizontalAlignment.Center,
                  VerticalAlignment   = VerticalAlignment.Center,
              };
        row.Children.Add(avatar);
        return row;
    }


    Control BuildSearchFab()
    {
        var fab = new Button
        {
            Width  = 50, Height = 50,
            CornerRadius = new CornerRadius(0),
            Background = new SolidColorBrush(YellowColor),
            Padding = new Thickness(0),
        };
        fab.Classes.Add("retro-fab");
        fab.Content = new PathIcon
        {
            Width = 22, Height = 22,
            Foreground = new SolidColorBrush(BgColor),
            Data = Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
        };
        fab.Click += (_, _) => _ = _nav?.PushModalAsync(BuildSearchModal());

        // Wrapper provides the yellow glow shadow and positions the FAB so its
        // center sits exactly on the tab bar's top dividing line (tabBarHeight=60,
        // FAB half-height=25 → bottom margin = 60 - 25 = 35).
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Bottom,
            Margin    = new Thickness(0, 0, 0, 35),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 10, Spread = 1,
                Color = Color.FromArgb(140, 255, 255, 0),
            }),
            Child = fab,
        };
    }


    ContentPage BuildSearchModal()
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgColor) };

        var root = new StackPanel { Spacing = 16, Margin = new Thickness(16, 24, 16, 16) };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
        header.Children.Add(new TextBlock
        {
            Text = "> SEARCH GAMES",
            FontFamily = RetroFont, FontSize = 18, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor),
            VerticalAlignment = VerticalAlignment.Center,
        });

        var closeBtn = new Button
        {
            Content = new TextBlock
            {
                Text = "X", FontFamily = RetroFont, FontSize = 16, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(CyanColor),
            },
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(CyanColor),
            CornerRadius = new CornerRadius(0),
            Width = 32, Height = 32, Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            [Grid.ColumnProperty] = 1,
        };
        closeBtn.Click += (_, _) => _ = _nav?.PopModalAsync();
        header.Children.Add(closeBtn);
        root.Children.Add(header);
        var searchBox = new TextBox
        {
            PlaceholderText = "Type game name...",
            FontFamily = RetroFont, FontSize = 14,
            Background = new SolidColorBrush(SurfaceColor),
            Foreground = new SolidColorBrush(TextColor),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(PrimaryColor),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(12, 10),
        };
        root.Children.Add(searchBox);
        var catRow = new WrapPanel { Orientation = Orientation.Horizontal };
        var categories = new[] { "ALL", "ACTION", "RPG", "RACING", "PUZZLE", "STRATEGY" };
        foreach (var cat in categories)
        {
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = cat, FontFamily = RetroFont, FontSize = 11,
                    Foreground = new SolidColorBrush(TextColor),
                },
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 0, 6, 6),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(MutedColor),
            };
            btn.Classes.Add("retro-cat-btn");
            catRow.Children.Add(btn);
        }
        root.Children.Add(catRow);
        root.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            Margin = new Thickness(0, 4),
        });
        root.Children.Add(new TextBlock
        {
            Text = "POPULAR SEARCHES",
            FontFamily = RetroFont, FontSize = 12, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(MutedColor),
        });
        var resultsList = new StackPanel { Spacing = 2 };
        foreach (var game in AllGames)
        {
            var row = new Button
            {
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            row.Classes.Add("retro-list-btn");
            row.Tag = game.Title;
            row.Click += async (s, e) =>
            {
                if (s is Button b && b.Tag is string title)
                {
                    await (_nav?.PopModalAsync() ?? Task.CompletedTask);
                    _nav?.Push(BuildDetailPage(title));
                }
            };

            var rowContent = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto") };
            rowContent.Children.Add(new Border
            {
                Width = 3, Height = 28,
                Background = new SolidColorBrush(game.Accent),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
            });
            var textCol = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                [Grid.ColumnProperty] = 1,
            };
            textCol.Children.Add(new TextBlock
            {
                Text = game.Title,
                FontFamily = RetroFont, FontSize = 13, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(TextColor),
            });
            textCol.Children.Add(new TextBlock
            {
                Text = game.Genre,
                FontFamily = RetroFont, FontSize = 10,
                Foreground = new SolidColorBrush(MutedColor),
            });
            rowContent.Children.Add(textCol);
            rowContent.Children.Add(new TextBlock
            {
                Text = ">",
                FontFamily = RetroFont, FontSize = 16,
                Foreground = new SolidColorBrush(MutedColor),
                VerticalAlignment = VerticalAlignment.Center,
                [Grid.ColumnProperty] = 2,
            });

            row.Content = rowContent;
            resultsList.Children.Add(row);
        }

        root.Children.Add(new ScrollViewer
        {
            Content = resultsList,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        });

        page.Content = root;
        return page;
    }


    TabbedPage BuildHomeTabbedPage()
    {
        var tp = new TabbedPage
        {
            Background         = new SolidColorBrush(BgColor),
            BarBackground      = new SolidColorBrush(SurfaceColor),
            SelectedTabBrush   = new SolidColorBrush(PrimaryColor),
            UnselectedTabBrush = new SolidColorBrush(MutedColor),
            TabPlacement       = TabPlacement.Bottom,
        };
        tp.Resources.Add("TabItemHeaderFontSize", 12.0);

        var homeTab = new ContentPage
        {
            Header     = "Home",
            Icon       = "M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z",
            Background = new SolidColorBrush(BgColor),
            Content    = BuildHomeTabContent(),
        };

        var gamesTab = new ContentPage
        {
            Header     = "Games",
            Icon       = "M7.97,16L5,19C4.67,19.3 4.23,19.5 3.75,19.5A1.75,1.75 0 0,1 2,17.75V17.5L3,10.12C3.21,7.81 5.14,6 7.5,6H16.5C18.86,6 20.79,7.81 21,10.12L22,17.5V17.75A1.75,1.75 0 0,1 20.25,19.5C19.77,19.5 19.33,19.3 19,19L16.03,16H7.97M7,9V11H5V13H7V15H9V13H11V11H9V9H7M14.5,12A1.5,1.5 0 0,0 13,13.5A1.5,1.5 0 0,0 14.5,15A1.5,1.5 0 0,0 16,13.5A1.5,1.5 0 0,0 14.5,12M17.5,9A1.5,1.5 0 0,0 16,10.5A1.5,1.5 0 0,0 17.5,12A1.5,1.5 0 0,0 19,10.5A1.5,1.5 0 0,0 17.5,9Z",
            Background = new SolidColorBrush(BgColor),
            Content    = BuildGamesTabContent(),
        };

        var favTab = new ContentPage
        {
            Header     = "Favorites",
            Icon       = "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z",
            Background = new SolidColorBrush(BgColor),
            Content    = BuildFavoritesTabContent(),
        };

        var profileTab = new ContentPage
        {
            Header     = "Profile",
            Icon       = "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
            Background = new SolidColorBrush(BgColor),
            Content    = BuildProfileTabContent(),
        };

        tp.Pages = new ObservableCollection<object?> { homeTab, gamesTab, favTab, profileTab };

        return tp;
    }


    Control BuildHomeTabContent()
    {
        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0 };

        // "> GAME OF THE DAY" header + "NEW!" badge
        stack.Children.Add(BuildSectionRow("> GAME OF THE DAY", showNew: true));
        stack.Children.Add(BuildHeroSection());

        // "CONTINUE PLAYING" header with TV/monitor icon
        const string monitorIcon = "M21,16H3V4H21M21,2H3C1.89,2 1,2.89 1,4V16A2,2 0 0,0 3,18H10V20H8V22H16V20H14V18H21A2,2 0 0,0 23,16V4C23,2.89 22.1,2 21,2Z";
        stack.Children.Add(BuildSectionRow("CONTINUE PLAYING", showNew: false, iconPath: monitorIcon));
        var continueScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, 4),
        };
        var continueRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            Margin = new Thickness(16, 0, 16, 0),
        };
        foreach (var g in ContinueGames)
            continueRow.Children.Add(CreateContinueCard(g));
        continueScroll.Content = continueRow;
        stack.Children.Add(continueScroll);

        // "~NEW RELEASES" header + "VIEW ALL >"
        stack.Children.Add(BuildNewReleasesHeader());
        stack.Children.Add(BuildNewReleasesRow());
        stack.Children.Add(BuildCategoryBar());

        scroll.Content = stack;
        return scroll;
    }


    static Control BuildSectionRow(string label, bool showNew, string? iconPath = null)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 16, 16, 8),
        };

        Control labelControl;
        if (iconPath != null)
        {
            var iconRow = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
            };
            iconRow.Children.Add(new PathIcon
            {
                Width = 13, Height = 13,
                Foreground = new SolidColorBrush(CyanColor),
                Data = Geometry.Parse(iconPath),
            });
            iconRow.Children.Add(new TextBlock
            {
                Text = label,
                FontFamily = RetroFont, FontSize = 11, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(CyanColor),
                VerticalAlignment = VerticalAlignment.Center,
            });
            labelControl = iconRow;
        }
        else
        {
            labelControl = new TextBlock
            {
                Text = label,
                FontFamily = RetroFont, FontSize = 11, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(CyanColor),
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        row.Children.Add(labelControl);

        if (showNew)
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(YellowColor),
                Padding = new Thickness(8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "NEW!",
                    FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(BgColor),
                },
            };
            Grid.SetColumn(badge, 1);
            row.Children.Add(badge);
        }

        return row;
    }


    Control BuildHeroSection()
    {
        var outer = new Border
        {
            Margin = new Thickness(16, 0, 16, 0),
            BorderBrush = new SolidColorBrush(CyanColor),
            BorderThickness = new Thickness(2),
        };

        var card = new StackPanel { Spacing = 0 };

        var imgArea = new Border { Height = 180, ClipToBounds = true };
        var imgGrid = new Grid();

        var heroImg = MakeGameImage("Cyber Ninja 2084");
        if (heroImg != null)
            imgGrid.Children.Add(heroImg);
        else
        {
            var grad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
            };
            grad.GradientStops.Add(new GradientStop(Color.Parse("#3d2060"), 0));
            grad.GradientStops.Add(new GradientStop(BgColor, 1));
            imgGrid.Children.Add(new Border { Background = grad });
        }

        // Bottom-to-top dark gradient so text is readable
        var dimOverlay = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        dimOverlay.GradientStops.Add(new GradientStop(Color.Parse("#20000000"), 0.0));
        dimOverlay.GradientStops.Add(new GradientStop(Color.Parse("#D0000000"), 1.0));
        imgGrid.Children.Add(new Border { Background = dimOverlay });

        // Genre badges overlaid at top-left
        var badgesRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0),
        };
        badgesRow.Children.Add(MakeGenreTag("ACTION", CyanColor, solid: true));
        badgesRow.Children.Add(MakeGenreTag("RPG",    MutedColor, solid: true));
        imgGrid.Children.Add(badgesRow);

        // Title + tagline overlaid at bottom-left
        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10, 0, 10, 10), Spacing = 4,
        };

        // "CYBER " (magenta) + "NINJA" (white) on one line, then "2084 ₿" on next
        var line1 = new WrapPanel { Orientation = Orientation.Horizontal };
        line1.Children.Add(new TextBlock
        {
            Text = "CYBER ",
            FontFamily = RetroFont, FontSize = 20, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(MagentaColor),
        });
        line1.Children.Add(new TextBlock
        {
            Text = "NINJA",
            FontFamily = RetroFont, FontSize = 20, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
        });
        titleStack.Children.Add(line1);

        var line2 = new WrapPanel { Orientation = Orientation.Horizontal };
        line2.Children.Add(new TextBlock
        {
            Text = "2084 ",
            FontFamily = RetroFont, FontSize = 20, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
        });
        line2.Children.Add(new TextBlock
        {
            Text = "₿",
            FontFamily = RetroFont, FontSize = 18, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor),
            VerticalAlignment = VerticalAlignment.Center,
        });
        titleStack.Children.Add(line2);

        titleStack.Children.Add(new TextBlock
        {
            Text = "Hack the mainframe. Slice the AI.",
            FontFamily = RetroFont, FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromArgb(210, 224, 208, 255)),
        });
        imgGrid.Children.Add(titleStack);

        imgArea.Child = imgGrid;
        card.Children.Add(imgArea);

        var playBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0),
            Padding = new Thickness(0, 14),
            CornerRadius = new CornerRadius(0),
            Tag = "Cyber Ninja 2084",
        };
        playBtn.Classes.Add("retro-primary-btn");
        playBtn.Content = new TextBlock
        {
            Text = "▶ INSERT COIN TO PLAY",
            FontFamily = RetroFont, FontSize = 10, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
        };
        playBtn.Click += OnGameClick;
        card.Children.Add(playBtn);

        outer.Child = card;
        return outer;
    }


    static Control BuildNewReleasesHeader()
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 20, 16, 10),
        };

        var lhsStack = new StackPanel { Spacing = 3 };
        lhsStack.Children.Add(new TextBlock
        {
            Text = "~NEW RELEASES",
            FontFamily = RetroFont, FontSize = 12, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextColor),
        });
        lhsStack.Children.Add(new Border
        {
            Height = 1, Background = new SolidColorBrush(PrimaryColor),
        });
        Grid.SetColumn(lhsStack, 0);
        row.Children.Add(lhsStack);

        var viewAll = new TextBlock
        {
            Text = "VIEW ALL >",
            FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(PrimaryColor),
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumn(viewAll, 1);
        row.Children.Add(viewAll);

        return row;
    }


    Control BuildNewReleasesRow()
    {
        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Disabled,
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Margin = new Thickness(16, 0, 16, 0),
        };
        foreach (var g in NewReleases)
            row.Children.Add(CreateNewReleaseCard(g));

        scroll.Content = row;
        return scroll;
    }

    Control CreateNewReleaseCard(GameEntry game)
    {
        var btn = new Button
        {
            Padding = new Thickness(0), Width = 100,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(0),
            Tag = game.Title,
        };
        btn.Classes.Add("retro-list-btn");
        btn.Click += OnGameClick;

        var card = new StackPanel { Spacing = 0 };
        var artBorder = new Border { Height = 115, ClipToBounds = true };
        var artGrid   = new Grid();

        var coverImg = MakeGameImage(game.Title);
        if (coverImg != null)
        {
            artGrid.Children.Add(coverImg);
        }
        else
        {
            var artGrad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
            };
            artGrad.GradientStops.Add(new GradientStop(Color.FromArgb(200, game.Accent.R, game.Accent.G, game.Accent.B), 0));
            artGrad.GradientStops.Add(new GradientStop(SurfaceColor, 1));
            artGrid.Children.Add(new Border { Background = artGrad });
            artGrid.Children.Add(new TextBlock
            {
                Text = game.Title[0].ToString(),
                FontFamily = RetroFont, FontSize = 38, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            });
        }
        artBorder.Child = artGrid;
        card.Children.Add(artBorder);
        var info = new StackPanel { Spacing = 2, Margin = new Thickness(2, 6, 2, 4) };
        info.Children.Add(new TextBlock
        {
            Text = game.Title, FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        info.Children.Add(new TextBlock
        {
            Text = game.Genre, FontFamily = RetroFont, FontSize = 8,
            Foreground = new SolidColorBrush(MutedColor),
        });
        card.Children.Add(info);

        btn.Content = card;
        return btn;
    }


    Control BuildCategoryBar()
    {
        var wrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(16, 14, 16, 24),
        };

        string[] categories = { "ARCADE", "STRATEGY", "RPG", "PUZZLE" };
        bool first = true;
        foreach (var cat in categories)
        {
            var btn = new Button
            {
                Padding = new Thickness(14, 8),
                CornerRadius = new CornerRadius(0),
                Margin = new Thickness(0, 0, 8, 8),
                Content = new TextBlock
                {
                    Text = cat, FontFamily = RetroFont, FontSize = 10, FontWeight = FontWeight.Bold,
                    Foreground = first ? Brushes.White : new SolidColorBrush(TextColor),
                },
            };
            btn.Classes.Add(first ? "retro-cat-selected" : "retro-cat-btn");
            first = false;
            wrap.Children.Add(btn);
        }
        return wrap;
    }


    Control BuildGamesTabContent()
    {
        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0, Margin = new Thickness(0, 8, 0, 16) };

        stack.Children.Add(BuildSectionRow("ALL GAMES", showNew: false));

        var grid = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 0),
        };
        grid.SizeChanged += (s, e) => AdaptGameCards((WrapPanel)s!, GameCardWidth);
        foreach (var g in AllGames)
            grid.Children.Add(CreateGameCard(g));
        stack.Children.Add(grid);

        scroll.Content = stack;
        return scroll;
    }


    static Control BuildFavoritesTabContent()
    {
        var center = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            Spacing = 12, Margin = new Thickness(32),
        };

        center.Children.Add(new Border
        {
            Width = 60, Height = 60,
            Background = new SolidColorBrush(SurfaceColor),
            BorderBrush = new SolidColorBrush(PrimaryColor),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new PathIcon
            {
                Width = 28, Height = 28,
                Foreground = new SolidColorBrush(PrimaryColor),
                Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
            },
        });
        center.Children.Add(new TextBlock
        {
            Text = "NO FAVORITES YET",
            FontFamily = RetroFont, FontSize = 10, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(MutedColor),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        center.Children.Add(new TextBlock
        {
            Text = "Add games to your favorites\nto find them here.",
            FontFamily = RetroFont, FontSize = 9,
            Foreground = new SolidColorBrush(MutedColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });

        return center;
    }


    static Control BuildProfileTabContent()
    {
        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0 };

        var avatarSection = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8, Margin = new Thickness(16, 24, 16, 16),
        };

        var avatarBmp = LoadAsset("hero.jpg");
        avatarSection.Children.Add(new Border
        {
            Width = 70, Height = 70,
            Background = new SolidColorBrush(SurfaceColor),
            BorderBrush = new SolidColorBrush(PrimaryColor),
            BorderThickness = new Thickness(2),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = avatarBmp != null
                ? (Control)new Image { Source = avatarBmp, Stretch = Stretch.UniformToFill }
                : new TextBlock
                  {
                      Text = "P1", FontFamily = RetroFont, FontSize = 22, FontWeight = FontWeight.Bold,
                      Foreground = new SolidColorBrush(CyanColor),
                      HorizontalAlignment = HorizontalAlignment.Center,
                      VerticalAlignment   = VerticalAlignment.Center,
                  },
        });

        avatarSection.Children.Add(new TextBlock
        {
            Text = "PLAYER 1",
            FontFamily = RetroFont, FontSize = 12, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextColor),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        avatarSection.Children.Add(new TextBlock
        {
            Text = "LV. 42  ■ 4,520 XP",
            FontFamily = RetroFont, FontSize = 9,
            Foreground = new SolidColorBrush(YellowColor),
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        stack.Children.Add(avatarSection);

        var statsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            Margin = new Thickness(16, 0, 16, 16),
        };
        var statData = new[] { ("42", "GAMES"), ("128", "HOURS"), ("3", "TROPHIES") };
        for (int i = 0; i < statData.Length; i++)
        {
            var box = new Border
            {
                Background = new SolidColorBrush(SurfaceColor),
                BorderBrush = new SolidColorBrush(PrimaryColor),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(i == 0 ? 0 : 4, 0, 0, 0),
                Padding = new Thickness(8, 10),
            };
            var inner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            inner.Children.Add(new TextBlock
            {
                Text = statData[i].Item1,
                FontFamily = RetroFont, FontSize = 14, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(CyanColor),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            inner.Children.Add(new TextBlock
            {
                Text = statData[i].Item2,
                FontFamily = RetroFont, FontSize = 8,
                Foreground = new SolidColorBrush(MutedColor),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            box.Child = inner;
            Grid.SetColumn(box, i);
            statsGrid.Children.Add(box);
        }
        stack.Children.Add(statsGrid);

        scroll.Content = stack;
        return scroll;
    }


    ContentPage BuildDetailPage(string gameTitle)
    {
        var page = new ContentPage { Background = new SolidColorBrush(BgColor) };

        // Overlay mode: content extends behind the nav bar.
        NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
        // BarBackground is global, so swap it via lifecycle events.
        page.NavigatedTo   += (_, _) => { if (_nav != null) _nav.BarBackground = Brushes.Transparent; };
        page.NavigatedFrom += (_, _) => { if (_nav != null) _nav.BarBackground = new SolidColorBrush(SurfaceColor); };
        var cmdBar = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var heartBtn = new Button();
        heartBtn.Classes.Add("retro-icon-btn");
        heartBtn.Content = new PathIcon
        {
            Width = 16, Height = 16,
            Foreground = new SolidColorBrush(PrimaryColor),
            Data = Geometry.Parse("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
        };
        var shareBtn = new Button();
        shareBtn.Classes.Add("retro-icon-btn");
        shareBtn.Content = new PathIcon
        {
            Width = 16, Height = 16,
            Foreground = new SolidColorBrush(TextColor),
            Data = Geometry.Parse("M18,16.08C17.24,16.08 16.56,16.38 16.04,16.85L8.91,12.7C8.96,12.47 9,12.24 9,12C9,11.76 8.96,11.53 8.91,11.3L15.96,7.19C16.5,7.69 17.21,8 18,8A3,3 0 0,0 21,5A3,3 0 0,0 18,2A3,3 0 0,0 15,5C15,5.24 15.04,5.47 15.09,5.7L8.04,9.81C7.5,9.31 6.79,9 6,9A3,3 0 0,0 3,12A3,3 0 0,0 6,15C6.79,15 7.5,14.69 8.04,14.19L15.16,18.35C15.11,18.56 15.08,18.78 15.08,19C15.08,20.61 16.39,21.92 18,21.92C19.61,21.92 20.92,20.61 20.92,19C20.92,17.39 19.61,16.08 18,16.08Z"),
        };
        cmdBar.Children.Add(heartBtn);
        cmdBar.Children.Add(shareBtn);
        NavigationPage.SetTopCommandBar(page, cmdBar);

        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0 };

        stack.Children.Add(BuildDetailHero(gameTitle));
        var metaRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Margin = new Thickness(16, 12, 16, 0),
        };
        metaRow.Children.Add(MakeMetaBadge("⭐ 4.5 / 5", CyanColor));
        metaRow.Children.Add(MakeMetaBadge("YEAR 1992", MutedColor));
        metaRow.Children.Add(MakeMetaBadge("ACTION", PrimaryColor));
        stack.Children.Add(metaRow);
        var infoRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("80,*"),
            Margin = new Thickness(16, 14, 16, 0),
        };
        infoRow.Children.Add(BuildPixelAvatar());

        var descStack = new StackPanel { Spacing = 6, Margin = new Thickness(12, 0, 0, 0) };
        descStack.Children.Add(new TextBlock
        {
            Text = "MISSION BRIEFING",
            FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor),
        });
        descStack.Children.Add(new TextBlock
        {
            Text = "A legendary ninja warrior awakens in a\ncyberpunk city 100 years in the future.\nDefeat the Neon Corporation to restore\nhonor to the ancient clan.",
            FontFamily = RetroFont, FontSize = 9, LineHeight = 16,
            Foreground = new SolidColorBrush(TextColor),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(descStack, 1);
        infoRow.Children.Add(descStack);
        stack.Children.Add(infoRow);
        var coinBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(16, 16, 16, 0),
            Padding = new Thickness(0, 14),
            CornerRadius = new CornerRadius(0),
        };
        coinBtn.Classes.Add("retro-primary-btn");
        var coinContent = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        coinContent.Children.Add(new PathIcon
        {
            Width = 14, Height = 14,
            Foreground = Brushes.White,
            Data = Geometry.Parse("M7.97,16L5,19C4.67,19.3 4.23,19.5 3.75,19.5A1.75,1.75 0 0,1 2,17.75V17.5L3,10.12C3.21,7.81 5.14,6 7.5,6H16.5C18.86,6 20.79,7.81 21,10.12L22,17.5V17.75A1.75,1.75 0 0,1 20.25,19.5C19.77,19.5 19.33,19.3 19,19L16.03,16H7.97M7,9V11H5V13H7V15H9V13H11V11H9V9H7M14.5,12A1.5,1.5 0 0,0 13,13.5A1.5,1.5 0 0,0 14.5,15A1.5,1.5 0 0,0 16,13.5A1.5,1.5 0 0,0 14.5,12M17.5,9A1.5,1.5 0 0,0 16,10.5A1.5,1.5 0 0,0 17.5,12A1.5,1.5 0 0,0 19,10.5A1.5,1.5 0 0,0 17.5,9Z"),
        });
        coinContent.Children.Add(new TextBlock
        {
            Text = "INSERT COIN",
            FontFamily = RetroFont, FontSize = 11, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
        });
        coinBtn.Content = coinContent;
        stack.Children.Add(coinBtn);

        stack.Children.Add(new TextBlock
        {
            Text = "COMMUNITY SCORE",
            FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor),
            Margin = new Thickness(16, 20, 16, 8),
        });
        stack.Children.Add(BuildCommunityScore());
        stack.Children.Add(BuildMetadataFooter());

        scroll.Content = stack;
        page.Content   = scroll;
        return page;
    }

    Control BuildDetailHero(string gameTitle)
    {
        var border = new Border { Height = 240, ClipToBounds = true };
        var grid   = new Grid();

        var heroImg = MakeGameImage(gameTitle) ?? MakeGameImage("Neon Ninja");
        if (heroImg != null)
        {
            grid.Children.Add(heroImg);
        }
        else
        {
            var grad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
            };
            grad.GradientStops.Add(new GradientStop(Color.Parse("#3d2060"), 0));
            grad.GradientStops.Add(new GradientStop(BgColor, 1));
            grid.Children.Add(new Border { Background = grad });
        }

        grid.Children.Add(BuildPixelDecorations());

        var overlay = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        overlay.GradientStops.Add(new GradientStop(Color.Parse("#00000000"), 0.4));
        overlay.GradientStops.Add(new GradientStop(Color.FromArgb(200, 18, 10, 31), 1.0));
        grid.Children.Add(new Border { Background = overlay });

        grid.Children.Add(new TextBlock
        {
            Text = gameTitle.ToUpperInvariant(),
            FontFamily = RetroFont, FontSize = 20, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(CyanColor),
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 16, 12),
        });

        border.Child = grid;
        return border;
    }

    static Control BuildPixelDecorations()
    {
        var canvas = new Canvas
        {
            Width = 220, Height = 150,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var pixels = new (double x, double y, double w, double h, Color c)[]
        {
            (20, 20, 8, 8, CyanColor),    (30, 12, 8, 8, CyanColor),   (30, 20, 8, 8, CyanColor),
            (40, 20, 8, 8, CyanColor),    (30, 28, 8, 8, CyanColor),
            (80, 35, 12, 12, PrimaryColor), (92, 22, 12, 12, PrimaryColor),
            (104, 35, 12, 12, PrimaryColor), (92, 47, 12, 12, PrimaryColor),
            (150, 18, 10, 10, YellowColor), (150, 42, 10, 10, YellowColor),
            (165, 30, 8, 8, Color.Parse("#ff4466")),
            (190, 15, 6, 6, CyanColor),   (200, 25, 4, 4, PrimaryColor),
            (180, 60, 8, 8, YellowColor),
        };

        foreach (var (x, y, w, h, c) in pixels)
        {
            var box = new Border { Width = w, Height = h, Background = new SolidColorBrush(c) };
            Canvas.SetLeft(box, x);
            Canvas.SetTop(box, y);
            canvas.Children.Add(box);
        }

        return canvas;
    }

    static Control BuildPixelAvatar()
    {
        var outer = new Border
        {
            Width = 72, Height = 80,
            BorderBrush = new SolidColorBrush(PrimaryColor),
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(SurfaceColor),
        };

        var inner = new Grid { RowDefinitions = new RowDefinitions("22,26,18,*") };

        var head = new Border
        {
            Width = 22, Height = 20,
            Background = new SolidColorBrush(CyanColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        Grid.SetRow(head, 0);
        inner.Children.Add(head);

        var body = new Border
        {
            Width = 30, Height = 24,
            Background = new SolidColorBrush(PrimaryColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        Grid.SetRow(body, 1);
        inner.Children.Add(body);

        var legs = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        legs.Children.Add(new Border { Width = 11, Height = 14, Background = new SolidColorBrush(MutedColor) });
        legs.Children.Add(new Border { Width = 11, Height = 14, Background = new SolidColorBrush(MutedColor) });
        Grid.SetRow(legs, 2);
        inner.Children.Add(legs);

        outer.Child = inner;
        return outer;
    }

    static Control BuildCommunityScore()
    {
        var stack = new StackPanel { Spacing = 5, Margin = new Thickness(16, 0, 16, 0) };

        var ratings = new[] { ("5 ★", 0.60), ("4 ★", 0.25), ("3 ★", 0.10), ("2 ★", 0.03), ("1 ★", 0.02) };
        foreach (var (label, pct) in ratings)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("32,*,32") };

            var lbl = new TextBlock
            {
                Text = label, FontFamily = RetroFont, FontSize = 8,
                Foreground = new SolidColorBrush(MutedColor),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(lbl, 0);
            row.Children.Add(lbl);

            int fillPct  = (int)(pct * 100);
            int emptyPct = 100 - fillPct;
            var barOuter = new Grid { ColumnDefinitions = new ColumnDefinitions($"{fillPct}*,{emptyPct}*") };
            barOuter.Children.Add(new Border { Height = 6, Background = new SolidColorBrush(PrimaryColor) });
            var barEmpty = new Border
            {
                Height = 6,
                Background = new SolidColorBrush(Color.FromArgb(80, SurfaceColor.R, SurfaceColor.G, SurfaceColor.B)),
            };
            Grid.SetColumn(barEmpty, 1);
            barOuter.Children.Add(barEmpty);
            Grid.SetColumn(barOuter, 1);
            row.Children.Add(barOuter);

            var pctLbl = new TextBlock
            {
                Text = $"{fillPct}%", FontFamily = RetroFont, FontSize = 8,
                Foreground = new SolidColorBrush(MutedColor),
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(pctLbl, 2);
            row.Children.Add(pctLbl);

            stack.Children.Add(row);
        }
        return stack;
    }

    static Control BuildMetadataFooter()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions    = new RowDefinitions("Auto,Auto"),
            Margin            = new Thickness(16, 16, 16, 24),
        };

        var items = new[] { ("DEVELOPER", "PixelForge"), ("PUBLISHER", "RetroSoft"), ("SIZE", "12 MB"), ("PLAYERS", "1-2 CO-OP") };
        for (int i = 0; i < items.Length; i++)
        {
            var box = new Border
            {
                Background = new SolidColorBrush(SurfaceColor),
                BorderBrush = new SolidColorBrush(PrimaryColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8),
                Margin = new Thickness(i % 2 == 0 ? 0 : 4, i < 2 ? 0 : 4, i % 2 == 0 ? 4 : 0, 0),
            };
            var inner = new StackPanel { Spacing = 2 };
            inner.Children.Add(new TextBlock
            {
                Text = items[i].Item1, FontFamily = RetroFont, FontSize = 7,
                Foreground = new SolidColorBrush(MutedColor),
            });
            inner.Children.Add(new TextBlock
            {
                Text = items[i].Item2, FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(TextColor),
            });
            box.Child = inner;
            Grid.SetRow(box, i / 2);
            Grid.SetColumn(box, i % 2);
            grid.Children.Add(box);
        }
        return grid;
    }


    Control CreateGameCard(GameEntry game)
    {
        var btn = new Button
        {
            Padding = new Thickness(4),
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(0),
            Tag = game.Title,
        };
        btn.Classes.Add("retro-list-btn");
        btn.Click += OnGameClick;

        var card = new Border
        {
            Width = GameCardWidth,
            Background = new SolidColorBrush(SurfaceColor),
            BorderBrush = new SolidColorBrush(game.Accent),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var inner = new StackPanel { Spacing = 0 };

        // Art area: real cover image or gradient fallback
        var artArea = new Border { Height = 90, ClipToBounds = true };
        var artGrid = new Grid();

        var coverImg = MakeGameImage(game.Title);
        if (coverImg != null)
        {
            artGrid.Children.Add(coverImg);
        }
        else
        {
            var artGrad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
            };
            artGrad.GradientStops.Add(new GradientStop(Color.FromArgb(180, game.Accent.R, game.Accent.G, game.Accent.B), 0));
            artGrad.GradientStops.Add(new GradientStop(SurfaceColor, 1));
            artGrid.Children.Add(new Border { Background = artGrad });
            artGrid.Children.Add(new TextBlock
            {
                Text = game.Title[0].ToString(),
                FontFamily = RetroFont, FontSize = 36, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            });
        }

        if (game.IsHot)
        {
            artGrid.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#ff4466")),
                Padding = new Thickness(4, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment   = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = "HOT", FontFamily = RetroFont, FontSize = 7, FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                },
            });
        }
        artArea.Child = artGrid;
        inner.Children.Add(artArea);

        var info = new StackPanel { Spacing = 4, Margin = new Thickness(8, 6, 8, 8) };
        info.Children.Add(new TextBlock
        {
            Text = game.Title, FontFamily = RetroFont, FontSize = 9, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        info.Children.Add(MakeGenreTag(game.Genre, game.Accent));
        inner.Children.Add(info);

        card.Child = inner;
        btn.Content = card;
        return btn;
    }

    static void AdaptGameCards(WrapPanel panel, double defaultWidth)
    {
        var available = panel.Bounds.Width;
        if (available <= 0)
            return;

        bool singleColumn = available < defaultWidth * 2;

        foreach (var child in panel.Children)
        {
            if (child is Button btn && btn.Content is Border card)
                card.Width = singleColumn ? available : defaultWidth;
        }
    }

    Control CreateContinueCard(ContinueGame game)
    {
        var btn = new Button
        {
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(0),
            Tag = game.Title,
            MinWidth = 240,
        };
        btn.Classes.Add("retro-list-btn");
        btn.Click += OnGameClick;

        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1a0a33")),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, game.Accent.R, game.Accent.G, game.Accent.B)),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
        };

        var outerStack = new StackPanel { Spacing = 0 };

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("80,*,40"),
            MinHeight = 80,
        };
        var imgBorder = new Border { Width = 80, ClipToBounds = true };
        var coverImg  = MakeGameImage(game.Title);
        if (coverImg != null)
        {
            imgBorder.Child = coverImg;
        }
        else
        {
            var fallGrad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
            };
            fallGrad.GradientStops.Add(new GradientStop(Color.FromArgb(150, game.Accent.R, game.Accent.G, game.Accent.B), 0));
            fallGrad.GradientStops.Add(new GradientStop(Color.Parse("#1a0a33"), 1));
            imgBorder.Child = new Border { Background = fallGrad };
        }
        Grid.SetColumn(imgBorder, 0);
        topRow.Children.Add(imgBorder);
        var info = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 10, 0, 10),
        };
        info.Children.Add(new TextBlock
        {
            Text = game.Title.ToUpperInvariant(),
            FontFamily = RetroFont, FontSize = 11, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(CyanColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        info.Children.Add(new TextBlock
        {
            Text = game.Status,
            FontFamily = RetroFont, FontSize = 9,
            Foreground = new SolidColorBrush(MutedColor),
        });
        Grid.SetColumn(info, 1);
        topRow.Children.Add(info);

        // Right: yellow ▶ play button — fixed 40×40 square, centered vertically
        var playColContainer = new Panel { Width = 40 };
        var playSquare = new Border
        {
            Width = 40, Height = 40,
            Background = new SolidColorBrush(YellowColor),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PathIcon
            {
                Width = 16, Height = 16,
                Foreground = new SolidColorBrush(BgColor),
                Data = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"),
            },
        };
        playColContainer.Children.Add(playSquare);
        Grid.SetColumn(playColContainer, 2);
        topRow.Children.Add(playColContainer);

        outerStack.Children.Add(topRow);

        var hpLabelRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(10, 4, 10, 2),
        };
        hpLabelRow.Children.Add(new TextBlock
        {
            Text = "HP", FontFamily = RetroFont, FontSize = 8,
            Foreground = new SolidColorBrush(MutedColor),
        });
        var pctLabel = new TextBlock
        {
            Text = $"{game.Progress}%", FontFamily = RetroFont, FontSize = 8, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(YellowColor),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        Grid.SetColumn(pctLabel, 1);
        hpLabelRow.Children.Add(pctLabel);
        outerStack.Children.Add(hpLabelRow);

        int fill  = game.Progress;
        int empty = 100 - fill;
        var barOuter = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions($"{fill}*,{empty}*"),
            Height = 5, Margin = new Thickness(0),
        };
        barOuter.Children.Add(new Border { Background = new SolidColorBrush(PrimaryColor) });
        var barEmpty = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(60, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B)),
        };
        Grid.SetColumn(barEmpty, 1);
        barOuter.Children.Add(barEmpty);
        outerStack.Children.Add(barOuter);

        card.Child = outerStack;
        btn.Content = card;
        return btn;
    }


    static Border MakeGenreTag(string genre, Color accent, bool solid = false) => new Border
    {
        Background = new SolidColorBrush(solid ? accent : Color.FromArgb(50, accent.R, accent.G, accent.B)),
        BorderBrush = new SolidColorBrush(accent),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(5, 2),
        Child = new TextBlock
        {
            Text = genre,
            FontFamily = new FontFamily("Courier New, monospace"),
            FontSize = 7, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(solid ? BgColor : accent),
        },
    };

    static Border MakeMetaBadge(string text, Color accent) => new Border
    {
        Background = new SolidColorBrush(Color.FromArgb(50, accent.R, accent.G, accent.B)),
        BorderBrush = new SolidColorBrush(accent),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(8, 3),
        Child = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Courier New, monospace"),
            FontSize = 9, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(accent),
        },
    };


    void OnGameClick(object? sender, RoutedEventArgs e)
    {
        string title = "Neon Ninja";
        if (sender is Button btn && btn.Tag is string tag)
            title = tag;
        _nav?.Push(BuildDetailPage(title));
    }
}
