using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ControlCatalog.Pages.Transitions;

namespace ControlCatalog.Pages
{
    public partial class CarouselPage : ContentPage
    {
        public CarouselPage()
        {
            InitializeComponent();
            
            left.Click += (s, e) => carousel.Previous();
            right.Click += (s, e) => carousel.Next();
            transition.SelectionChanged += TransitionChanged;
            orientation.SelectionChanged += TransitionChanged;
            viewportFraction.ValueChanged += ViewportFractionChanged;

            wrapSelection.IsChecked = carousel.WrapSelection;
            wrapSelection.IsCheckedChanged += (s, e) =>
            {
                carousel.WrapSelection = wrapSelection.IsChecked ?? false;
                UpdateButtonState();
            };

            swipeEnabled.IsChecked = carousel.IsSwipeEnabled;
            swipeEnabled.IsCheckedChanged += (s, e) =>
            {
                carousel.IsSwipeEnabled = swipeEnabled.IsChecked ?? false;
            };
            
            carousel.PropertyChanged += (s, e) =>
            {
                if (e.Property == SelectingItemsControl.SelectedIndexProperty)
                {
                    UpdateButtonState();
                }
                else if (e.Property == Carousel.ViewportFractionProperty)
                {
                    UpdateViewportFractionDisplay();
                }
            };

            carousel.ViewportFraction = viewportFraction.Value;
            UpdateButtonState();
            UpdateViewportFractionDisplay();
        }

        private void UpdateButtonState()
        {
            itemsCountIndicator.Text = carousel.ItemCount.ToString();
            selectedIndexIndicator.Text = carousel.SelectedIndex.ToString();

            var wrap = carousel.WrapSelection;
            left.IsEnabled = wrap || carousel.SelectedIndex > 0;
            right.IsEnabled = wrap || carousel.SelectedIndex < carousel.ItemCount - 1;
        }

        private void ViewportFractionChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            carousel.ViewportFraction = Math.Round(e.NewValue, 2);
            UpdateViewportFractionDisplay();
        }

        private void UpdateViewportFractionDisplay()
        {
            var value = carousel.ViewportFraction;
            viewportFractionIndicator.Text = value.ToString("0.00");

            var pagesInView = 1d / value;
            viewportFractionHint.Text = value >= 1d
                ? "1.00 shows a single full page."
                : $"{pagesInView:0.##} pages fit in view. Try 0.80 for peeking or 0.33 for three full items.";
        }

        private void TransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var isVertical = orientation.SelectedIndex == 1;
            var axis = isVertical ? PageSlide.SlideAxis.Vertical : PageSlide.SlideAxis.Horizontal;
            
            switch (transition.SelectedIndex)
            {
                case 0:
                    carousel.PageTransition = null;
                    break;
                case 1:
                    carousel.PageTransition = new PageSlide(TimeSpan.FromSeconds(0.25), axis);
                    break;
                case 2:
                    carousel.PageTransition = new CrossFade(TimeSpan.FromSeconds(0.25));
                    break;
                case 3:
                    carousel.PageTransition = new Rotate3DTransition(TimeSpan.FromSeconds(0.5), axis);
                    break;
                case 4:
                    carousel.PageTransition = new CardStackPageTransition(TimeSpan.FromSeconds(0.5), axis);
                    break;
                case 5:
                    carousel.PageTransition = new WaveRevealPageTransition(TimeSpan.FromSeconds(0.8), axis);
                    break;
                case 6:
                    carousel.PageTransition = new CompositePageTransition
                    {
                        PageTransitions =
                        {
                            new PageSlide(TimeSpan.FromSeconds(0.25), axis),
                            new CrossFade(TimeSpan.FromSeconds(0.25)),
                        }
                    };
                    break;
            }
            
            UpdateLayoutForOrientation(isVertical);
        }

        private void UpdateLayoutForOrientation(bool isVertical)
        {
            if (isVertical)
            {
                Grid.SetColumn(left, 1);
                Grid.SetRow(left, 0);
                Grid.SetColumn(right, 1);
                Grid.SetRow(right, 2);
                
                left.Padding = new Thickness(20, 10);
                right.Padding = new Thickness(20, 10);

                leftArrow.RenderTransform = new Avalonia.Media.RotateTransform(90);
                rightArrow.RenderTransform = new Avalonia.Media.RotateTransform(90);
            }
            else
            {
                Grid.SetColumn(left, 0);
                Grid.SetRow(left, 1);
                Grid.SetColumn(right, 2);
                Grid.SetRow(right, 1);

                left.Padding = new Thickness(10, 20);
                right.Padding = new Thickness(10, 20);

                leftArrow.RenderTransform = null;
                rightArrow.RenderTransform = null;
            }
        }
    }
}
