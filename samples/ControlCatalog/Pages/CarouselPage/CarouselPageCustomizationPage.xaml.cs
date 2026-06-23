using System;
using System.Collections;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageCustomizationPage : UserControl
    {
        public CarouselPageCustomizationPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Horizontal);
            DemoCarousel.SelectionChanged += OnSelectionChanged;
            UpdateDots(DemoCarousel.SelectedIndex);
            UpdateStatus();
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            UpdateDots(DemoCarousel.SelectedIndex);
            UpdateStatus();
        }

        private void OnOrientationChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoCarousel == null)
                return;

            var axis = OrientationCombo.SelectedIndex == 1
                ? PageSlide.SlideAxis.Vertical
                : PageSlide.SlideAxis.Horizontal;

            DemoCarousel.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(300), axis);
            UpdateStatus();
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

        private void UpdateDots(int selectedIndex)
        {
            Dot0.Opacity = selectedIndex == 0 ? 1.0 : 0.4;
            Dot1.Opacity = selectedIndex == 1 ? 1.0 : 0.4;
            Dot2.Opacity = selectedIndex == 2 ? 1.0 : 0.4;
            Dot3.Opacity = selectedIndex == 3 ? 1.0 : 0.4;
        }

        private void UpdateStatus()
        {
            if (StatusText == null) return;
            var pageCount = (DemoCarousel.Pages as IList)?.Count ?? 0;
            var axis = OrientationCombo?.SelectedIndex == 1 ? "Vertical" : "Horizontal";
            StatusText.Text = $"Page {DemoCarousel.SelectedIndex + 1} of {pageCount} | {axis}";
        }
    }
}
