using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageGesturePage : UserControl
    {
        public CarouselPageGesturePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e) => UpdateStatus();

        private void OnGestureChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoCarousel == null)
                return;
            DemoCarousel.IsGestureEnabled = GestureCheck.IsChecked == true;
        }

        private void OnKeyboardChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoCarousel == null)
                return;
            DemoCarousel.IsKeyboardNavigationEnabled = KeyboardCheck.IsChecked == true;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (StatusText == null)
                return;
            var pageCount = (DemoCarousel.Pages as IList)?.Count ?? 0;
            StatusText.Text = $"Page {DemoCarousel.SelectedIndex + 1} of {pageCount}";
        }
    }
}
