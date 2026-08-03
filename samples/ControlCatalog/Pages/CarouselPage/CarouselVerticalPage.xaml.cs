using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselVerticalPage : UserControl
    {
        public CarouselVerticalPage()
        {
            InitializeComponent();
            PreviousButton.Click += (_, _) => DemoCarousel.Previous();
            NextButton.Click += (_, _) => DemoCarousel.Next();
            DemoCarousel.SelectionChanged += OnSelectionChanged;
            TransitionCombo.SelectionChanged += OnTransitionChanged;
            DemoCarousel.Loaded += (_, _) => DemoCarousel.Focus();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            StatusText.Text = $"Item: {DemoCarousel.SelectedIndex + 1} / {DemoCarousel.ItemCount}";
        }

        private void OnTransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            DemoCarousel.PageTransition = TransitionCombo.SelectedIndex switch
            {
                1 => new CrossFade(System.TimeSpan.FromSeconds(0.3)),
                2 => null,
                _ => new PageSlide(System.TimeSpan.FromSeconds(0.3), PageSlide.SlideAxis.Vertical),
            };
        }

        private void OnWrapSelectionChanged(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.WrapSelection = WrapCheck.IsChecked == true;
        }
    }
}
