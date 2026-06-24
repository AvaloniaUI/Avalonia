using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageSelectionPage : UserControl
    {
        public CarouselPageSelectionPage()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateStatus();
        }

        private void OnGoTo0(object? sender, RoutedEventArgs e) => DemoCarousel.SelectedIndex = 0;
        private void OnGoTo1(object? sender, RoutedEventArgs e) => DemoCarousel.SelectedIndex = 1;
        private void OnGoTo2(object? sender, RoutedEventArgs e) => DemoCarousel.SelectedIndex = 2;
        private void OnGoTo3(object? sender, RoutedEventArgs e) => DemoCarousel.SelectedIndex = 3;

        private void OnFirst(object? sender, RoutedEventArgs e) => DemoCarousel.SelectedIndex = 0;

        private void OnPrevious(object? sender, RoutedEventArgs e)
        {
            if (DemoCarousel.SelectedIndex > 0)
                DemoCarousel.SelectedIndex--;
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            var pageCount = (DemoCarousel.Pages as IList)?.Count ?? 0;
            if (DemoCarousel.SelectedIndex < pageCount - 1)
                DemoCarousel.SelectedIndex++;
        }

        private void OnLast(object? sender, RoutedEventArgs e)
        {
            var pageCount = (DemoCarousel.Pages as IList)?.Count ?? 0;
            if (pageCount > 0)
                DemoCarousel.SelectedIndex = pageCount - 1;
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
            var header = (DemoCarousel.SelectedPage as ContentPage)?.Header?.ToString() ?? "—";
            StatusText.Text = $"Page {DemoCarousel.SelectedIndex + 1} of {pageCount}: {header}";
        }
    }
}
