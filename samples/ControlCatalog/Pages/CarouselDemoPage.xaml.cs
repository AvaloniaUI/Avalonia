using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselDemoPage : ContentPage
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

            // Carousel (ItemsControl) demos
            ("Carousel", "Getting Started",
                "Basic Carousel with image items and previous/next navigation buttons.",
                () => new CarouselGettingStartedPage()),
            ("Carousel", "Transitions",
                "Configure page transitions: PageSlide, CrossFade, 3D Rotation, or None.",
                () => new CarouselTransitionsPage()),
            ("Carousel", "Customization",
                "Adjust orientation and transition type to tailor the carousel layout.",
                () => new CarouselCustomizationPage()),
            ("Carousel", "Gestures & Keyboard",
                "Navigate items via swipe gesture and arrow keys. Toggle each input mode on and off.",
                () => new CarouselGesturesPage()),
            ("Carousel", "Vertical Orientation",
                "Carousel with Orientation set to Vertical, navigated with Up/Down keys, swipe, or buttons.",
                () => new CarouselVerticalPage()),
            ("Carousel", "Multi-Item Peek",
                "Adjust ViewportFraction to show multiple items simultaneously with adjacent cards peeking.",
                () => new CarouselMultiItemPage()),
            ("Carousel", "Data Binding",
                "Bind Carousel to an ObservableCollection and add, remove, or shuffle items at runtime.",
                () => new CarouselDataBindingPage()),
            ("Carousel", "Curated Gallery",
                "Editorial art gallery app with DrawerPage navigation, hero Carousel with PipsPager dots, and a horizontal peek carousel for collection highlights.",
                () => new CarouselGalleryAppPage()),
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
