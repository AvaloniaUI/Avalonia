using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageFirstLookPage : UserControl
    {
        public CarouselPageFirstLookPage()
        {
            InitializeComponent();
        }

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

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            if (StatusText == null)
                return;
            var pageCount = (DemoCarousel.Pages as IList)?.Count ?? 0;
            StatusText.Text = $"Page {DemoCarousel.SelectedIndex + 1} of {pageCount}";
        }
    }
}
