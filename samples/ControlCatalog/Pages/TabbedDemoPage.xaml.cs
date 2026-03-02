using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedDemoPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            // Overview
            ("Overview", "First Look",
                "Basic TabbedPage with three tabs, tab placement selector, and selection status.",
                () => new TabbedPageFirstLookPage()),

            // Populate
            ("Populate", "Page Collection",
                "Populate a TabbedPage by adding ContentPage objects directly to the Pages collection.",
                () => new TabbedPageCollectionPage()),
            ("Populate", "Data Templates",
                "Populate a TabbedPage with a data collection and a custom PageTemplate to render each item.",
                () => new TabbedPageDataTemplatePage()),

            // Appearance
            ("Appearance", "Tab Customization",
                "Customize tab placement, bar background, selected and unselected tab colors.",
                () => new TabbedPageCustomizationPage()),
            ("Appearance", "Custom Tab Bar",
                "VYNTRA-style custom tab bar with floating pill, brand colours, and system-adaptive theme using only resource overrides and styles.",
                () => new TabbedPageCustomTabBarPage()),
            ("Appearance", "FAB Tab Bar",
                "Social-media-style bottom nav with a central floating action button that triggers a command, not a tab.",
                () => new TabbedPageFabPage()),
            ("Appearance", "Fluid Nav Bar",
                "Inspired by the Flutter fluid_nav_bar vignette. Color themes with animated indicator and icons.",
                () => new TabbedPageFluidNavPage()),

            // Features
            ("Features", "Programmatic Selection",
                "Preset the initial tab with SelectedIndex, jump to any tab programmatically, and respond to SelectionChanged events.",
                () => new TabbedPageProgrammaticPage()),
            ("Features", "Placement", "Switch the tab bar between Top, Bottom, Left, and Right placements.",
                () => new TabbedPagePlacementPage()),
            ("Features", "Page Transitions",
                "Animate tab switches with CrossFade, PageSlide, or composite transitions.",
                () => new TabbedPageTransitionsPage()),
            ("Features", "Keyboard Navigation",
                "Keyboard shortcuts to navigate between tabs, with a toggle to enable or disable.",
                () => new TabbedPageKeyboardPage()),
            ("Features", "Swipe Gestures",
                "Swipe left/right (Top/Bottom) or up/down (Left/Right) to navigate. Toggle IsGestureEnabled.",
                () => new TabbedPageGesturePage()),
            ("Features", "Events",
                "SelectionChanged, Appearing, Disappearing, NavigatedTo, and NavigatedFrom events. Switch tabs to see the live event log.",
                () => new TabbedPageEventsPage()),
            ("Features", "Disabled Tabs",
                "IsTabEnabled attached property: disable individual tabs so they cannot be selected.",
                () => new TabbedPageDisabledTabsPage()),

            // Performance
            ("Performance", "Performance Monitor",
                "Track tab count, live page instances, and managed heap size. Observe how GC reclaims memory after removing tabs.",
                () => new TabbedPagePerformancePage()),

            // Composition
            ("Composition", "With NavigationPage",
                "Embed a NavigationPage inside each TabbedPage tab for drill-down navigation.",
                () => new TabbedPageWithNavigationPage()),
            ("Composition", "With DrawerPage",
                "Combine TabbedPage with DrawerPage: a global navigation drawer sits over tabbed content.",
                () => new TabbedPageWithDrawerPage()),

            // Showcases
            ("Showcases", "Pulse Fitness",
                "Fitness app with bottom TabbedPage navigation, NavigationPage drill-down inside tabs, and workout detail screens.",
                () => new PulseAppPage()),
            ("Showcases", "L'Avenir Restaurant",
                "Restaurant app with DrawerPage root, NavigationPage detail, and TabbedPage bottom tabs for Menu, Reservations, and Profile.",
                () => new LAvenirAppPage()),
            ("Showcases", "Retro Gaming",
                "Arcade-style app with NavigationPage header, TabbedPage bottom tabs with CenteredTabPanel, and game detail push.",
                () => new RetroGamingAppPage()),
        };

        public TabbedDemoPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await SampleNav.PushAsync(CreateHomePage(), null);
        }

        private ContentPage CreateHomePage()
        {
            var stack = new StackPanel
            {
                Margin = new Avalonia.Thickness(12),
                Spacing = 16
            };

            var groups = new Dictionary<string, WrapPanel>();
            var groupOrder = new List<string>();

            foreach (var (group, title, description, factory) in Demos)
            {
                if (!groups.ContainsKey(group))
                {
                    groups[group] = new WrapPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    groupOrder.Add(group);
                }

                var demoFactory = factory;
                var demoTitle = title;

                var card = new Button
                {
                    Width = 170,
                    MinHeight = 80,
                    Margin = new Avalonia.Thickness(0, 0, 8, 8),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    Padding = new Avalonia.Thickness(12, 8),
                    Content = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = title,
                                FontSize = 13,
                                FontWeight = FontWeight.SemiBold,
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = description,
                                FontSize = 11,
                                Opacity = 0.6,
                                TextWrapping = TextWrapping.Wrap
                            }
                        }
                    }
                };

                card.Click += async (s, e) =>
                {
                    var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
                    headerGrid.Children.Add(new TextBlock
                    {
                        Text = demoTitle,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    var closeBtn = new Button
                    {
                        Content = new PathIcon
                        {
                            Data = Geometry.Parse("M4.397 4.397a1 1 0 0 1 1.414 0L12 10.585l6.19-6.188a1 1 0 0 1 1.414 1.414L13.413 12l6.19 6.189a1 1 0 0 1-1.414 1.414L12 13.413l-6.189 6.19a1 1 0 0 1-1.414-1.414L10.585 12 4.397 5.811a1 1 0 0 1 0-1.414z")
                        },
                        Background = Brushes.Transparent,
                        BorderThickness = new Avalonia.Thickness(0),
                        Padding = new Avalonia.Thickness(8, 4),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(closeBtn, 1);
                    headerGrid.Children.Add(closeBtn);
                    closeBtn.Click += async (_, _) => await SampleNav.PopAsync(null);

                    var page = new ContentPage
                    {
                        Header = headerGrid,
                        Content = demoFactory(),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch
                    };
                    NavigationPage.SetHasBackButton(page, false);
                    await SampleNav.PushAsync(page, null);
                };

                groups[group].Children.Add(card);
            }

            foreach (var groupName in groupOrder)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = groupName,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Avalonia.Thickness(0, 0, 0, 4),
                    Opacity = 0.6
                });
                stack.Children.Add(groups[groupName]);
            }

            var homePage = new ContentPage
            {
                Content = new ScrollViewer { Content = stack },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            NavigationPage.SetHasNavigationBar(homePage, false);

            return homePage;
        }
    }
}
