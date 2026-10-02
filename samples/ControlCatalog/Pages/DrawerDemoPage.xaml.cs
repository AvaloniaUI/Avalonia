using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class DrawerDemoPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            // Overview
            new(SampleGroups.Overview, "First Look", "Basic DrawerPage with a navigation drawer, menu items, and detail content.",
                () => new DrawerPageFirstLookPage()),

            // Features
            new(SampleGroups.Features, "Navigation",
                "Master-detail pattern: select a drawer menu item to navigate the detail via a NavigationPage with hamburger-to-back-button transition.",
                () => new DrawerPageNavigationPage()),
            new(SampleGroups.Features, "Compact Rail",
                "CompactOverlay and CompactInline layout modes: a narrow icon rail is always visible and expands on open. Adjust rail width and open pane width.",
                () => new DrawerPageCompactPage()),
            new(SampleGroups.Features, "Breakpoint",
                "DrawerBreakpointLength: below the threshold the drawer switches to Overlay mode automatically. Resize the window or adjust the slider to see the layout switch in real time.",
                () => new DrawerPageBreakpointPage()),
            new(SampleGroups.Features, "Events",
                "Opened, Closing, and Closed drawer events plus NavigatedTo and NavigatedFrom page lifecycle events. Enable 'Cancel next close' to prevent the drawer from closing.",
                () => new DrawerPageEventsPage()),
            new(SampleGroups.Features, "RTL Layout", "Right-to-left layout: drawer opens from the right edge with mirrored gestures.",
                () => new DrawerPageRtlPage()),

            // Appearance
            new(SampleGroups.Appearance, "Customization",
                "Customize drawer behavior, layout mode, length, colors, header and footer.",
                () => new DrawerPageCustomizationPage()),
            new(SampleGroups.Appearance, "Custom Flyout",
                "Dark overlay menu with staggered item animations and CrossFade page transitions on the detail NavigationPage.",
                () => new DrawerPageCustomFlyoutPage()),
            new(SampleGroups.Appearance, "Transitions",
                "Configure the detail NavigationPage transition. Choose CrossFade, PageSlide, or CompositePageTransition to animate detail page changes.",
                () => new DrawerPageTransitionsPage()),

            // Performance
            new(SampleGroups.Performance, "Performance Monitor",
                "Track detail page swaps, live page instances, and managed heap size. Observe how GC reclaims memory after swapping pages.",
                () => new DrawerPagePerformancePage()),

            // Showcases
            new(SampleGroups.Showcases, "AvaloniaFlix",
                "Streaming app with DrawerPage wrapping NavigationPage. Hamburger auto-injected at root, back arrow on detail, and dark themed flyout menu.",
                () => new AvaloniaFlixAppPage()),
            new(SampleGroups.Showcases, "L'Avenir Restaurant",
                "Restaurant app with DrawerPage as the root container, NavigationPage for detail navigation, and TabbedPage bottom tabs for Menu, Reservations, and Profile.",
                () => new LAvenirAppPage()),
            new(SampleGroups.Showcases, "EcoTracker",
                "Sustainability tracker with CompactInline drawer, eco leaf hamburger icon, crossfade compact/open menu transitions, and green-themed Home, Stats, Habits, and Community pages.",
                () => new EcoTrackerAppPage()),
            new(SampleGroups.Showcases, "ModernApp",
                "Travel social app using a top-placement DrawerPage. A slide-down nav pane gives access to Discover, My Trips, Profile, and Settings. Features destination cards, story circles, an experience feed, a stats profile, and a travel gallery.",
                () => new ModernAppPage()),
            new(SampleGroups.Showcases, "Controls Gallery App",
                "Controls gallery app using DrawerPage CompactInline mode. Dark Fluent palette, accent pill selection indicator, search box that fades in when open, expandable category groups, and Settings pinned to the footer.",
                () => new ControlsGalleryAppPage()),
        };

        public DrawerDemoPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
