using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationDemoPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            // Overview
            ("Overview", "First Look", "Basic NavigationPage with push/pop navigation and back button support.",
                () => new NavigationPageFirstLookPage()),
            ("Overview", "Modal Navigation", "Push and pop modal pages that appear on top of the navigation stack.",
                () => new NavigationPageModalPage()),
            ("Overview", "Navigation Events",
                "Subscribe to Pushed, Popped, PoppedToRoot, ModalPushed, and ModalPopped events.",
                () => new NavigationPageEventsPage()),

            // Appearance
            ("Appearance", "Bar Customization",
                "Customize the navigation bar background, foreground, shadow, and visibility.",
                () => new NavigationPageAppearancePage()),
            ("Appearance", "Header",
                "Set page header content: a string, icon, or any custom control in the navigation bar.",
                () => new NavigationPageTitlePage()),

            // Data
            ("Data", "Pass Data", "Pass data during navigation via constructor arguments or DataContext.",
                () => new NavigationPagePassDataPage()),
            ("Data", "MVVM Navigation",
                "Keep navigation decisions in view models by routing NavigationPage push and pop operations through a small INavigationService.",
                () => new NavigationPageMvvmPage()),

            // Features
            ("Features", "Attached Methods",
                "Per-page navigation bar and back button control via static attached methods.",
                () => new NavigationPageAttachedMethodsPage()),
            ("Features", "Back Button", "Customize, hide, or intercept the back button.",
                () => new NavigationPageBackButtonPage()),
            ("Features", "CommandBar",
                "Add, remove and position CommandBar items inside the navigation bar or as a bottom bar.",
                () => new NavigationPageToolbarPage()),
            ("Features", "Transitions",
                "Configure page transitions: PageSlide, Parallax Slide, CrossFade, Fade Through, and more.",
                () => new NavigationPageTransitionsPage()),
            ("Features", "Modal Transitions", "Configure modal transition: PageSlide from bottom, CrossFade, or None.",
                () => new NavigationPageModalTransitionsPage()),
            ("Features", "Stack Management", "Remove or insert pages anywhere in the navigation stack at runtime.",
                () => new NavigationPageStackPage()),
            ("Features", "Interactive Header",
                "Build a header with a title and live search box that filters page content in real time.",
                () => new NavigationPageInteractiveHeaderPage()),
            ("Features", "Back Swipe Gesture", "Swipe from the left edge to interactively pop the current page.",
                () => new NavigationPageGesturePage()),
            ("Features", "Scroll-Aware Bar",
                "Hide the navigation bar on downward scroll and reveal it on upward scroll.",
                () => new NavigationPageScrollAwarePage()),

            // Performance
            ("Performance", "Performance Monitor",
                "Track stack depth, live page instances, and managed heap size. Observe how memory is reclaimed after popping pages.",
                () => new NavigationPagePerformancePage()),

            // Showcases
            ("Showcases", "Pulse Fitness",
                "Login flow with RemovePage, TabbedPage dashboard with bottom tabs, and NavigationPage push for workout detail.",
                () => new PulseAppPage()),
            ("Showcases", "L'Avenir",
                "Restaurant app with DrawerPage flyout menu, TabbedPage bottom tabs, and NavigationPage push for dish detail.",
                () => new LAvenirAppPage()),
            ("Showcases", "AvaloniaFlix",
                "Streaming app with dark NavigationPage, hidden nav bar on home, and custom bar tint on movie detail pages.",
                () => new AvaloniaFlixAppPage()),
            ("Showcases", "Retro Gaming",
                "Arcade-style app with NavigationPage header, TabbedPage bottom tabs with CenteredTabPanel, and game detail push.",
                () => new RetroGamingAppPage()),
            ("Showcases", "Curved Header",
                "Shop app with dome-bottomed white header on home (nav bar hidden) and blue curved header on detail (BarLayoutBehavior.Overlay).",
                () => new NavigationPageCurvedHeaderPage()),
        };

        public NavigationDemoPage()
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
