using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages;

public partial class AvaloniaFlixAppPage : UserControl
{

    static readonly string[] MovieTitles =
    {
        "Neon Horizon", "Shadow Protocol", "Iron Bloom",
        "Void Runners", "Eclipse Rising", "Phantom Code",
        "Starfall Legacy", "Crimson Depths", "Astral Divide",
        "Binary Dawn", "Pulse Override", "Glacier Eye",
    };

    static readonly string[] MovieAssets =
    {
        "avares://ControlCatalog/Assets/Movies/trending1.jpg",
        "avares://ControlCatalog/Assets/Movies/trending2.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated1.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated2.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated3.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated4.jpg",
        "avares://ControlCatalog/Assets/Movies/continue1.jpg",
        "avares://ControlCatalog/Assets/Movies/morelike1.jpg",
        "avares://ControlCatalog/Assets/Movies/search1.jpg",
        "avares://ControlCatalog/Assets/Movies/hero.jpg",
        "avares://ControlCatalog/Assets/Movies/cast1.jpg",
        "avares://ControlCatalog/Assets/Movies/cast2.jpg",
    };

    static readonly string[] CastAssets =
    {
        "avares://ControlCatalog/Assets/Movies/cast1.jpg",
        "avares://ControlCatalog/Assets/Movies/cast2.jpg",
        "avares://ControlCatalog/Assets/Movies/trending1.jpg",
        "avares://ControlCatalog/Assets/Movies/trending2.jpg",
    };
    static readonly string[] CastNames      = { "Aris Thorne", "Lyra Vance", "Jax Markus", "Nova Elena" };
    static readonly string[] CastCharacters = { "Kaelen Voss", "Sera Finn",  "Reed",       "Joy"        };


    NavigationPage? _detailNav;
    bool _initialized;
    ScrollViewer? _infoPanel;


    public AvaloniaFlixAppPage() => InitializeComponent();


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
        UpdateInfoPanelVisibility();

        if (_initialized) return;
        _initialized = true;

