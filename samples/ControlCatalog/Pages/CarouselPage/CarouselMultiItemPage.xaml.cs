using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselMultiItemPage : UserControl
    {
        public CarouselMultiItemPage()
        {
            InitializeComponent();
            PreviousButton.Click += (_, _) => DemoCarousel.Previous();
            NextButton.Click += (_, _) => DemoCarousel.Next();
            DemoCarousel.SelectionChanged += OnSelectionChanged;
        }

        private void OnViewportFractionChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (DemoCarousel is null)
                return;
            var value = Math.Round(e.NewValue, 2);
            DemoCarousel.ViewportFraction = value;
            ViewportLabel.Text = value.ToString("0.00");
            ViewportHint.Text = value >= 1d ? "1.00 — single full item." : $"~{1d / value:0.#} items visible.";
        }

        private void OnWrapChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoCarousel is null)
                return;
            DemoCarousel.WrapSelection = WrapCheck.IsChecked == true;
        }

        private void OnSwipeChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoCarousel is null)
                return;
            DemoCarousel.IsSwipeEnabled = SwipeCheck.IsChecked == true;
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            StatusText.Text = $"Item: {DemoCarousel.SelectedIndex + 1} / {DemoCarousel.ItemCount}";
        }
    }
}
