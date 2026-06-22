using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages;

public partial class ControlsGalleryAppPage : UserControl
{
    static readonly Color Accent = Color.Parse("#60CDFF");
    static readonly Color ContentBg = Color.Parse("#141414");
    static readonly Color CardBg = Color.Parse("#1F1F1F");
    static readonly Color BorderCol = Color.Parse("#2EFFFFFF");
    static readonly Color TextCol = Color.Parse("#FFFFFF");
    static readonly Color TextSec = Color.Parse("#C8FFFFFF");
    static readonly Color TextMuted = Color.Parse("#80FFFFFF");

    DrawerPage? _drawer;
    NavigationPage? _detailNav;
    Button? _selectedBtn;
    TextBox? _searchBox;
    ContentPage? _preSearchPage;
    bool _isSearching;

    public ControlsGalleryAppPage()
    {
        InitializeComponent();

        _drawer = this.FindControl<DrawerPage>("NavDrawer");
        _detailNav = this.FindControl<NavigationPage>("DetailNav");
        _selectedBtn = this.FindControl<Button>("BtnWhatsNew");
        _searchBox = this.FindControl<TextBox>("SearchBox");

        if (_detailNav != null)
            _ = _detailNav.PushAsync(BuildWhatsNewPage());
    }

    private void OnHamburgerClick(object? sender, RoutedEventArgs e)
    {
        if (_drawer != null)
            _drawer.IsOpen = !_drawer.IsOpen;
    }

    private async void OnNavItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;
        if (btn == _selectedBtn) return;

        if (_selectedBtn != null)
            _selectedBtn.Classes.Remove("navItemSelected");
        _selectedBtn = btn;
        btn.Classes.Add("navItemSelected");

        if (_detailNav == null) return;

        var page = tag switch
        {
            "WhatsNew" => BuildWhatsNewPage(),
            "AllControls" => BuildAllControlsPage(),
            "BasicInput" => BuildCategoryPage("Basic Input",
                "Buttons, checkboxes, radio buttons, sliders, and toggle switches."),
            "Collections" => BuildCategoryPage("Collections",
                "List view, tree view, data grid, flip view, and more."),
            "Media" => BuildCategoryPage("Media",
                "Image, web view, map control, and media player."),
            "Menus" => BuildCategoryPage("Menus and Toolbars",
                "Menus, context menus, command bar, and toolbar."),
            "Navigation" => BuildCategoryPage("Navigation",
                "Navigation view, pivot, tab control, and breadcrumb bar."),
            "Text" => BuildCategoryPage("Text",
                "Text block, text box, auto-suggest box, and rich text."),
            "Settings" => BuildSettingsPage(),
            _ => BuildWhatsNewPage(),
        };

