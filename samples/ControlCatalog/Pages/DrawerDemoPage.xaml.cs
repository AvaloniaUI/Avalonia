using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class DrawerDemoPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            // Overview
            ("Overview", "First Look", "Basic DrawerPage with a navigation drawer, menu items, and detail content.",
                () => new DrawerPageFirstLookPage()),

            // Features
            ("Features", "Navigation",
                "Master-detail pattern: select a drawer menu item to navigate the detail via a NavigationPage with hamburger-to-back-button transition.",
                () => new DrawerPageNavigationPage()),
            ("Features", "Compact Rail",
                "CompactOverlay and CompactInline layout modes: a narrow icon rail is always visible and expands on open. Adjust rail width and open pane width.",
                () => new DrawerPageCompactPage()),
            ("Features", "Events",
                "Opened, Closing, and Closed drawer events plus Appearing and Disappearing page lifecycle events. Enable 'Cancel next close' to prevent the drawer from closing.",
                () => new DrawerPageEventsPage()),
            ("Features", "RTL Layout", "Right-to-left layout: drawer opens from the right edge with mirrored gestures.",
                () => new DrawerPageRtlPage()),

            // Appearance
            ("Appearance", "Customization",
                "Customize drawer behavior, layout mode, length, colors, header and footer.",
                () => new DrawerPageCustomizationPage()),
            ("Appearance", "Custom Flyout",
                "Dark overlay menu with staggered item animations and CrossFade page transitions on the detail NavigationPage.",
                () => new DrawerPageCustomFlyoutPage()),
            ("Appearance", "Transitions",
                "Configure the detail NavigationPage transition. Choose CrossFade, PageSlide, or CompositePageTransition to animate detail page changes.",
                () => new DrawerPageTransitionsPage()),

            // Performance
            ("Performance", "Performance Monitor",
                "Track detail page swaps, live page instances, and managed heap size. Observe how GC reclaims memory after swapping pages.",
                () => new DrawerPagePerformancePage()),

            // Showcases
            ("Showcases", "AvaloniaFlix",
                "Streaming app with DrawerPage wrapping NavigationPage. Hamburger auto-injected at root, back arrow on detail, and dark themed flyout menu.",
                () => new AvaloniaFlixAppPage()),
            ("Showcases", "L'Avenir Restaurant",
                "Restaurant app with DrawerPage as the root container, NavigationPage for detail navigation, and TabbedPage bottom tabs for Menu, Reservations, and Profile.",
                () => new LAvenirAppPage()),
            ("Showcases", "EcoTracker",
                "Sustainability tracker with CompactInline drawer, eco leaf hamburger icon, crossfade compact/open menu transitions, and green-themed Home, Stats, Habits, and Community pages.",
                () => new EcoTrackerAppPage()),
            ("Showcases", "ModernApp",
                "Travel social app using a top-placement DrawerPage. A slide-down nav pane gives access to Discover, My Trips, Profile, and Settings. Features destination cards, story circles, an experience feed, a stats profile, and a travel gallery.",
                () => new ModernAppPage()),
        };

        public DrawerDemoPage()
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
            var stack = new StackPanel { Margin = new Avalonia.Thickness(12), Spacing = 16 };

            var groups = new Dictionary<string, WrapPanel>();
            var groupOrder = new List<string>();

            foreach (var (group, title, description, factory) in Demos)
            {
                if (!groups.ContainsKey(group))
                {
                    groups[group] = new WrapPanel
                    {
                        Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left
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
                        Text = demoTitle, VerticalAlignment = VerticalAlignment.Center
                    });
                    var closeBtn = new Button
                    {
                        Content = new PathIcon
                        {
                            Data = Geometry.Parse(
                                "M4.397 4.397a1 1 0 0 1 1.414 0L12 10.585l6.19-6.188a1 1 0 0 1 1.414 1.414L13.413 12l6.19 6.189a1 1 0 0 1-1.414 1.414L12 13.413l-6.189 6.19a1 1 0 0 1-1.414-1.414L10.585 12 4.397 5.811a1 1 0 0 1 0-1.414z")
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
