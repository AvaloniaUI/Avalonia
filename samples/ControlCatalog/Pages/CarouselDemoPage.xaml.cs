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
            ("Overview", "Getting Started",
                "Basic Carousel with image items and previous/next navigation buttons.",
                () => new CarouselGettingStartedPage()),

            // Features
            ("Features", "Transitions",
                "Configure page transitions: PageSlide, CrossFade, 3D Rotation, or None.",
                () => new CarouselTransitionsPage()),
            ("Features", "Customization",
                "Adjust orientation and transition type to tailor the carousel layout.",
                () => new CarouselCustomizationPage()),
            ("Features", "Gestures & Keyboard",
                "Navigate items via swipe gesture and arrow keys. Toggle each input mode on and off.",
                () => new CarouselGesturesPage()),
            ("Features", "Vertical Orientation",
                "Carousel with Orientation set to Vertical, navigated with Up/Down keys, swipe, or buttons.",
                () => new CarouselVerticalPage()),
            ("Features", "Multi-Item Peek",
                "Adjust ViewportFraction to show multiple items simultaneously with adjacent cards peeking.",
                () => new CarouselMultiItemPage()),
            ("Features", "Data Binding",
                "Bind Carousel to an ObservableCollection and add, remove, or shuffle items at runtime.",
                () => new CarouselDataBindingPage()),

            // Showcases
            ("Showcases", "Curated Gallery",
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
