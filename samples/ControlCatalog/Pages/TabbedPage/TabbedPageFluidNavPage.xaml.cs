using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageFluidNavPage : UserControl
    {
        // Icon paths — coordinate system centred at (0,0), roughly ±12 range.
        // All paths are open strokes so SKPathMeasure traces them naturally.

        // House: roof (open triangle) + body (open rectangle)
        private const string HomePath =
            "M-12,2 L0,-12 L12,2 M-7,2 L-7,10 L7,10 L7,2";

        // Magnifying glass: circle head + handle line
        private const string ExplorePath =
            "M6,-6 A7,7 0 0 1 0,-13 A7,7 0 0 1 -7,-6 A7,7 0 0 1 0,1 A7,7 0 0 1 6,-6 M3.9,1.1 L10,8";

        // Person: head circle (4 quarter-arcs) + shoulder arc
        private const string ProfilePath =
            "M5,-7 A5,5 0 0 1 0,-2 A5,5 0 0 1 -5,-7 A5,5 0 0 1 0,-12 A5,5 0 0 1 5,-7 " +
            "M-9,12 A9,7 0 0 1 9,12";

        private bool _syncing;

        public TabbedPageFluidNavPage()
        {
            InitializeComponent();
            SetupNavBar();
            WireEvents();
        }

        private void SetupNavBar()
        {
            NavBar.Items = new[]
            {
                new FluidNavItem(HomePath,    "Home"),
                new FluidNavItem(ExplorePath, "Explore"),
                new FluidNavItem(ProfilePath, "Profile"),
            };
            NavBar.SelectedIndex = 0;
        }

        private void WireEvents()
        {
            // FluidNavBar tap → TabbedPage
            NavBar.SelectionChanged += (_, index) =>
            {
                if (_syncing) return;
                _syncing = true;
                TabbedPageControl.SelectedIndex = index;
                UpdateStatus();
                _syncing = false;
            };

            // TabbedPage swipe → FluidNavBar
            TabbedPageControl.SelectionChanged += (_, _) =>
            {
                if (_syncing) return;
                var i = TabbedPageControl.SelectedIndex;
                if (NavBar.SelectedIndex != i)
                {
                    _syncing = true;
                    NavBar.SelectedIndex = i;
                    UpdateStatus();
                    _syncing = false;
                }
            };
        }

        private void UpdateStatus()
        {
            var names = new[] { "Home", "Explore", "Profile" };
            var idx   = TabbedPageControl.SelectedIndex;
            var tab   = idx >= 0 && idx < names.Length ? names[idx] : "?";
            StatusText.Text = $"Active tab: {tab}  (swipe or tap)";
        }
    }
}
