using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace ControlCatalog.Pages;

public partial class LAvenirAppPage : UserControl
{
    // Design tokens
    static readonly Color Primary = Color.Parse("#4b2bee");
    static readonly Color BgDark = Color.Parse("#131022");
    static readonly Color BgLight = Color.Parse("#f6f6f8");
    static readonly Color Surface = Color.Parse("#1a1836");
    static readonly Color TextDark = Color.Parse("#1e293b");
    static readonly Color TextWhite = Colors.White;
    static readonly Color TextMuted = Color.Parse("#94a3b8");
    static readonly Color BorderLight = Color.Parse("#e2e8f0");

    NavigationPage? _navPage;
    DrawerPage? _drawerPage;
    bool _initialized;
    ScrollViewer? _infoPanel;

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

        _navPage = this.FindControl<NavigationPage>("NavPage");
        _drawerPage = this.FindControl<DrawerPage>("DrawerPageControl");

        if (_navPage == null) return;

        var tabbedPage = BuildMenuTabbedPage();
        _navPage.Push(tabbedPage);
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
            var uri = new Uri($"avares://ControlCatalog/Assets/Restaurant/{fileName}");
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

        var pointerOver = new Style(x => x.OfType<Button>().Class(":pointerover").Descendant().OfType<ContentPresenter>());
        pointerOver.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, new SolidColorBrush(Color.Parse("#3d22cc"))));
        pointerOver.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, Brushes.White));
        btn.Styles.Add(pointerOver);

        var pressed = new Style(x => x.OfType<Button>().Class(":pressed").Descendant().OfType<ContentPresenter>());
        pressed.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, new SolidColorBrush(Color.Parse("#3518b0"))));
        pressed.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, Brushes.White));
        btn.Styles.Add(pressed);

        return btn;
    }

    static Border Chip(string text, bool active)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(16, 8),
            Background = new SolidColorBrush(active ? Primary : Colors.Transparent),
            BorderThickness = new Thickness(active ? 0 : 1),
            BorderBrush = active ? null : new SolidColorBrush(BorderLight),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = active ? FontWeight.SemiBold : FontWeight.Medium,
                Foreground = new SolidColorBrush(active ? TextWhite : TextMuted),
            },
        };
    }

    Button GhostButton(object content, double width, double height, CornerRadius cornerRadius, IBrush background, IBrush foreground, IBrush? borderBrush = null)
    {
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

        var pointerOver = new Style(x => x.OfType<Button>().Class(":pointerover").Descendant().OfType<ContentPresenter>());
        pointerOver.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, background));
        pointerOver.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, foreground));
        btn.Styles.Add(pointerOver);

        var pressed = new Style(x => x.OfType<Button>().Class(":pressed").Descendant().OfType<ContentPresenter>());
        pressed.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, background));
        pressed.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, foreground));
        btn.Styles.Add(pressed);

        return btn;
    }


    TabbedPage BuildMenuTabbedPage()
    {
        var tp = new TabbedPage
        {
            Background = new SolidColorBrush(BgLight),
            BarBackground = new SolidColorBrush(Colors.White),
            SelectedTabBrush = new SolidColorBrush(Primary),
            UnselectedTabBrush = new SolidColorBrush(TextMuted),
            TabPlacement = TabPlacement.Bottom,
            PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200)),
        };
        tp.Resources.Add("TabItemHeaderFontSize", 12.0);
        // Tab strip top separator
        tp.Resources["TabbedPageTabStripBorderThickness"] = new Thickness(0, 1, 0, 0);
        tp.Resources["TabbedPageTabStripBorderBrush"] = new SolidColorBrush(BorderLight);
        tp.IndicatorTemplate = new FuncDataTemplate<object>((_, _) =>
            new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 5, Height = 5,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Fill = new SolidColorBrush(Primary),
            });
        tp.Header = new TextBlock
        {
            Text = "L'Avenir",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextDark),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        };
        NavigationPage.SetTopCommandBar(tp, new Button
        {
            Width = 40, Height = 40,
            CornerRadius = new CornerRadius(12),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(TextDark),
            Padding = new Thickness(8),
            BorderThickness = new Thickness(0),
            Content = new PathIcon
            {
                Data = Geometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
                Width = 18, Height = 18,
            },
            VerticalAlignment = VerticalAlignment.Center,
        });

        var menuPage = BuildMenuPage();
        menuPage.Header = "Menu";
        menuPage.Icon = "M11 9H9V2H7v7H5V2H3v7c0 2.12 1.66 3.84 3.75 3.97V22h2.5v-9.03C11.34 12.84 13 11.12 13 9V2h-2v7zm5-3v8h2.5v8H21V2c-2.76 0-5 2.24-5 4z";

        var reservationsPage = BuildReservationsPage();
        reservationsPage.Header = "Reservations";
        reservationsPage.Icon = "M19 3h-1V1h-2v2H8V1H6v2H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11zM9 10H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2z";

        var profilePage = BuildProfilePage();
        profilePage.Header = "Profile";
        profilePage.Icon = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z";

        tp.Pages = new ObservableCollection<object?> { menuPage, reservationsPage, profilePage };

        return tp;
    }


    ContentPage BuildMenuPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
        };

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var greetCol = new StackPanel { Margin = new Thickness(16, 20, 16, 8) };
        var welcomeLabel = Label("GOOD EVENING", 10, FontWeight.Medium, TextMuted);
        welcomeLabel.LetterSpacing = 2;
        greetCol.Children.Add(welcomeLabel);
        greetCol.Children.Add(Label("Alexander", 22, FontWeight.Bold, TextDark));
        root.Children.Add(greetCol);

        var heroCard = new Border
        {
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
            Height = 200,
            Margin = new Thickness(16, 8, 16, 16),
        };
        var heroPanel = new Panel();

        var heroBg = ImageBorder("featured_dish.jpg", double.NaN, double.NaN, 0);
        heroBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        heroBg.VerticalAlignment = VerticalAlignment.Stretch;
        heroBg.Width = double.NaN;
        heroBg.Height = double.NaN;
        heroPanel.Children.Add(heroBg);

        heroPanel.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(180, 0, 0, 0), 1),
                },
            },
        });

        var heroText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 16, 16),
        };
        heroText.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(999),
            Background = new SolidColorBrush(Primary),
            Padding = new Thickness(10, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8),
            Child = Label("CHEF'S SPECIAL", 10, FontWeight.Bold, TextWhite),
        });
        heroText.Children.Add(Label("Seared Scallops", 22, FontWeight.Bold, TextWhite));
        var heroDesc = Label("Fresh scallops with truffle butter and microgreens", 12, FontWeight.Normal, Color.Parse("#e2e8f0"));
        heroDesc.TextWrapping = TextWrapping.Wrap;
        heroText.Children.Add(heroDesc);
        heroPanel.Children.Add(heroText);
        heroCard.Child = heroPanel;
        root.Children.Add(heroCard);

        var chipsScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, 16),
        };
        var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(16, 0) };
        chips.Children.Add(Chip("Starters", true));
        chips.Children.Add(Chip("Mains", false));
        chips.Children.Add(Chip("Desserts", false));
        chips.Children.Add(Chip("Wines", false));
        chips.Children.Add(Chip("Cocktails", false));
        chipsScroll.Content = chips;
        root.Children.Add(chipsScroll);

        var featuredHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 0, 16, 12),
        };
        featuredHeader.Children.Add(Label("Featured Starters", 18, FontWeight.Bold, TextDark));
        var seeAll = Label("See All", 13, FontWeight.SemiBold, Primary);
        Grid.SetColumn(seeAll, 1);
        seeAll.VerticalAlignment = VerticalAlignment.Center;
        featuredHeader.Children.Add(seeAll);
        root.Children.Add(featuredHeader);

        root.Children.Add(BuildMenuItemCard("Seared Scallops", "Fresh scallops with truffle butter and microgreens", "$38", "dish1.jpg"));
        root.Children.Add(BuildMenuItemCard("Truffle Risotto", "Creamy arborio rice with black truffle shavings", "$34", "dish2.jpg"));
        root.Children.Add(BuildMenuItemCard("Wagyu Tartare", "Hand-cut wagyu beef with quail egg yolk", "$42", "dish3.jpg"));
        root.Children.Add(BuildMenuItemCard("Lobster Bisque", "Classic French bisque with cream and cognac", "$24", "dish4.jpg"));

        root.Children.Add(new Border { Height = 16 });

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    Border BuildMenuItemCard(string name, string description, string price, string imageFile)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(12),
            Margin = new Thickness(16, 0, 16, 12),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };
        card.PointerPressed += (_, _) => PushDishDetail(name, price, description, imageFile);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        var thumb = ImageBorder(imageFile, 72, 72, 10);
        thumb.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(thumb);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0), Spacing = 2 };
        info.Children.Add(Label(name, 15, FontWeight.SemiBold, TextDark));
        var descLabel = Label(description, 11, FontWeight.Normal, TextMuted);
        descLabel.TextWrapping = TextWrapping.Wrap;
        descLabel.MaxWidth = 160;
        info.Children.Add(descLabel);
        info.Children.Add(Label(price, 16, FontWeight.Bold, Primary));
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        var addBtn = new Border
        {
            Width = 36, Height = 36,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PathIcon
            {
                Data = Geometry.Parse("M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"),
                Width = 18, Height = 18,
                Foreground = new SolidColorBrush(Primary),
            },
        };
        Grid.SetColumn(addBtn, 2);
        grid.Children.Add(addBtn);

        card.Child = grid;
        return card;
    }


    void PushDishDetail(string name, string price, string description, string imageFile)
    {
        if (_navPage == null) return;
        var detail = BuildDishDetailPage(name, price, description, imageFile);
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

    ContentPage BuildDishDetailPage(string name, string price, string description, string imageFile)
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgDark),
        };
        page.Header = name;

        var root = new Panel();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var content = new StackPanel { Spacing = 0 };

        var hero = new Panel { Height = 260 };
        var heroBg = ImageBorder(imageFile, double.NaN, double.NaN, 0);
        heroBg.Width = double.NaN;
        heroBg.Height = double.NaN;
        heroBg.HorizontalAlignment = HorizontalAlignment.Stretch;
        heroBg.VerticalAlignment = VerticalAlignment.Stretch;
        hero.Children.Add(heroBg);

        hero.Children.Add(new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(77, BgDark.R, BgDark.G, BgDark.B), 0),
                    new GradientStop(Color.FromArgb(230, BgDark.R, BgDark.G, BgDark.B), 1),
                },
            },
        });

        content.Children.Add(hero);

        var titleLabel = Label(name, 26, FontWeight.Bold, TextWhite);
        titleLabel.Margin = new Thickness(16, 16, 16, 0);
        content.Children.Add(titleLabel);

        var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(16, 8, 16, 0) };
        metaRow.Children.Add(Label(price, 20, FontWeight.Bold, Primary));
        metaRow.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(999),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = Label("Main Course", 11, FontWeight.Medium, Color.Parse("#a78bfa")),
        });
        content.Children.Add(metaRow);

        var badges = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(16, 12, 16, 0) };
        badges.Children.Add(BuildDietaryBadge("Vegetarian"));
        badges.Children.Add(BuildDietaryBadge("Gluten-Free"));
        content.Children.Add(badges);

        var aboutHeader = Label("About This Dish", 16, FontWeight.SemiBold, TextWhite);
        aboutHeader.Margin = new Thickness(16, 20, 16, 8);
        content.Children.Add(aboutHeader);
        var descText = new TextBlock
        {
            Text = description.Length > 20 ? description :
                "A masterfully crafted dish featuring the finest seasonal ingredients, prepared with classical French technique and contemporary presentation. Each element is carefully balanced to create a harmonious dining experience.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new SolidColorBrush(TextMuted),
            LineHeight = 20,
            Margin = new Thickness(16, 0),
        };
        content.Children.Add(descText);

        var ingredientsHeader = Label("Key Ingredients", 16, FontWeight.SemiBold, TextWhite);
        ingredientsHeader.Margin = new Thickness(16, 20, 16, 12);
        content.Children.Add(ingredientsHeader);

        string[] ingredients = { "Black Truffle", "Arborio Rice", "Parmigiano", "White Wine", "Shallots", "Butter" };
        var ingredientGrid = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(16, 0),
        };
        foreach (var ing in ingredients)
        {
            ingredientGrid.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Surface),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Child = Label(ing, 12, FontWeight.Medium, TextMuted),
            });
        }
        content.Children.Add(ingredientGrid);

        var reviewsHeader = Label("Reviews", 16, FontWeight.SemiBold, TextWhite);
        reviewsHeader.Margin = new Thickness(16, 20, 16, 12);
        content.Children.Add(reviewsHeader);

        var reviewCard = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Surface),
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0),
        };
        var reviewContent = new StackPanel { Spacing = 8 };

        var ratingRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var stars = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        for (int i = 0; i < 5; i++)
        {
            stars.Children.Add(new PathIcon
            {
                Data = Geometry.Parse("M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"),
                Width = 14, Height = 14,
                Foreground = new SolidColorBrush(i < 5 ? Color.Parse("#fbbf24") : TextMuted),
            });
        }
        ratingRow.Children.Add(stars);
        ratingRow.Children.Add(Label("4.8", 16, FontWeight.Bold, TextWhite));
        ratingRow.Children.Add(Label("(124 reviews)", 12, FontWeight.Normal, TextMuted));
        reviewContent.Children.Add(ratingRow);

        reviewContent.Children.Add(new TextBlock
        {
            Text = "\"Absolutely divine! The truffle flavor was perfectly balanced and the presentation was stunning.\"",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(TextMuted),
        });
        var reviewerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        reviewerRow.Children.Add(new Border
        {
            Width = 24, Height = 24, CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Primary),
            Child = new TextBlock
            {
                Text = "M", FontSize = 10, FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });
        reviewerRow.Children.Add(Label("Marie L.", 12, FontWeight.SemiBold, TextWhite));
        reviewContent.Children.Add(reviewerRow);
        reviewCard.Child = reviewContent;
        content.Children.Add(reviewCard);

        scroll.Content = content;
        root.Children.Add(scroll);

        var floatingBar = new Border
        {
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromArgb(178, BgDark.R, BgDark.G, BgDark.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(16, 8, 16, 8),
        };
        var barGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var barInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        barInfo.Children.Add(Label("Add to Order", 14, FontWeight.Bold, TextWhite));
        barInfo.Children.Add(Label(price, 12, FontWeight.Medium, TextMuted));
        barGrid.Children.Add(barInfo);

        var orderBtn = new Button
        {
            Content = "Add",
            Width = 80,
            Height = 40,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Primary),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 14,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        var orderPointerOver = new Style(x => x.OfType<Button>().Class(":pointerover").Descendant().OfType<ContentPresenter>());
        orderPointerOver.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, new SolidColorBrush(Color.Parse("#3d22cc"))));
        orderBtn.Styles.Add(orderPointerOver);
        Grid.SetColumn(orderBtn, 1);
        barGrid.Children.Add(orderBtn);
        floatingBar.Child = barGrid;

        page.Content = root;
        NavigationPage.SetBottomCommandBar(page, floatingBar);
        return page;
    }

    static Border BuildDietaryBadge(string text)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(26, 16, 185, 129)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(51, 16, 185, 129)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 4),
            Child = Label(text, 11, FontWeight.Medium, Color.Parse("#34d399")),
        };
    }


    ContentPage BuildReservationsPage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
        };

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Spacing = 0 };

        var reserveTitle = Label("Reserve a Table", 24, FontWeight.Bold, TextDark);
        reserveTitle.Margin = new Thickness(16, 20, 16, 4);
        root.Children.Add(reserveTitle);
        var reserveSubtitle = Label("Select your preferred date and time", 13, FontWeight.Normal, TextMuted);
        reserveSubtitle.Margin = new Thickness(16, 0, 16, 16);
        root.Children.Add(reserveSubtitle);

        var calCard = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(8),
            Margin = new Thickness(16, 0, 16, 16),
        };

        var calendar = new Calendar
        {
            SelectionMode = CalendarSelectionMode.SingleDate,
            DisplayDate = new DateTime(2023, 10, 1),
            SelectedDate = new DateTime(2023, 10, 15),
            FirstDayOfWeek = DayOfWeek.Monday,
            IsTodayHighlighted = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        // Scoped calendar styles
        var primaryBrush = new SolidColorBrush(Primary);
        var textDarkBrush = new SolidColorBrush(TextDark);
        var textMutedBrush = new SolidColorBrush(TextMuted);
        var pastDayBrush = new SolidColorBrush(Color.Parse("#cbd5e1"));

        // CalendarDayButton base: transparent bg, rounded corners, dark text
        calendar.Styles.Add(new Style(x => x.OfType<CalendarDayButton>())
        {
            Setters =
            {
                new Setter(CalendarDayButton.BackgroundProperty, Brushes.Transparent),
                new Setter(CalendarDayButton.ForegroundProperty, textDarkBrush),
                new Setter(CalendarDayButton.FontSizeProperty, 13d),
                new Setter(CalendarDayButton.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(CalendarDayButton.MinWidthProperty, 32d),
                new Setter(CalendarDayButton.MinHeightProperty, 32d),
                new Setter(CalendarDayButton.MarginProperty, new Thickness(2)),
                new Setter(CalendarDayButton.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                new Setter(CalendarDayButton.VerticalContentAlignmentProperty, VerticalAlignment.Center),
            }
        });

        // Selected day: purple bg, white bold text
        calendar.Styles.Add(new Style(x => x.OfType<CalendarDayButton>().Class(":selected"))
        {
            Setters =
            {
                new Setter(CalendarDayButton.BackgroundProperty, primaryBrush),
                new Setter(CalendarDayButton.ForegroundProperty, Brushes.White),
                new Setter(CalendarDayButton.FontWeightProperty, FontWeight.Bold),
            }
        });

        // Inactive (outside current month): muted text
        calendar.Styles.Add(new Style(x => x.OfType<CalendarDayButton>().Class(":inactive"))
        {
            Setters =
            {
                new Setter(CalendarDayButton.ForegroundProperty, pastDayBrush),
            }
        });

        // CalendarItem (the month container): white bg, no border
        calendar.Styles.Add(new Style(x => x.OfType<CalendarItem>())
        {
            Setters =
            {
                new Setter(CalendarItem.BackgroundProperty, Brushes.White),
                new Setter(CalendarItem.BorderThicknessProperty, new Thickness(0)),
            }
        });

        // Calendar header button: dark text, semi-bold
        calendar.Styles.Add(new Style(x => x.OfType<CalendarItem>().Descendant().OfType<Button>())
        {
            Setters =
            {
                new Setter(Button.ForegroundProperty, textDarkBrush),
                new Setter(Button.FontWeightProperty, FontWeight.SemiBold),
                new Setter(Button.FontSizeProperty, 16d),
            }
        });

        // Calendar root: no border
        calendar.Styles.Add(new Style(x => x.OfType<Calendar>())
        {
            Setters =
            {
                new Setter(Calendar.BorderThicknessProperty, new Thickness(0)),
                new Setter(Calendar.BackgroundProperty, Brushes.White),
            }
        });

        // CalendarButton (month/year view buttons): rounded, dark text
        calendar.Styles.Add(new Style(x => x.OfType<CalendarButton>())
        {
            Setters =
            {
                new Setter(CalendarButton.ForegroundProperty, textDarkBrush),
                new Setter(CalendarButton.CornerRadiusProperty, new CornerRadius(8)),
            }
        });

        calCard.Child = calendar;
        root.Children.Add(calCard);

        var partyCard = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0, 16, 16),
        };
        var partyGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var partyInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        partyInfo.Children.Add(Label("Party Size", 15, FontWeight.SemiBold, TextDark));
        partyInfo.Children.Add(Label("2 Guests \u2022 Standard Seating", 12, FontWeight.Normal, TextMuted));
        partyGrid.Children.Add(partyInfo);

        var sizeControls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
        sizeControls.Children.Add(new Border
        {
            Width = 32, Height = 32, CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(BorderLight),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = "\u2212", FontSize = 16, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(TextMuted),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });
        sizeControls.Children.Add(Label("2", 18, FontWeight.Bold, TextDark));
        sizeControls.Children.Add(new Border
        {
            Width = 32, Height = 32, CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Primary),
            Child = new TextBlock
            {
                Text = "+", FontSize = 16, FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });
        Grid.SetColumn(sizeControls, 1);
        partyGrid.Children.Add(sizeControls);
        partyCard.Child = partyGrid;
        root.Children.Add(partyCard);

        var timesHeader = Label("Available Times", 16, FontWeight.SemiBold, TextDark);
        timesHeader.Margin = new Thickness(16, 0, 16, 12);
        root.Children.Add(timesHeader);

        var timesScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, 16),
        };
        var timesRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(16, 0) };
        string[] times = { "5:00 PM", "5:30 PM", "6:00 PM", "6:30 PM", "7:00 PM", "7:30 PM", "8:00 PM", "8:30 PM", "9:00 PM" };
        int selectedTimeIdx = 3; // 6:30 PM
        for (int i = 0; i < times.Length; i++)
        {
            bool isSelected = i == selectedTimeIdx;
            timesRow.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = isSelected ? new SolidColorBrush(Primary) : Brushes.White,
                BorderBrush = isSelected ? null : new SolidColorBrush(BorderLight),
                BorderThickness = new Thickness(isSelected ? 0 : 1),
                Padding = new Thickness(16, 10),
                Child = new TextBlock
                {
                    Text = times[i],
                    FontSize = 13,
                    FontWeight = isSelected ? FontWeight.Bold : FontWeight.Medium,
                    Foreground = new SolidColorBrush(isSelected ? TextWhite : TextDark),
                },
            });
        }
        timesScroll.Content = timesRow;
        root.Children.Add(timesScroll);

        var summaryCard = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            Padding = new Thickness(16),
            Margin = new Thickness(16, 0, 16, 16),
        };
        var summaryStack = new StackPanel { Spacing = 8 };
        summaryStack.Children.Add(Label("Reservation Summary", 15, FontWeight.SemiBold, TextDark));

        var summaryGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 4, 0, 0) };
        summaryGrid.Children.Add(Label("Date", 13, FontWeight.Normal, TextMuted));
        var dateVal = Label("October 15, 2023", 13, FontWeight.Medium, TextDark);
        Grid.SetColumn(dateVal, 1);
        summaryGrid.Children.Add(dateVal);
        summaryStack.Children.Add(summaryGrid);

        var timeGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        timeGrid.Children.Add(Label("Time", 13, FontWeight.Normal, TextMuted));
        var timeVal = Label("6:30 PM", 13, FontWeight.Medium, TextDark);
        Grid.SetColumn(timeVal, 1);
        timeGrid.Children.Add(timeVal);
        summaryStack.Children.Add(timeGrid);

        var guestGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        guestGrid.Children.Add(Label("Guests", 13, FontWeight.Normal, TextMuted));
        var guestVal = Label("2", 13, FontWeight.Medium, TextDark);
        Grid.SetColumn(guestVal, 1);
        guestGrid.Children.Add(guestVal);
        summaryStack.Children.Add(guestGrid);

        summaryStack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(BorderLight), Margin = new Thickness(0, 4) });

        var depositGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        depositGrid.Children.Add(Label("Deposit", 14, FontWeight.SemiBold, TextDark));
        var depositVal = Label("$50.00", 14, FontWeight.Bold, Primary);
        Grid.SetColumn(depositVal, 1);
        depositGrid.Children.Add(depositVal);
        summaryStack.Children.Add(depositGrid);

        summaryCard.Child = summaryStack;
        root.Children.Add(summaryCard);

        var confirmBtn = PrimaryButton("Confirm Reservation", 48, 14);
        confirmBtn.Margin = new Thickness(16, 0, 16, 24);
        root.Children.Add(confirmBtn);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }


    ContentPage BuildProfilePage()
    {
        var page = new ContentPage
        {
            Background = new SolidColorBrush(BgLight),
        };

        var root = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
        };

        root.Children.Add(new Border
        {
            Width = 72, Height = 72,
            CornerRadius = new CornerRadius(36),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = "AK",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Primary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });

        var nameLabel = Label("Alexander Koch", 20, FontWeight.Bold, TextDark);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        root.Children.Add(nameLabel);

        var emailLabel = Label("alexander@lavenir.com", 13, FontWeight.Normal, TextMuted);
        emailLabel.HorizontalAlignment = HorizontalAlignment.Center;
        root.Children.Add(emailLabel);

        root.Children.Add(new Border { Height = 8 });

        var memberBadge = new Border
        {
            CornerRadius = new CornerRadius(999),
            Background = new SolidColorBrush(Color.FromArgb(26, Primary.R, Primary.G, Primary.B)),
            Padding = new Thickness(16, 8),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = Label("Gold Member", 12, FontWeight.SemiBold, Primary),
        };
        root.Children.Add(memberBadge);

        page.Content = root;
        return page;
    }


    private void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string) return;

        if (_drawerPage != null)
            _drawerPage.IsOpen = false;

        _ = _navPage?.PopToRootAsync();
    }
}
