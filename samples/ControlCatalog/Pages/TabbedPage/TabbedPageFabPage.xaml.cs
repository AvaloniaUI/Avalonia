using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageFabPage : UserControl
    {
        private static readonly StreamGeometry FeedGeometry =
            StreamGeometry.Parse("M12.9942 2.79444C12.4118 2.30208 11.5882 2.30208 11.0058 2.79444L3.50582 9.39444C3.18607 9.66478 3 10.0634 3 10.4828V20.25C3 20.9404 3.55964 21.5 4.25 21.5H8.25C8.94036 21.5 9.5 20.9404 9.5 20.25V14.75C9.5 14.6119 9.61193 14.5 9.75 14.5H14.25C14.3881 14.5 14.5 14.6119 14.5 14.75V20.25C14.5 20.9404 15.0596 21.5 15.75 21.5H19.75C20.4404 21.5 21 20.9404 21 20.25V10.4828C21 10.0634 20.8139 9.66478 20.4942 9.39444L12.9942 2.79444Z");
        private static readonly StreamGeometry DiscoverGeometry =
            StreamGeometry.Parse("M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm4.24 5.76-3.03 6.55-6.55 3.03L9.69 10.8l6.55-3.04zM12 13.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z");
        private static readonly StreamGeometry AlertsGeometry =
            StreamGeometry.Parse("M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.64-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.63 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2z");
        private static readonly StreamGeometry ProfileGeometry =
            StreamGeometry.Parse("M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z");

        private int _postCount;

        public TabbedPageFabPage()
        {
            InitializeComponent();
            SetupIcons();

            FabButton.Click += OnFabClicked;
            TriggerFabButton.Click += OnFabClicked;
        }

        private void SetupIcons()
        {
            FeedPage.Icon = FeedGeometry;
            DiscoverPage.Icon = DiscoverGeometry;
            AlertsPage.Icon = AlertsGeometry;
            ProfilePage.Icon = ProfileGeometry;
        }

        private void OnFabClicked(object? sender, RoutedEventArgs e)
        {
            _postCount++;
            StatusText.Text = _postCount == 1
                ? "Post created! Check your feed."
                : $"{_postCount} posts created!";
        }
    }
}
