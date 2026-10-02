using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedDemoPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            // Overview
            new(SampleGroups.Overview, "First Look",
                "Basic TabbedPage with three tabs, tab placement selector, and selection status.",
                () => new TabbedPageFirstLookPage()),

            // Populate
            new(SampleGroups.Populate, "Page Collection",
                "Populate a TabbedPage by adding ContentPage objects directly to the Pages collection.",
                () => new TabbedPageCollectionPage()),
            new(SampleGroups.Populate, "Data Templates",
                "Bind TabbedPage to an ObservableCollection, add or remove tabs at runtime, and switch the page template.",
                () => new TabbedPageDataTemplatePage()),

            // Appearance
            new(SampleGroups.Appearance, "Tab Customization",
                "Customize tab placement, bar background, selected and unselected tab colors.",
                () => new TabbedPageCustomizationPage()),
            new(SampleGroups.Appearance, "Custom Tab Bar",
                "VYNTRA-style custom tab bar with floating pill, brand colours, and system-adaptive theme using only resource overrides and styles.",
                () => new TabbedPageCustomTabBarPage()),
            new(SampleGroups.Appearance, "FAB Tab Bar",
                "Social-media-style bottom nav with a central floating action button that triggers a command, not a tab.",
                () => new TabbedPageFabPage()),
            new(SampleGroups.Appearance, "Fluid Nav Bar",
                "Inspired by the Flutter fluid_nav_bar vignette. Color themes with animated indicator and icons.",
                () => new TabbedPageFluidNavPage()),

            // Features
            new(SampleGroups.Features, "Programmatic Selection",
                "Preset the initial tab with SelectedIndex, jump to any tab programmatically, and respond to SelectionChanged events.",
                () => new TabbedPageProgrammaticPage()),
            new(SampleGroups.Features, "Placement", "Switch the tab bar between Top, Bottom, Left, and Right placements.",
                () => new TabbedPagePlacementPage()),
            new(SampleGroups.Features, "Page Transitions",
                "Animate tab switches with CrossFade, PageSlide, or composite transitions.",
                () => new TabbedPageTransitionsPage()),
            new(SampleGroups.Features, "Keyboard Navigation",
                "Keyboard shortcuts to navigate between tabs, with a toggle to enable or disable.",
                () => new TabbedPageKeyboardPage()),
            new(SampleGroups.Features, "Swipe Gestures",
                "Swipe left/right (Top/Bottom) or up/down (Left/Right) to navigate. Toggle IsGestureEnabled.",
                () => new TabbedPageGesturePage()),
            new(SampleGroups.Features, "Events",
                "SelectionChanged, NavigatedTo, and NavigatedFrom events. Switch tabs to see the live event log.",
                () => new TabbedPageEventsPage()),
            new(SampleGroups.Features, "Disabled Tabs",
                "IsTabEnabled attached property: disable individual tabs so they cannot be selected.",
                () => new TabbedPageDisabledTabsPage()),

            // Performance
            new(SampleGroups.Performance, "Performance Monitor",
                "Track tab count, live page instances, and managed heap size. Observe how GC reclaims memory after removing tabs.",
                () => new TabbedPagePerformancePage()),

            // Composition
            new(SampleGroups.Features, "With NavigationPage",
                "Embed a NavigationPage inside each TabbedPage tab for drill-down navigation.",
                () => new TabbedPageWithNavigationPage()),
            new(SampleGroups.Features, "With DrawerPage",
                "Combine TabbedPage with DrawerPage: a global navigation drawer sits over tabbed content.",
                () => new TabbedPageWithDrawerPage()),

            // Showcases
            new(SampleGroups.Showcases, "Pulse Fitness",
                "Fitness app with bottom TabbedPage navigation, NavigationPage drill-down inside tabs, and workout detail screens.",
                () => new PulseAppPage()),
            new(SampleGroups.Showcases, "L'Avenir Restaurant",
                "Restaurant app with DrawerPage root, NavigationPage detail, and TabbedPage bottom tabs for Menu, Reservations, and Profile.",
                () => new LAvenirAppPage()),
            new(SampleGroups.Showcases, "Retro Gaming",
                "Arcade-style app with NavigationPage header, TabbedPage bottom tabs with CenteredTabPanel, and game detail push.",
                () => new RetroGamingAppPage()),
        };

        public TabbedDemoPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
