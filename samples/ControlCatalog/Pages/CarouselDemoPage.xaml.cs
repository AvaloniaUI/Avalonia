using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselDemoPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            // Overview
            ("Overview", "First Look",
                "Basic CarouselPage with three pages and page indicator.",
                () => new CarouselPageFirstLookPage()),

            // Populate
            ("Populate", "Data Templates",
                "Bind CarouselPage to an ObservableCollection, add or remove pages at runtime, and switch the page template.",
                () => new CarouselPageDataTemplatePage()),

            // Appearance
            ("Appearance", "Customization",
                "Switch slide direction between horizontal and vertical with PageSlide. Page indicator dots update on each selection.",
                () => new CarouselPageCustomizationPage()),

            // Features
            ("Features", "Page Transitions",
                "Animate page switches with CrossFade or PageSlide.",
                () => new CarouselPageTransitionsPage()),
            ("Features", "Programmatic Selection",
                "Jump to any page programmatically with SelectedIndex and respond to SelectionChanged events.",
                () => new CarouselPageSelectionPage()),
            ("Features", "Gesture & Keyboard",
                "Swipe left/right to navigate pages. Toggle IsGestureEnabled and IsKeyboardNavigationEnabled.",
                () => new CarouselPageGesturePage()),
            ("Features", "Events",
                "SelectionChanged, NavigatedTo, and NavigatedFrom events. Swipe or navigate to see the live event log.",
                () => new CarouselPageEventsPage()),

            // Performance
            ("Performance", "Performance Monitor",
                "Track page count, live page instances, and managed heap size. Observe how GC reclaims memory after removing pages.",
                () => new CarouselPagePerformancePage()),

            // Showcases
            ("Showcases", "Sanctuary",
                "Travel discovery app with 3 full-screen immersive pages. Each page has a real background photo, gradient overlay, and themed content. Built as a 1:1 replica of a Stitch design.",
                () => new SanctuaryShowcasePage()),
            ("Showcases", "Care Companion",
                "Healthcare onboarding with CarouselPage (3 pages), then a TabbedPage patient dashboard. Skip or complete onboarding to navigate to the dashboard via RemovePage.",
                () => new CareCompanionAppPage()),
        };

        public CarouselDemoPage()
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
