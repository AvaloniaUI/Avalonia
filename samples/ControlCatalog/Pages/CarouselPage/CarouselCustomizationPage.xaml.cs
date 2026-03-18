using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselCustomizationPage : UserControl
    {
        public CarouselCustomizationPage()
        {
            InitializeComponent();
            PreviousButton.Click += (_, _) => DemoCarousel.Previous();
            NextButton.Click += (_, _) => DemoCarousel.Next();
            OrientationCombo.SelectionChanged += (_, _) => ApplyOrientation();
            ViewportSlider.ValueChanged += OnViewportFractionChanged;
        }

        private void ApplyOrientation()
        {
            var horizontal = OrientationCombo.SelectedIndex == 0;
            var axis = horizontal ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical;
            DemoCarousel.PageTransition = new PageSlide(TimeSpan.FromSeconds(0.25), axis);
            StatusText.Text = $"Orientation: {(horizontal ? "Horizontal" : "Vertical")}";
        }

        private void OnViewportFractionChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            var value = Math.Round(e.NewValue, 2);
            DemoCarousel.ViewportFraction = value;
            ViewportLabel.Text = value.ToString("0.00");
            ViewportHint.Text = value >= 1d
                ? "1.00 shows a single full page."
                : $"{1d / value:0.##} pages fit in view. Try 0.80 for peeking.";
        }

        private void OnWrapSelectionChanged(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.WrapSelection = WrapSelectionCheck.IsChecked == true;
        }

        private void OnSwipeEnabledChanged(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.IsSwipeEnabled = SwipeEnabledCheck.IsChecked == true;
        }
    }
}
