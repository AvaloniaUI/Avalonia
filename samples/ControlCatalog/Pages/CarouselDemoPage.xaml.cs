using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class CarouselDemoPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            // Overview
            new(SampleGroups.Overview, "First Look",
                "Basic CarouselPage with three pages and page indicator.",
                () => new CarouselPageFirstLookPage()),

            // Populate
            new(SampleGroups.Populate, "Data Templates",
                "Bind CarouselPage to an ObservableCollection, add or remove pages at runtime, and switch the page template.",
                () => new CarouselPageDataTemplatePage()),

            // Appearance
            new(SampleGroups.Appearance, "Customization",
                "Switch slide direction between horizontal and vertical with PageSlide. Page indicator dots update on each selection.",
                () => new CarouselPageCustomizationPage()),

            // Features
            new(SampleGroups.Features, "Page Transitions",
                "Animate page switches with CrossFade or PageSlide.",
                () => new CarouselPageTransitionsPage()),
            new(SampleGroups.Features, "Programmatic Selection",
                "Jump to any page programmatically with SelectedIndex and respond to SelectionChanged events.",
                () => new CarouselPageSelectionPage()),
            new(SampleGroups.Features, "Gesture & Keyboard",
                "Swipe left/right to navigate pages. Toggle IsGestureEnabled and IsKeyboardNavigationEnabled.",
                () => new CarouselPageGesturePage()),
            new(SampleGroups.Features, "Events",
                "SelectionChanged, NavigatedTo, and NavigatedFrom events. Swipe or navigate to see the live event log.",
                () => new CarouselPageEventsPage()),

            // Performance
            new(SampleGroups.Performance, "Performance Monitor",
                "Track page count, live page instances, and managed heap size. Observe how GC reclaims memory after removing pages.",
                () => new CarouselPagePerformancePage()),

            // Showcases
            new(SampleGroups.Showcases, "Sanctuary",
                "Travel discovery app with 3 full-screen immersive pages. Each page has a real background photo, gradient overlay, and themed content. Built as a 1:1 replica of a Stitch design.",
                () => new SanctuaryShowcasePage()),
            new(SampleGroups.Showcases, "Care Companion",
                "Healthcare onboarding with CarouselPage (3 pages), then a TabbedPage patient dashboard. Skip or complete onboarding to navigate to the dashboard via RemovePage.",
                () => new CareCompanionAppPage()),
        };

        public CarouselDemoPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
