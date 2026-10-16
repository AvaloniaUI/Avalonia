using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class CarouselPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            new(SampleGroups.Overview, "Getting Started",
                "Basic Carousel with image items and previous/next navigation buttons.",
                () => new CarouselGettingStartedPage()),
            new(SampleGroups.Features, "Transitions",
                "Configure page transitions: PageSlide, CrossFade, 3D Rotation, or None.",
                () => new CarouselTransitionsPage()),
            new(SampleGroups.Appearance, "Customization",
                "Adjust orientation and transition type to tailor the carousel layout.",
                () => new CarouselCustomizationPage()),
            new(SampleGroups.Features, "Gestures & Keyboard",
                "Navigate items via swipe gesture and arrow keys. Toggle each input mode on and off.",
                () => new CarouselGesturesPage()),
            new(SampleGroups.Features, "Vertical Orientation",
                "Carousel with Orientation set to Vertical, navigated with Up/Down keys, swipe, or buttons.",
                () => new CarouselVerticalPage()),
            new(SampleGroups.Features, "Multi-Item Peek",
                "Adjust ViewportFraction to show multiple items simultaneously with adjacent cards peeking.",
                () => new CarouselMultiItemPage()),
            new(SampleGroups.Populate, "Data Binding",
                "Bind Carousel to an ObservableCollection and add, remove, or shuffle items at runtime.",
                () => new CarouselDataBindingPage()),
            new(SampleGroups.Showcases, "Curated Gallery",
                "Editorial art gallery app with DrawerPage navigation, hero Carousel with PipsPager dots, and a horizontal peek carousel for collection highlights.",
                () => new CarouselGalleryAppPage()),
        };

        public CarouselPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
