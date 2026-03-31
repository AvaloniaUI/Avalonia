using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class GesturePage : UserControl
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            ("Touch / Pen", "Pull Gesture",
                "Press and drag from colored border zones. A green ball tracks the pull delta and springs back on release.",
                () => new GesturePullPage()),

            ("Multi Touch", "Pinch / Zoom",
                "Pinch to scale an image using composition visuals. Scroll to pan when zoomed in.",
                () => new GesturePinchZoomPage()),

            ("Multi Touch", "Pinch / Rotation",
                "Pinch to rotate a rectangle. The Angle property from the pinch event drives a RotateTransform.",
                () => new GesturePinchRotationPage()),

            ("Touch / Pen / Mouse", "Swipe Gesture",
                "Swipe horizontally or vertically. Configure direction, threshold, and mouse support. Shows live delta, velocity, and direction.",
                () => new GestureSwipePage()),
        };

        public GesturePage()
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
