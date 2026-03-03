using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
            await SampleNav.PushAsync(NavigationDemoHelper.CreateGalleryHomePage(SampleNav, Demos), null);
        }
    }
}
