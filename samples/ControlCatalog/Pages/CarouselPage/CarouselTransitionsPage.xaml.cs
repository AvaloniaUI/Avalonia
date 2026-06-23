using System;
using Avalonia.Animation;
using Avalonia.Controls;
using ControlCatalog.Pages.Transitions;

namespace ControlCatalog.Pages
{
    public partial class CarouselTransitionsPage : UserControl
    {
        public CarouselTransitionsPage()
        {
            InitializeComponent();
            PreviousButton.Click += (_, _) => DemoCarousel.Previous();
            NextButton.Click += (_, _) => DemoCarousel.Next();
            TransitionCombo.SelectionChanged += (_, _) => ApplyTransition();
            OrientationCombo.SelectionChanged += (_, _) => ApplyTransition();
        }

        private void ApplyTransition()
        {
            var axis = OrientationCombo.SelectedIndex == 0 ?
                PageSlide.SlideAxis.Horizontal :
                PageSlide.SlideAxis.Vertical;
            var label = axis == PageSlide.SlideAxis.Horizontal ? "Horizontal" : "Vertical";

            switch (TransitionCombo.SelectedIndex)
            {
                case 0:
                    DemoCarousel.PageTransition = null;
                    StatusText.Text = "Transition: None";
                    break;
                case 1:
                    DemoCarousel.PageTransition = new PageSlide(TimeSpan.FromSeconds(0.25), axis);
                    StatusText.Text = $"Transition: Page Slide ({label})";
                    break;
                case 2:
                    DemoCarousel.PageTransition = new CrossFade(TimeSpan.FromSeconds(0.25));
                    StatusText.Text = "Transition: Cross Fade";
                    break;
                case 3:
                    DemoCarousel.PageTransition = new Rotate3DTransition(TimeSpan.FromSeconds(0.5), axis);
                    StatusText.Text = $"Transition: Rotate 3D ({label})";
                    break;
                case 4:
                    DemoCarousel.PageTransition = new CardStackPageTransition(TimeSpan.FromSeconds(0.5), axis);
                    StatusText.Text = $"Transition: Card Stack ({label})";
                    break;
                case 5:
                    DemoCarousel.PageTransition = new WaveRevealPageTransition(TimeSpan.FromSeconds(0.8), axis);
                    StatusText.Text = $"Transition: Wave Reveal ({label})";
                    break;
                case 6:
                    DemoCarousel.PageTransition = new CompositePageTransition
                    {
                        PageTransitions =
                        {
                            new PageSlide(TimeSpan.FromSeconds(0.25), axis),
                            new CrossFade(TimeSpan.FromSeconds(0.25)),
                        }
                    };
                    StatusText.Text = "Transition: Composite (Slide + Fade)";
                    break;
            }
        }
    }
}
