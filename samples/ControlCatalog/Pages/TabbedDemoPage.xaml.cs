using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
                "SelectionChanged, NavigatedTo, and NavigatedFrom events. Switch tabs to see the live event log.",
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
            await SampleNav.PushAsync(NavigationDemoHelper.CreateGalleryHomePage(SampleNav, Demos), null);
        }
    }
}
