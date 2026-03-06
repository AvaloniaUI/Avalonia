using System;
using System.Collections;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageTransitionsPage : UserControl
    {
        public CarouselPageTransitionsPage()
        {
            InitializeComponent();
        }

        private void OnTransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoCarousel == null)
                return;

            DemoCarousel.PageTransition = TransitionCombo?.SelectedIndex switch
            {
                0 => null,
                1 => new CrossFade(TimeSpan.FromMilliseconds(300)),
                2 => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Horizontal),
                3 => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical),
                _ => null
            };

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

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (StatusText == null)
                return;
            var pageCount = (DemoCarousel.Pages as IList)?.Count ?? 0;
            var modeName = DemoCarousel.PageTransition == null ? "None" : DemoCarousel.PageTransition.GetType().Name;
            StatusText.Text = $"Page {DemoCarousel.SelectedIndex + 1} of {pageCount} | Transition: {modeName}";
        }
    }
}
