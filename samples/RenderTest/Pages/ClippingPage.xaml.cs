using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace RenderTest.Pages
{
    public class ClippingPage : UserControl
    {
        private Geometry _clip;

        public ClippingPage()
        {
            InitializeComponent();
            CreateAnimations();
            WireUpCheckbox();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreateAnimations()
        {
            var clipped = this.FindControl<Border>("clipChild");
            var degrees = Animate.Timer.Select(x => x.TotalMilliseconds / 5);
            clipped.RenderTransform = new RotateTransform();
            clipped.RenderTransform.Bind(RotateTransform.AngleProperty, degrees, BindingPriority.Animation);
            clipped.Bind(
                Border.BackgroundProperty,
                clipped.GetObservable(Control.IsPointerOverProperty)
                    .Select(x => x ? Brushes.Crimson : AvaloniaProperty.UnsetValue));
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
