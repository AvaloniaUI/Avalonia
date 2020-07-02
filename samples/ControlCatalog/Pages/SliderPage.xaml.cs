using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class SliderPage : UserControl
    {
        public SliderPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var slider = this.FindControl<Slider>("CustomTickedSlider");
            slider.Ticks = new List<double>
            {
                0d,
                5d,
                20d,
                50d,
                100d
            };
        }
    }
}
