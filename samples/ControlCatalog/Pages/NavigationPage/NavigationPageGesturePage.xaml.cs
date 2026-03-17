using System.Linq;
using Avalonia.Controls;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageGesturePage : UserControl
    {
        public NavigationPageGesturePage()
        {
            InitializeComponent();
            EnableMouseSwipeGesture(DemoNav);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Page 1", "← Drag from the left edge to go back", 0), null);
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Page 2", "← Drag from the left edge to go back", 1), null);
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Page 3", "← Drag from the left edge to go back", 2), null);
            UpdateStatus();
        }

        private void OnGestureEnabledChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.IsGestureEnabled = GestureCheck.IsChecked == true;
        }

        private async void OnPushPages(object? sender, RoutedEventArgs e)
        {
            var depth = DemoNav.StackDepth;
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage($"Page {depth + 1}", "← Drag from the left edge to go back", depth), null);
            UpdateStatus();
        }

        private async void OnPop(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopAsync();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Depth: {DemoNav.StackDepth}";
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
