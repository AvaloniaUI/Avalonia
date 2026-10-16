using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class PipsPagerPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            new(SampleGroups.Overview, "First Look",
                "Default PipsPager with horizontal and vertical orientation, with and without navigation buttons.",
                () => new PipsPagerGettingStartedPage()),

            new(SampleGroups.Features, "Carousel Integration",
                "Bind SelectedPageIndex to a Carousel's SelectedIndex for two-way synchronized page navigation.",
                () => new PipsPagerCarouselPage()),
            new(SampleGroups.Features, "Large Collections",
                "Use MaxVisiblePips to limit visible indicators when the page count is large. Pips scroll automatically.",
                () => new PipsPagerLargeCollectionPage()),
            new(SampleGroups.Features, "Events",
                "Monitor SelectedPageIndex changes to react to user navigation.",
                () => new PipsPagerEventsPage()),

            new(SampleGroups.Appearance, "Custom Colors",
                "Override pip indicator colors using resource keys for normal, selected, and hover states.",
                () => new PipsPagerCustomColorsPage()),
            new(SampleGroups.Appearance, "Custom Button Themes",
                "Replace the default chevron navigation buttons with custom button themes.",
                () => new PipsPagerCustomButtonThemesPage()),
            new(SampleGroups.Appearance, "Custom Templates",
                "Override pip item templates to create squares, pills, numbers, or any custom shape.",
                () => new PipsPagerCustomTemplatesPage()),

            new(SampleGroups.Showcases, "Care Companion",
                "A health care onboarding flow using PipsPager as the page indicator for a CarouselPage.",
                () => new CareCompanionAppPage()),
            new(SampleGroups.Showcases, "Sanctuary",
                "A travel discovery app using PipsPager as the page indicator for a CarouselPage.",
                () => new SanctuaryShowcasePage()),
        };

        public PipsPagerPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
