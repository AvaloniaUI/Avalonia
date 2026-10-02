using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class ContentDemoPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            // Overview
            new(SampleGroups.Overview, "First Look",     "Basic ContentPage with header, content, and background inside a NavigationPage.",                           () => new ContentPageFirstLookPage()),

            // Appearance
            new(SampleGroups.Appearance, "Customization", "Adjust content alignment, background color, and padding to understand how ContentPage adapts its layout.", () => new ContentPageCustomizationPage()),

            // Features
            new(SampleGroups.Features, "CommandBar",     "Attach a CommandBar to the top or bottom of a ContentPage. Add and remove items at runtime.",               () => new ContentPageCommandBarPage()),
            new(SampleGroups.Features, "Safe Area",      "Understand how AutomaticallyApplySafeAreaPadding absorbs platform insets.",                                () => new ContentPageSafeAreaPage()),
            new(SampleGroups.Features, "Events",         "Observe page lifecycle events: NavigatedTo, NavigatedFrom, and Navigating.", () => new ContentPageEventsPage()),

            // Performance
            new(SampleGroups.Performance, "Performance Monitor", "Push ContentPages of varying weight (50 KB to 2 MB) and observe live instance count and managed heap. Pop pages and force GC to confirm memory is released.", () => new ContentPagePerformancePage()),
        };

        public ContentDemoPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
