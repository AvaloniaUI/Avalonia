using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselGettingStartedPage : UserControl
    {
        public CarouselGettingStartedPage()
        {
            InitializeComponent();
            PreviousButton.Click += OnPrevious;
            NextButton.Click += OnNext;
            DemoCarousel.SelectionChanged += OnSelectionChanged;
        }

        private void OnPrevious(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.Previous();
            UpdateStatus();
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.Next();
            UpdateStatus();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var index = DemoCarousel.SelectedIndex + 1;
            var count = DemoCarousel.ItemCount;
            StatusText.Text = $"Item: {index} / {count}";
        }
    }
}
