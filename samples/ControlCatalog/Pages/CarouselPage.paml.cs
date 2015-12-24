using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class CarouselPage : UserControl
    {
        private Carousel _carousel;
        private Button _left;
        private Button _right;

        public CarouselPage()
        {
            this.InitializeComponent();
            _left.Click += (s, e) => _carousel.Previous();
            _right.Click += (s, e) => _carousel.Next();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
            _carousel = this.FindControl<Carousel>("carousel");
            _left = this.FindControl<Button>("left");
            _right = this.FindControl<Button>("right");
        }
    }
}
