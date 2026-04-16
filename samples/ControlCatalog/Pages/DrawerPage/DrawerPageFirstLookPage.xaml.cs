using System.Linq;
using Avalonia.Controls;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageFirstLookPage : UserControl
    {
        public DrawerPageFirstLookPage()
        {
            InitializeComponent();
            EnableMouseSwipeGesture(DemoDrawer);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            DemoDrawer.Opened += OnDrawerStatusChanged;
            DemoDrawer.Closed += OnDrawerStatusChanged;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            DemoDrawer.Opened -= OnDrawerStatusChanged;
            DemoDrawer.Closed -= OnDrawerStatusChanged;
        }

        private void OnDrawerStatusChanged(object? sender, System.EventArgs e) => UpdateStatus();

        private void OnToggleDrawer(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.IsOpen = !DemoDrawer.IsOpen;
        }

        private void OnGestureChanged(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.IsGestureEnabled = GestureCheck.IsChecked == true;
        }

        private void OnMenuSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrawerMenu.SelectedItem is ListBoxItem item)
            {
                DemoDrawer.Content = new ContentPage
                {
                    Header = item.Content?.ToString(),
                    Content = new TextBlock
                    {
                        Text = $"{item.Content} page content",
                        FontSize = 16,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                };
                DemoDrawer.IsOpen = false;
            }
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Drawer: {(DemoDrawer.IsOpen ? "Open" : "Closed")}";
        }

        private static void EnableMouseSwipeGesture(Control control)
        {
            var recognizer = control.GestureRecognizers
                .OfType<SwipeGestureRecognizer>()
                .FirstOrDefault();

            if (recognizer is not null)
                recognizer.IsMouseEnabled = true;
        }
    }
}
