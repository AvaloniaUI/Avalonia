using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class CarouselPage : UserControl
    {
        private Carousel _carousel;
        private Button _left;
        private Button _right;
        private ComboBox _transition;
        private ComboBox _orientation;

        public CarouselPage()
        {
            this.InitializeComponent();
            _carousel = this.Get<Carousel>("carousel");
            _left = this.Get<Button>("left");
            _right = this.Get<Button>("right");
            _transition = this.Get<ComboBox>("transition");
            _orientation = this.Get<ComboBox>("orientation");
            _left.Click += (s, e) => _carousel.Previous();
            _right.Click += (s, e) => _carousel.Next();
            _transition.SelectionChanged += TransitionChanged;
            _orientation.SelectionChanged += TransitionChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

        }

        private void TransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            switch (_transition.SelectedIndex)
            {
                case 0:
                    _carousel.PageTransition = null;
                    break;
                case 1:
                    _carousel.PageTransition = new PageSlide(TimeSpan.FromSeconds(0.25), _orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical);
                    break;
                case 2:
                    _carousel.PageTransition = new CrossFade(TimeSpan.FromSeconds(0.25));
                    break;
                case 3:
                    _carousel.PageTransition = new Rotate3DTransition(TimeSpan.FromSeconds(0.5), _orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical);
                    break;
            }
        }
    }
}
