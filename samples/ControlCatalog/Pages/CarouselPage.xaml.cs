using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ControlCatalog.Pages
{
    public partial class CarouselPage : UserControl
    {
        public CarouselPage()
        {
            InitializeComponent();
            
            left.Click += (s, e) => carousel.Previous();
            right.Click += (s, e) => carousel.Next();
            transition.SelectionChanged += TransitionChanged;
            orientation.SelectionChanged += TransitionChanged;

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
            };

            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            itemsCountIndicator.Text = carousel.ItemCount.ToString();
            selectedIndexIndicator.Text = carousel.SelectedIndex.ToString();

            var wrap = carousel.WrapSelection;
            left.IsEnabled = wrap || carousel.SelectedIndex > 0;
            right.IsEnabled = wrap || carousel.SelectedIndex < carousel.ItemCount - 1;
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
