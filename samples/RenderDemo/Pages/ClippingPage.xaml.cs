using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace RenderDemo.Pages
{
    public class ClippingPage : UserControl
    {
        private Geometry _clip;

        public ClippingPage()
        {
            InitializeComponent();
            WireUpCheckbox();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void WireUpCheckbox()
        {
            var useMask = this.FindControl<CheckBox>("useMask");
            var clipped = this.FindControl<Border>("clipped");
            _clip = clipped.Clip;
            useMask.Click += (s, e) => clipped.Clip = clipped.Clip == null ? _clip : null;
        }
    }
}