        NavigationPage.SetHasBackButton(page, false);
        await _detailNav.ReplaceAsync(page, new CrossFade(TimeSpan.FromMilliseconds(180)));
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_detailNav == null) return;
        var query = _searchBox?.Text?.Trim() ?? "";

        if (query.Length == 0)
        {
            if (_isSearching)
            {
                _isSearching = false;
                var restore = _preSearchPage ?? BuildWhatsNewPage();
                _preSearchPage = null;
                NavigationPage.SetHasBackButton(restore, false);
                _ = _detailNav.ReplaceAsync(restore, new CrossFade(TimeSpan.FromMilliseconds(180)));
            }
            return;
        }

        if (!_isSearching)
        {
            _preSearchPage = _detailNav.CurrentPage as ContentPage;
            _isSearching = true;
        }

        var resultsPage = BuildSearchResultsPage(query);
        NavigationPage.SetHasBackButton(resultsPage, false);
        _ = _detailNav.ReplaceAsync(resultsPage, null);
    }

    ContentPage BuildSearchResultsPage(string query)
    {
        var page = new ContentPage { Background = new SolidColorBrush(ContentBg) };
        page.Header = "Search";

        var scroll = new ScrollViewer();
        var root = new StackPanel { Spacing = 0 };

        (string Category, string[] Controls)[] allSections =
        {
            ("Basic Input",  new[] { "Button", "CheckBox", "ComboBox", "RadioButton", "Slider", "ToggleButton", "ToggleSwitch" }),
            ("Collections",  new[] { "DataGrid", "ItemsControl", "ListBox", "ListView", "TreeView" }),
            ("Date & Time",  new[] { "CalendarDatePicker", "DatePicker", "TimePicker" }),
            ("Layout",       new[] { "Border", "Grid", "Panel", "StackPanel", "WrapPanel" }),
            ("Navigation",   new[] { "DrawerPage", "NavigationPage", "TabControl", "TabbedPage" }),
            ("Text",         new[] { "AutoCompleteBox", "RichTextBox", "TextBlock", "TextBox" }),
        };

        var q = query.ToLowerInvariant();
        bool anyMatch = false;

        foreach (var (category, controls) in allSections)
        {
            var matches = Array.FindAll(controls, c => c.ToLowerInvariant().Contains(q));
            if (matches.Length == 0) continue;

            anyMatch = true;
            root.Children.Add(SectionHeader(category));
            var chips = new WrapPanel { Margin = new Thickness(24, 4, 24, 0), Orientation = Orientation.Horizontal };
            foreach (var ctrl in matches)
                chips.Children.Add(new Border
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(12, 6),
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(CardBg),
                    BorderBrush = new SolidColorBrush(BorderCol),
                    BorderThickness = new Thickness(1),
                    Child = Txt(ctrl, 12, FontWeight.Normal, TextCol),
                });
            root.Children.Add(chips);
        }

        if (!anyMatch)
        {
            var empty = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0),
                Spacing = 8,
            };
            empty.Children.Add(Txt("No results", 16, FontWeight.SemiBold, TextCol));
            empty.Children.Add(Txt($"No controls match \"{query}\"", 13, FontWeight.Normal, TextSec));
            root.Children.Add(empty);
        }

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    ContentPage BuildWhatsNewPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(ContentBg) };
        page.Header = "What's New";

        var scroll = new ScrollViewer();
        var root = new StackPanel { Spacing = 0 };

        var heroBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        heroBrush.GradientStops.Add(new GradientStop(Color.Parse("#003B6F"), 0));
        heroBrush.GradientStops.Add(new GradientStop(Color.Parse("#0078D4"), 0.5));
        heroBrush.GradientStops.Add(new GradientStop(Color.Parse("#60CDFF"), 1));

        var heroContent = new StackPanel
        {
            Margin = new Thickness(24), Spacing = 8, VerticalAlignment = VerticalAlignment.Bottom,
        };
        heroContent.Children.Add(Txt("NEW IN AVALONIA UI", 11, FontWeight.SemiBold, Accent));
        heroContent.Children.Add(Txt("Controls Gallery", 28, FontWeight.Bold, TextCol));
        heroContent.Children.Add(new TextBlock
        {
            Text = "Explore all controls, styles, and animations available in Avalonia.",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            TextWrapping = TextWrapping.Wrap,
        });

        root.Children.Add(new Border
        {
            Height = 200,
            Margin = new Thickness(24, 24, 24, 0),
            CornerRadius = new CornerRadius(8),
            Background = heroBrush,
            Child = heroContent,
        });

        root.Children.Add(SectionHeader("New Controls"));
        var wrap = new WrapPanel { Margin = new Thickness(24, 8, 24, 0), Orientation = Orientation.Horizontal, };

        (string Icon, string Title, string Desc)[] newControls =
        {
            ("M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5",
                "DrawerPage", "Master-detail navigation with compact icon rail"),
            ("M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z",
                "NavigationPage", "Push/pop stack with animated transitions"),
            ("M3 3h8v8H3zm10 0h8v8h-8zM3 13h8v8H3zm10 0h8v8h-8z",
                "TabbedPage", "Multi-tab layout with swipe navigation"),
            ("M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z",
                "ContentPage", "Single-content page with lifecycle events"),
        };
        foreach (var (icon, title, desc) in newControls)
            wrap.Children.Add(ControlCard(icon, title, desc));
        root.Children.Add(wrap);

        root.Children.Add(SectionHeader("Recently Updated"));
        var updList = new StackPanel { Margin = new Thickness(24, 8, 24, 24), Spacing = 4 };
        (string Name, string Change)[] updates =
        {
            ("CommandBar",  "Overflow menu and compact label mode"),
            ("ContentPage", "Lifecycle events and navigation bar customization"),
        };
        foreach (var (name, change) in updates)
        {
            var infoStack = new StackPanel { Spacing = 2 };
            infoStack.Children.Add(Txt(name, 13, FontWeight.SemiBold, TextCol));
            infoStack.Children.Add(Txt(change, 11, FontWeight.Normal, TextSec));
            updList.Children.Add(new Border
            {
                Padding = new Thickness(12, 10),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderCol),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = infoStack,
            });
        }

        root.Children.Add(updList);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    Border ControlCard(string iconData, string title, string desc)
    {
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(new PathIcon
        {
            Width = 20,
            Height = 20,
            Data = Geometry.Parse(iconData),
            Foreground = new SolidColorBrush(Accent),
            HorizontalAlignment = HorizontalAlignment.Left,
        });
        stack.Children.Add(Txt(title, 14, FontWeight.SemiBold, TextCol));
        stack.Children.Add(new TextBlock
        {
            Text = desc, FontSize = 11, Foreground = new SolidColorBrush(TextSec), TextWrapping = TextWrapping.Wrap,
        });

        return new Border
        {
            Width = 182,
            Height = 130,
            Margin = new Thickness(0, 0, 12, 12),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(CardBg),
            BorderBrush = new SolidColorBrush(BorderCol),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(16),
            Child = stack,
        };
    }

    ContentPage BuildAllControlsPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(ContentBg) };
        page.Header = "All Controls";

        var scroll = new ScrollViewer();
        var root = new StackPanel { Spacing = 0 };

        (string Category, string[] Controls)[] sections =
        {
            ("Basic Input",
                new[]
                {
                    "Button", "CheckBox", "ComboBox", "RadioButton", "Slider", "ToggleButton", "ToggleSwitch"
                }),
            ("Collections", new[] { "DataGrid", "ItemsControl", "ListBox", "ListView", "TreeView" }),
            ("Date & Time", new[] { "CalendarDatePicker", "DatePicker", "TimePicker" }),
            ("Layout", new[] { "Border", "Grid", "Panel", "StackPanel", "WrapPanel" }),
            ("Navigation", new[] { "DrawerPage", "NavigationPage", "TabControl", "TabbedPage" }),
            ("Text", new[] { "AutoCompleteBox", "RichTextBox", "TextBlock", "TextBox" }),
        };

        foreach (var (category, controls) in sections)
        {
            root.Children.Add(SectionHeader(category));
            var chips = new WrapPanel { Margin = new Thickness(24, 4, 24, 0), Orientation = Orientation.Horizontal };
            foreach (var ctrl in controls)
            {
                chips.Children.Add(new Border
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(12, 6),
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(CardBg),
                    BorderBrush = new SolidColorBrush(BorderCol),
                    BorderThickness = new Thickness(1),
                    Child = Txt(ctrl, 12, FontWeight.Normal, TextCol),
                });
            }

            root.Children.Add(chips);
        }

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    ContentPage BuildCategoryPage(string title, string description)
    {
        var page = new ContentPage { Background = new SolidColorBrush(ContentBg) };
        page.Header = title;

        var scroll = new ScrollViewer();
        var root = new StackPanel { Spacing = 0 };

        var headerStack = new StackPanel { Spacing = 4 };
        headerStack.Children.Add(Txt(title, 20, FontWeight.SemiBold, TextCol));
        headerStack.Children.Add(Txt(description, 13, FontWeight.Normal, TextSec));
        root.Children.Add(new Border
        {
            Margin = new Thickness(24, 24, 24, 0),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(CardBg),
            Child = headerStack,
        });

        root.Children.Add(SectionHeader("Controls"));
        var list = new StackPanel { Margin = new Thickness(24, 4, 24, 24), Spacing = 4 };
        string[] sampleNames = { "Primary Control", "Secondary Control", "Advanced Control", "Variant A", "Variant B" };
        for (int i = 0; i < sampleNames.Length; i++)
        {
            var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var label = new StackPanel { Spacing = 2 };
            label.Children.Add(Txt(sampleNames[i], 13, FontWeight.SemiBold, TextCol));
            label.Children.Add(Txt($"Example usage in {title}", 11, FontWeight.Normal, TextSec));
            rowGrid.Children.Add(label);

            if (i < 2)
            {
                var badge = new Border
                {
                    Padding = new Thickness(6, 2),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromArgb(30, 96, 205, 255)),
                    Child = Txt("NEW", 10, FontWeight.Bold, Accent),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(badge, 1);
                rowGrid.Children.Add(badge);
            }

            list.Children.Add(new Border
            {
                Padding = new Thickness(16, 12),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderCol),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = rowGrid,
            });
        }

        root.Children.Add(list);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    ContentPage BuildSettingsPage()
    {
        var page = new ContentPage { Background = new SolidColorBrush(ContentBg) };
        page.Header = "Settings";

        var scroll = new ScrollViewer();
        var root = new StackPanel { Spacing = 0 };

        root.Children.Add(SectionHeader("Appearance"));
        var appearList = new StackPanel { Margin = new Thickness(24, 4, 24, 0), Spacing = 2 };
        appearList.Children.Add(SettingsRow("App theme", "Dark"));
        appearList.Children.Add(SettingsRow("Accent color", "#60CDFF"));
        appearList.Children.Add(SettingsRow("Font size", "Medium"));
        root.Children.Add(appearList);

        root.Children.Add(SectionHeader("About"));
        var aboutList = new StackPanel { Margin = new Thickness(24, 4, 24, 24), Spacing = 2 };
        aboutList.Children.Add(SettingsRow("Version", "1.0.0"));
        aboutList.Children.Add(SettingsRow("Framework", "Avalonia"));
        aboutList.Children.Add(SettingsRow("Theme", "Fluent"));
        root.Children.Add(aboutList);

        scroll.Content = root;
        page.Content = scroll;
        return page;
    }

    Border SectionHeader(string title) => new()
    {
        Margin = new Thickness(24, 20, 24, 0),
        Padding = new Thickness(0, 0, 0, 8),
        BorderBrush = new SolidColorBrush(BorderCol),
        BorderThickness = new Thickness(0, 0, 0, 1),
        Child = Txt(title, 13, FontWeight.SemiBold, Accent),
    };

    static Border SettingsRow(string label, string value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(Txt(label, 13, FontWeight.Normal, TextCol));
        var val = Txt(value, 12, FontWeight.Normal, TextMuted);
        val.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);

        return new Border
        {
            Padding = new Thickness(16, 14),
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(CardBg),
            BorderBrush = new SolidColorBrush(BorderCol),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
        };
    }

    static TextBlock Txt(string text, double size, FontWeight weight, Color color) => new()
    {
        Text = text, FontSize = size, FontWeight = weight, Foreground = new SolidColorBrush(color),
    };
}