        _detailNav = this.FindControl<NavigationPage>("DetailNav");
        if (_detailNav != null)
        {
            _detailNav.ModalTransition = new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical);
            _detailNav.Push(BuildHomePage());
        }
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
        var page = new ContentPage { Background = Brushes.Transparent };

        page.Header = new TextBlock
        {
            Text = "AVALONIAFLIX",
            Foreground = new SolidColorBrush(Color.Parse("#E50914")),
            FontSize = 18, FontWeight = FontWeight.Black,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var cmdBar = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var searchBtn = new Button { Padding = new Thickness(4) };
        searchBtn.Classes.Add("flixTransparent");
        searchBtn.Click += OnSearchClick;
        searchBtn.Content = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
        };
        cmdBar.Children.Add(searchBtn);
        cmdBar.Children.Add(new Border
        {
            Width = 30, Height = 30, CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.Parse("#333333")),
            Child = new TextBlock
            {
                Text = "JD", FontSize = 10, FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            },
        });
        NavigationPage.SetTopCommandBar(page, cmdBar);

        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0 };
        var pills = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 24,
            Margin = new Thickness(20, 8, 20, 4),
        };
        foreach (var label in new[] { "TV Shows", "Movies", "Categories" })
        {
            pills.Children.Add(new TextBlock
            {
                Text = label, FontSize = 14, FontWeight = FontWeight.Medium,
                Foreground = Brushes.White,
            });
        }
        stack.Children.Add(pills);
        stack.Children.Add(BuildHeroSection());
        stack.Children.Add(MakeSectionLabel("Trending Now", 16, 16, 16, 8));
        var trendingScroll = MakeHScrollViewer(b: 8);
        var trendingRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Margin = new Thickness(16, 0),
        };
        for (int i = 0; i < 8; i++)
            trendingRow.Children.Add(CreatePosterCard(MovieTitles[i % MovieTitles.Length], 110, 165, i));
        trendingScroll.Content = trendingRow;
        stack.Children.Add(trendingScroll);
        stack.Children.Add(MakeSectionLabel("Continue Watching for You", 16, 12, 16, 8));
        var continueScroll = MakeHScrollViewer(b: 8);
        var continueRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            Margin = new Thickness(16, 0),
        };
        for (int i = 0; i < 5; i++)
            continueRow.Children.Add(CreateContinueCard(MovieTitles[(i + 3) % MovieTitles.Length], i));
        continueScroll.Content = continueRow;
        stack.Children.Add(continueScroll);
        stack.Children.Add(MakeSectionLabel("Top Rated", 16, 12, 16, 8));
        var topRatedScroll = MakeHScrollViewer(b: 24);
        var topRatedRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Margin = new Thickness(16, 0),
        };
        for (int i = 0; i < 8; i++)
            topRatedRow.Children.Add(CreatePosterCard(MovieTitles[(i + 5) % MovieTitles.Length], 110, 165, i + 5));
        topRatedScroll.Content = topRatedRow;
        stack.Children.Add(topRatedScroll);

        scroll.Content = stack;
        page.Content   = scroll;
        return page;
    }

    ContentPage BuildDetailPage(string movieTitle)
    {
        var page = new ContentPage { Background = Brushes.Transparent };

        // Nav-bar title: BarForeground=White
        page.Header = new TextBlock
        {
            Text = movieTitle, FontSize = 17, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center,
        };
        var cmdBar = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var shareBtn = new Button { Padding = new Thickness(8) };
        shareBtn.Classes.Add("flixTransparent");
        shareBtn.Content = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Geometry.Parse("M18,16.08C17.24,16.08 16.56,16.38 16.04,16.85L8.91,12.7C8.96,12.47 9,12.24 9,12C9,11.76 8.96,11.53 8.91,11.3L15.96,7.19C16.5,7.69 17.21,8 18,8A3,3 0 0,0 21,5A3,3 0 0,0 18,2A3,3 0 0,0 15,5C15,5.24 15.04,5.47 15.09,5.7L8.04,9.81C7.5,9.31 6.79,9 6,9A3,3 0 0,0 3,12A3,3 0 0,0 6,15C6.79,15 7.5,14.69 8.04,14.19L15.16,18.35C15.11,18.56 15.08,18.78 15.08,19C15.08,20.61 16.39,21.92 18,21.92C19.61,21.92 20.92,20.61 20.92,19C20.92,17.39 19.61,16.08 18,16.08Z"),
        };
        var bookmarkBtn = new Button { Padding = new Thickness(8) };
        bookmarkBtn.Classes.Add("flixTransparent");
        bookmarkBtn.Content = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Geometry.Parse("M17,3H7A2,2 0 0,0 5,5V21L12,18L19,21V5C19,3.89 18.1,3 17,3Z"),
        };
        cmdBar.Children.Add(shareBtn);
        cmdBar.Children.Add(bookmarkBtn);
        NavigationPage.SetTopCommandBar(page, cmdBar);

        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0 };

        var rng    = new Random(movieTitle.GetHashCode());
        int imgIdx = Math.Abs(movieTitle.GetHashCode()) % MovieAssets.Length;

        var heroBorder = new Border { Height = 300, ClipToBounds = true };
        var heroGrid   = new Grid();
        try
        {
            heroGrid.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(MovieAssets[imgIdx]))),
                Stretch = Stretch.UniformToFill,
            });
        }
        catch { }
        var grad = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        grad.GradientStops.Add(new GradientStop(Color.Parse("#00000000"), 0.3));
        grad.GradientStops.Add(new GradientStop(Color.Parse("#FF0A0A0A"), 1.0));
        heroGrid.Children.Add(new Border { Background = grad });

        // Title overlaid at bottom of hero
        var heroText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(24, 0, 24, 16), Spacing = 6,
        };
        heroText.Children.Add(new TextBlock
        {
            Text = movieTitle, FontSize = 40, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
        });
        heroGrid.Children.Add(heroText);
        heroBorder.Child = heroGrid;
        stack.Children.Add(heroBorder);

        string year     = (2020 + rng.Next(6)).ToString();
        string rating   = $"{6.5 + rng.NextDouble() * 3.0:F1}/10";
        int    mins     = 90 + rng.Next(60);
        string duration = $"{mins / 60}h {mins % 60}m";

        var meta = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            Margin = new Thickness(24, 16, 24, 0),
        };
        meta.Children.Add(new TextBlock { Text = year, FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")) });
        meta.Children.Add(MakeDot());
        var ratingRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        ratingRow.Children.Add(new PathIcon
        {
            Width = 13, Height = 13,
            Foreground = new SolidColorBrush(Color.Parse("#E50914")),
            Data = Geometry.Parse("M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"),
        });
        ratingRow.Children.Add(new TextBlock { Text = rating, FontSize = 14, Foreground = Brushes.White, FontWeight = FontWeight.SemiBold });
        meta.Children.Add(ratingRow);
        meta.Children.Add(MakeDot());
        meta.Children.Add(new TextBlock { Text = duration, FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")) });
        stack.Children.Add(meta);

        var tags = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Margin = new Thickness(24, 12, 24, 0),
        };
        foreach (var genre in new[] { "SCI-FI", "ACTION", "EPIC" })
        {
            tags.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(4), Padding = new Thickness(12, 5),
                BorderBrush     = new SolidColorBrush(Color.Parse("#666666")),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = genre, FontSize = 11, FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White,
                },
            });
        }
        stack.Children.Add(tags);

        var actionGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(24, 16, 24, 0),
        };
        var playBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0, 13), CornerRadius = new CornerRadius(4),
        };
        playBtn.Classes.Add("flixPlayRed");
        var playContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        playContent.Children.Add(new PathIcon
        {
            Width = 18, Height = 18, Foreground = Brushes.White,
            Data = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"),
        });
        playContent.Children.Add(new TextBlock
        {
            Text = "Play", FontSize = 15, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center,
        });
        playBtn.Content = playContent;
        Grid.SetColumn(playBtn, 0);
        actionGrid.Children.Add(playBtn);

        var dlBtn = new Button
        {
            CornerRadius = new CornerRadius(4), Padding = new Thickness(13),
            Margin = new Thickness(10, 0, 0, 0),
            Content = new PathIcon
            {
                Width = 20, Height = 20,
                Foreground = new SolidColorBrush(Color.Parse("#E50914")),
                Data = Geometry.Parse("M5,20H19V18H5M19,9H15V3H9V9H5L12,16L19,9Z"),
            },
        };
        dlBtn.Classes.Add("flixIconBtn");
        Grid.SetColumn(dlBtn, 1);
        actionGrid.Children.Add(dlBtn);

        var likeBtn = new Button
        {
            CornerRadius = new CornerRadius(4), Padding = new Thickness(13),
            Margin = new Thickness(8, 0, 0, 0),
            Content = new PathIcon
            {
                Width = 20, Height = 20,
                Foreground = new SolidColorBrush(Color.Parse("#E50914")),
                Data = Geometry.Parse("M1,21H5V9H1V21M23,10C23,8.9 22.1,8 21,8H14.68L15.64,3.43L15.67,3.11C15.67,2.7 15.5,2.32 15.23,2.05L14.17,1L7.59,7.59C7.22,7.95 7,8.45 7,9V19A2,2 0 0,0 9,21H18C18.83,21 19.54,20.5 19.84,19.78L22.86,12.73C22.95,12.5 23,12.26 23,12V10Z"),
            },
        };
        likeBtn.Classes.Add("flixIconBtn");
        Grid.SetColumn(likeBtn, 2);
        actionGrid.Children.Add(likeBtn);
        stack.Children.Add(actionGrid);

        stack.Children.Add(new TextBlock
        {
            Text = "Synopsis", FontSize = 18, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White, Margin = new Thickness(24, 24, 24, 10),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "In a future where water is the only currency, a lone wanderer named Aris Thorne discovers a forgotten data-bank hidden beneath the shifting crimson sands of the Forbidden Sector. As rival corporate factions close in, Aris must lead a desperate band of outcasts to protect the secrets of the Cyber Dune.",
            TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20,
            Foreground = new SolidColorBrush(Color.Parse("#999999")),
            Margin = new Thickness(24, 0),
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Top Cast", FontSize = 18, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White, Margin = new Thickness(24, 24, 24, 12),
        });
        var castScroll = MakeHScrollViewer();
        var castRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 16,
            Margin = new Thickness(24, 0),
        };
        for (int i = 0; i < CastNames.Length; i++)
            castRow.Children.Add(CreateCastItem(i));
        castScroll.Content = castRow;
        stack.Children.Add(castScroll);

        var moreHeader = new Grid { Margin = new Thickness(24, 24, 24, 12) };
        moreHeader.Children.Add(new TextBlock
        {
            Text = "More Like This", FontSize = 18, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
        });
        moreHeader.Children.Add(new TextBlock
        {
            Text = "View All", FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#E50914")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Center,
        });
        stack.Children.Add(moreHeader);

        var moreScroll = MakeHScrollViewer(b: 24);
        var moreRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 10,
            Margin = new Thickness(24, 0),
        };
        for (int i = 0; i < 6; i++)
            moreRow.Children.Add(CreatePosterCard(MovieTitles[(i + 6) % MovieTitles.Length], 130, 195, i + 6));
        moreScroll.Content = moreRow;
        stack.Children.Add(moreScroll);

        scroll.Content = stack;
        page.Content   = scroll;
        return page;
    }

    ContentPage BuildSearchPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(Color.Parse("#0A0A0A")) };
        NavigationPage.SetHasNavigationBar(page, false);

        var topBar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(16, 12, 12, 8),
        };

        var searchInnerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        searchInnerGrid.Children.Add(new PathIcon
        {
            Width = 15, Height = 15,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Data = Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
            [Grid.ColumnProperty] = 0,
        });
        searchInnerGrid.Children.Add(new TextBlock
        {
            Text = "Search AvaloniaFlix", FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            VerticalAlignment = VerticalAlignment.Center,
            [Grid.ColumnProperty] = 1,
        });
        var searchBar = new Border
        {
            CornerRadius = new CornerRadius(4),
            Background   = new SolidColorBrush(Color.Parse("#222222")),
            Padding      = new Thickness(12, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Child = searchInnerGrid,
        };
        Grid.SetColumn(searchBar, 0);
        topBar.Children.Add(searchBar);

        var micBtn = new Button { Padding = new Thickness(8), Margin = new Thickness(6, 0, 0, 0) };
        micBtn.Classes.Add("flixTransparent");
        micBtn.Content = new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.White,
            Data = Geometry.Parse("M12,2A3,3 0 0,1 15,5V11A3,3 0 0,1 12,14A3,3 0 0,1 9,11V5A3,3 0 0,1 12,2M19,11C19,14.53 16.39,17.44 13,17.93V21H11V17.93C7.61,17.44 5,14.53 5,11H7A5,5 0 0,0 12,16A5,5 0 0,0 17,11H19Z"),
        };
        Grid.SetColumn(micBtn, 1);
        topBar.Children.Add(micBtn);

        var closeBtn = new Button { Padding = new Thickness(8), Margin = new Thickness(2, 0, 0, 0) };
        closeBtn.Classes.Add("flixTransparent");
        closeBtn.Content = new PathIcon
        {
            Width = 18, Height = 18, Foreground = Brushes.White,
            Data = Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z"),
        };
        closeBtn.Click += async (_, _) => await (page.Navigation?.PopModalAsync() ?? Task.CompletedTask);
        Grid.SetColumn(closeBtn, 2);
        topBar.Children.Add(closeBtn);

        var scroll = new ScrollViewer();
        var stack  = new StackPanel { Spacing = 0 };

        stack.Children.Add(new TextBlock
        {
            Text = "Popular Searches", FontSize = 18, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White, Margin = new Thickness(16, 8, 16, 10),
        });
        string[] popularTitles = { "Neon Horizon", "Shadow Protocol", "Iron Bloom", "Void Runners", "Eclipse Rising" };
        var popularPanel = new StackPanel { Spacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        for (int i = 0; i < popularTitles.Length; i++)
            popularPanel.Children.Add(CreateSearchItem(popularTitles[i], i));
        stack.Children.Add(popularPanel);

        stack.Children.Add(new TextBlock
        {
            Text = "Top Movies & TV", FontSize = 18, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White, Margin = new Thickness(16, 12, 16, 10),
        });
        var topGrid = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 0, 12, 24),
        };
        string[] badges = { "", "", "TOP 10", "NEW EPISODES", "", "TOP 10", "", "" };
        for (int i = 0; i < 8; i++)
            topGrid.Children.Add(CreateGridPosterCard(MovieTitles[(i + 2) % MovieTitles.Length], i + 2, badges[i]));
        stack.Children.Add(topGrid);

        scroll.Content = stack;

        var dock = new DockPanel();
        DockPanel.SetDock(topBar, Dock.Top);
        dock.Children.Add(topBar);
        dock.Children.Add(scroll);

        page.Content = dock;
        return page;
    }

    Control BuildHeroSection()
    {
        var border = new Border { Height = 360, ClipToBounds = true, Margin = new Thickness(0, 4, 0, 4) };
        var grid   = new Grid();

        try
        {
            grid.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ControlCatalog/Assets/Movies/hero.jpg"))),
                Stretch = Stretch.UniformToFill,
            });
        }
        catch { }

        var gradBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        gradBrush.GradientStops.Add(new GradientStop(Color.Parse("#B0000000"), 0.0));
        gradBrush.GradientStops.Add(new GradientStop(Color.Parse("#00000000"), 0.4));
        gradBrush.GradientStops.Add(new GradientStop(Color.Parse("#FF0A0A0A"), 1.0));
        grid.Children.Add(new Border { Background = gradBrush });

        var heroStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 16, 16), Spacing = 8,
        };

        heroStack.Children.Add(new TextBlock
        {
            Text = "CYBER DUNE", FontSize = 38, FontWeight = FontWeight.Black,
            Foreground = Brushes.White,
        });
        var infoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        infoRow.Children.Add(new TextBlock
        {
            Text = "98% Match", FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#46D369")),
            FontWeight = FontWeight.SemiBold,
        });
        infoRow.Children.Add(MakePill("2024"));
        infoRow.Children.Add(new TextBlock { Text = "2h 14m", FontSize = 11, Foreground = Brushes.White });
        infoRow.Children.Add(MakePill("HD"));
        heroStack.Children.Add(infoRow);

        heroStack.Children.Add(new TextBlock
        {
            Text = "Dystopian  \u2022  Sci-Fi  \u2022  Action",
            FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
        });
        var btnGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,12,*"),
            Margin = new Thickness(0, 4, 0, 0),
        };
        var playBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0, 10), CornerRadius = new CornerRadius(4),
            Tag = "Cyber Dune",
        };
        playBtn.Classes.Add("flixPlayWhite");
        playBtn.Click += OnMovieClick;
        var playContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        playContent.Children.Add(new PathIcon
        {
            Width = 20, Height = 20, Foreground = Brushes.Black,
            Data = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"),
        });
        playContent.Children.Add(new TextBlock
        {
            Text = "Play", FontSize = 14, FontWeight = FontWeight.Bold,
            Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center,
        });
        playBtn.Content = playContent;
        Grid.SetColumn(playBtn, 0);
        btnGrid.Children.Add(playBtn);

        var listBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0, 10), CornerRadius = new CornerRadius(4),
        };
        listBtn.Classes.Add("flixGhost");
        var listContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        listContent.Children.Add(new PathIcon
        {
            Width = 18, Height = 18, Foreground = Brushes.White,
            Data = Geometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"),
        });
        listContent.Children.Add(new TextBlock
        {
            Text = "My List", FontSize = 14, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center,
        });
        listBtn.Content = listContent;
        Grid.SetColumn(listBtn, 2);
        btnGrid.Children.Add(listBtn);

        heroStack.Children.Add(btnGrid);
        grid.Children.Add(heroStack);
        border.Child = grid;
        return border;
    }

    static Border MakeDot() => new Border
    {
        Width = 4, Height = 4, CornerRadius = new CornerRadius(2),
        Background = new SolidColorBrush(Color.Parse("#E50914")),
        VerticalAlignment = VerticalAlignment.Center,
    };

    static Border MakePill(string text) => new Border
    {
        CornerRadius = new CornerRadius(2), Padding = new Thickness(4, 1),
        BorderBrush = new SolidColorBrush(Color.Parse("#666666")),
        BorderThickness = new Thickness(1),
        Child = new TextBlock { Text = text, FontSize = 9, Foreground = Brushes.White },
    };

    static TextBlock MakeSectionLabel(string text, double l, double t, double r, double b) =>
        new TextBlock
        {
            Text = text, FontSize = 16, FontWeight = FontWeight.Bold,
            Foreground = Brushes.White, Margin = new Thickness(l, t, r, b),
        };

    static ScrollViewer MakeHScrollViewer(double l = 0, double t = 0, double r = 0, double b = 0) =>
        new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Disabled,
            Margin = new Thickness(l, t, r, b),
        };

    Control CreatePosterCard(string title, double width, double height, int index)
    {
        var btn = new Button
        {
            Background = Brushes.Transparent, Padding = new Thickness(0),
            Tag = title, Cursor = new Cursor(StandardCursorType.Hand),
        };
        btn.Click += OnMovieClick;

        var border = new Border
        {
            Width = width, Height = height,
            CornerRadius = new CornerRadius(6), ClipToBounds = true,
        };

        var grid = new Grid();
        try
        {
            grid.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(MovieAssets[index % MovieAssets.Length]))),
                Stretch = Stretch.UniformToFill,
            });
        }
        catch { }

        // Gradient overlay
        var gradBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        gradBrush.GradientStops.Add(new GradientStop(Color.Parse("#00000000"), 0.0));
        gradBrush.GradientStops.Add(new GradientStop(Color.Parse("#CC000000"), 1.0));
        grid.Children.Add(new Border { Background = gradBrush });
        grid.Children.Add(new TextBlock
        {
            Text = title, FontSize = 11, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8, 0, 8, 8),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        // Rank number for trending
        if (index < 8)
        {
            grid.Children.Add(new TextBlock
            {
                Text = (index + 1).ToString(), FontSize = 42, FontWeight = FontWeight.Black,
                Foreground = new SolidColorBrush(Color.Parse("#55FFFFFF")),
                VerticalAlignment   = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 8, 0),
            });
        }

        border.Child = grid;
        btn.Content  = new StackPanel { Children = { border } };
        return btn;
    }

    Control CreateGridPosterCard(string title, int index, string badge)
    {
        var btn = new Button
        {
            Background = Brushes.Transparent, Padding = new Thickness(4),
            Tag = title, Cursor = new Cursor(StandardCursorType.Hand),
            Width = 160,
        };
        btn.Click += OnMovieClick;

        var outer = new Grid();
        var border = new Border
        {
            Height = 200, CornerRadius = new CornerRadius(6), ClipToBounds = true,
        };
        var grid = new Grid();
        try
        {
            grid.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(MovieAssets[index % MovieAssets.Length]))),
                Stretch = Stretch.UniformToFill,
            });
        }
        catch { }

        if (!string.IsNullOrEmpty(badge))
        {
            grid.Children.Add(new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment   = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(6, 3),
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Child = new TextBlock
                {
                    Text = badge, FontSize = 9, FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E50914")),
                },
            });
        }

        border.Child = grid;
        outer.Children.Add(border);
        btn.Content = outer;
        return btn;
    }

    Control CreateContinueCard(string title, int index)
    {
        var btn = new Button
        {
            Background = Brushes.Transparent, Padding = new Thickness(0),
            Tag = title, Cursor = new Cursor(StandardCursorType.Hand),
        };
        btn.Click += OnMovieClick;

        var container = new StackPanel { Spacing = 4, Width = 200 };

        var imageBorder = new Border
        {
            Width = 200, Height = 112,
            CornerRadius = new CornerRadius(6), ClipToBounds = true,
        };
        var imageGrid = new Grid();
        try
        {
            imageGrid.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(MovieAssets[index % MovieAssets.Length]))),
                Stretch = Stretch.UniformToFill,
            });
        }
        catch { }
        imageGrid.Children.Add(new Border
        {
            Height = 3, VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Color.Parse("#333333")),
        });
        imageGrid.Children.Add(new Border
        {
            Height = 3, VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 200 * (0.25 + index * 0.15),
            Background = new SolidColorBrush(Color.Parse("#E50914")),
        });
        imageBorder.Child = imageGrid;

        container.Children.Add(imageBorder);
        container.Children.Add(new TextBlock
        {
            Text = title, FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
            MaxWidth = 200, TextTrimming = TextTrimming.CharacterEllipsis,
        });

        btn.Content = container;
        return btn;
    }

    // Cast item: 72px circle, #444 border, name + character
    Control CreateCastItem(int index)
    {
        var item = new StackPanel
        {
            Spacing = 5, Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var avatar = new Border
        {
            Width = 72, Height = 72,
            CornerRadius = new CornerRadius(36),
            ClipToBounds = true,
            BorderBrush     = new SolidColorBrush(Color.Parse("#444444")),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        try
        {
            avatar.Child = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(CastAssets[index % CastAssets.Length]))),
                Stretch = Stretch.UniformToFill,
            };
        }
        catch { }
        item.Children.Add(avatar);

        item.Children.Add(new TextBlock
        {
            Text = CastNames[index % CastNames.Length],
            FontSize = 11, FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextTrimming  = TextTrimming.CharacterEllipsis,
        });
        item.Children.Add(new TextBlock
        {
            Text = CastCharacters[index % CastCharacters.Length],
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#999999")),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        });

        return item;
    }

    // Search item — thumbnail + title + red circular play button
    Control CreateSearchItem(string title, int index)
    {
        var btn = new Button
        {
            Background = Brushes.Transparent, Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Tag = title, Cursor = new Cursor(StandardCursorType.Hand),
        };
        btn.Click += OnMovieClick;

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        var thumb = new Border { Width = 130, Height = 73, ClipToBounds = true };
        try
        {
            thumb.Child = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(MovieAssets[index % MovieAssets.Length]))),
                Stretch = Stretch.UniformToFill,
            };
        }
        catch { }
        Grid.SetColumn(thumb, 0);
        row.Children.Add(thumb);
        var titleText = new TextBlock
        {
            Text = title, FontSize = 14, FontWeight = FontWeight.Medium,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
        };
        Grid.SetColumn(titleText, 1);
        row.Children.Add(titleText);
        var playCircle = new Border
        {
            Width = 36, Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.Parse("#E50914")),
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        playCircle.Child = new PathIcon
        {
            Width = 16, Height = 16, Foreground = Brushes.White,
            Data = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z"),
        };
        Grid.SetColumn(playCircle, 2);
        row.Children.Add(playCircle);

        btn.Content = row;

        // Wrap with optional divider
        var wrapper = new StackPanel();
        if (index > 0)
        {
            wrapper.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.Parse("#222222")),
            });
        }
        wrapper.Children.Add(btn);
        return wrapper;
    }

    async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        if (_detailNav == null) return;
        await _detailNav.PushModalAsync(BuildSearchPage());
    }

    async void OnMovieClick(object? sender, RoutedEventArgs e)
    {
        string title = "Cyber Dune";
        if (sender is Button btn && btn.Tag is string tag)
            title = tag;

        // Dismiss search modal first if it's open
        if (_detailNav != null && _detailNav.ModalStack.Count > 0)
            await _detailNav.PopModalAsync();

        _detailNav?.Push(BuildDetailPage(title));

        // Close drawer if open
        var drawer = this.FindControl<DrawerPage>("DrawerPageControl");
        if (drawer is { IsOpen: true })
            drawer.IsOpen = false;
    }

    void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        var drawer = this.FindControl<DrawerPage>("DrawerPageControl");
        if (drawer != null)
            drawer.IsOpen = false;

        _ = _detailNav?.PopToRootAsync();
    }
}
