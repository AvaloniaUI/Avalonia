using System;
using Avalonia.Animation;
using Avalonia.Controls;

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
        }

        private void TransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            switch (transition.SelectedIndex)
            {
                case 0:
                    carousel.PageTransition = null;
                    break;
                case 1:
                    carousel.PageTransition = new PageSlide(TimeSpan.FromSeconds(0.25), orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical);
                    break;
                case 2:
                    carousel.PageTransition = new CrossFade(TimeSpan.FromSeconds(0.25));
                    break;
                case 3:
                    carousel.PageTransition = new Rotate3DTransition(TimeSpan.FromSeconds(0.5), orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical);
                    break;
            }
        }
    }
}
