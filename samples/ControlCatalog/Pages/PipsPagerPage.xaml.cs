using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class PipsPagerPage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            ("Getting Started", "First Look",
                "Default PipsPager with horizontal and vertical orientation, with and without navigation buttons.",
                () => new PipsPagerGettingStartedPage()),

            ("Features", "Carousel Integration",
                "Bind SelectedPageIndex to a Carousel's SelectedIndex for two-way synchronized page navigation.",
                () => new PipsPagerCarouselPage()),
            ("Features", "Large Collections",
                "Use MaxVisiblePips to limit visible indicators when the page count is large. Pips scroll automatically.",
                () => new PipsPagerLargeCollectionPage()),
            ("Features", "Events",
                "Monitor SelectedPageIndex changes to react to user navigation.",
                () => new PipsPagerEventsPage()),

            ("Appearance", "Custom Colors",
                "Override pip indicator colors using resource keys for normal, selected, and hover states.",
                () => new PipsPagerCustomColorsPage()),
            ("Appearance", "Custom Button Themes",
                "Replace the default chevron navigation buttons with custom button themes.",
                () => new PipsPagerCustomButtonThemesPage()),
            ("Appearance", "Custom Templates",
                "Override pip item templates to create squares, pills, numbers, or any custom shape.",
                () => new PipsPagerCustomTemplatesPage()),

            ("Showcases", "Care Companion",
                "A health care onboarding flow using PipsPager as the page indicator for a CarouselPage.",
                () => new CareCompanionAppPage()),
            ("Showcases", "Sanctuary",
                "A travel discovery app using PipsPager as the page indicator for a CarouselPage.",
                () => new SanctuaryShowcasePage()),
        };

        public PipsPagerPage()
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
