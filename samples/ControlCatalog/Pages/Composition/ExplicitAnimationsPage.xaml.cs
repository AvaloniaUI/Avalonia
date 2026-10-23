using System;
using System.Numerics;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace ControlCatalog.Pages.Composition
{
    public partial class ExplicitAnimationsPage : UserControl
    {
        private CompositionSolidColorVisual? _solidVisual;
        public ExplicitAnimationsPage()
        {
            InitializeComponent();
            AttachAnimatedSolidVisual(SolidVisualHost);
        }

        private void AttachAnimatedSolidVisual(Visual v)
        {
            void Update()
            {
                if (_solidVisual == null)
                    return;
                _solidVisual.Size = new(v.Bounds.Width / 3, v.Bounds.Height / 3);
                _solidVisual.Offset = new(v.Bounds.Width / 3, v.Bounds.Height / 3, 0);
            }
            v.AttachedToVisualTree += delegate
            {
                var compositor = ElementComposition.GetElementVisual(v)?.Compositor;
                if (compositor == null || _solidVisual?.Compositor == compositor)
                    return;
                _solidVisual = compositor.CreateSolidColorVisual();
                ElementComposition.SetElementChildVisual(v, _solidVisual);
                _solidVisual.Color = Colors.Red;
                var animation = _solidVisual.Compositor.CreateColorKeyFrameAnimation();
                animation.InsertKeyFrame(0, Colors.Red);
                animation.InsertKeyFrame(0.5f, Colors.Blue);
                animation.InsertKeyFrame(1, Colors.Green);
                animation.Duration = TimeSpan.FromSeconds(5);
                animation.IterationBehavior = AnimationIterationBehavior.Forever;
                animation.Direction = PlaybackDirection.Alternate;
                _solidVisual.StartAnimation("Color", animation);

                _solidVisual.AnchorPoint = new(0, 0);

                var scale = _solidVisual.Compositor.CreateVector3KeyFrameAnimation();
                scale.Duration = TimeSpan.FromSeconds(5);
                scale.IterationBehavior = AnimationIterationBehavior.Forever;
                scale.InsertKeyFrame(0, new Vector3(1, 1, 0));
                scale.InsertKeyFrame(0.5f, new Vector3(1.5f, 1.5f, 0));
                scale.InsertKeyFrame(1, new Vector3(1, 1, 0));

                _solidVisual.StartAnimation("Scale", scale);

                var center =
                    _solidVisual.Compositor.CreateExpressionAnimation(
                        "Vector3(this.Target.Size.X * 0.5, this.Target.Size.Y * 0.5, 1)");
                _solidVisual.StartAnimation("CenterPoint", center);
                Update();
            };
            v.PropertyChanged += (_, a) =>
            {
                if (a.Property == BoundsProperty)
                    Update();
            };
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Thread.Sleep(2000);
        }
    }
}
