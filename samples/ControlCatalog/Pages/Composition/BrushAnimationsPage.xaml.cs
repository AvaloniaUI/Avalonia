using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace ControlCatalog.Pages.Composition
{
    public partial class BrushAnimationsPage : UserControl
    {
        public BrushAnimationsPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
        }

        private void ButtonThreadSleep(object? sender, RoutedEventArgs e)
        {
            Thread.Sleep(5000);
        }

        private void SolidBrushStartAnimation_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(SolidBrushHost);
            if (visual == null)
                return;

            var compositor = visual.Compositor;
            var brush = SolidBrushHost.Background as CompositionSolidColorBrush
                        ?? compositor.CreateSolidColorBrush();

            brush.Color = Color.FromRgb(168, 213, 85);
            SolidBrushHost.Background = brush;

            var animation = compositor.CreateColorKeyFrameAnimation();
            animation.InsertKeyFrame(0f, Color.FromRgb(168, 213, 85));
            animation.InsertKeyFrame(0.25f, Color.FromRgb(31, 167, 168));
            animation.InsertKeyFrame(0.75f, Color.FromRgb(44, 121, 251));
            animation.InsertKeyFrame(1f, Color.FromRgb(168, 213, 85));
            animation.Duration = TimeSpan.FromSeconds(4);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Target = "Color";
            brush?.StartAnimation("Color", animation);
        }

        private void SolidBrushStopAnimate_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(SolidBrushHost);
            if (visual == null)
                return;

            var brush = SolidBrushHost.Background as CompositionSolidColorBrush;
            SolidBrushHost.Background = Brushes.Gray;
        }


        private void LinearBrushStartAnimation_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(LinearBrushHost);
            if (visual == null)
                return;

            var compositor = visual.Compositor;

            var brush = LinearBrushHost.Background as CompositionLinearGradientBrush
                        ?? compositor.CreateLinearGradientBrush();

            brush.GradientStops.Clear();
            brush.GradientStops.Add(compositor.CreateGradientStop(0f, Color.FromRgb(168, 213, 85)));
            brush.GradientStops.Add(compositor.CreateGradientStop(0.33f, Color.FromRgb(31, 167, 168)));
            brush.GradientStops.Add(compositor.CreateGradientStop(0.66f, Color.FromRgb(44, 121, 251)));
            brush.GradientStops.Add(compositor.CreateGradientStop(1f, Color.FromRgb(168, 213, 85)));
            brush.StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative);
            brush.EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative);

            LinearBrushHost.Background = brush;

            var start = compositor.CreateRelativePointKeyFrameAnimation();
            start.Duration = TimeSpan.FromSeconds(4);
            start.IterationBehavior = AnimationIterationBehavior.Forever;
            start.InsertKeyFrame(0f, new RelativePoint(-1, 0, RelativeUnit.Relative));
            start.InsertKeyFrame(0.5f, new RelativePoint(1, 0, RelativeUnit.Relative));
            start.InsertKeyFrame(1f, new RelativePoint(-1, 0, RelativeUnit.Relative));
            start.Target = "StartPoint";

            var end = compositor.CreateRelativePointKeyFrameAnimation();
            end.Duration = start.Duration;
            end.IterationBehavior = AnimationIterationBehavior.Forever;
            end.InsertKeyFrame(0f, new RelativePoint(0, 0, RelativeUnit.Relative));
            end.InsertKeyFrame(0.5f, new RelativePoint(2, 0, RelativeUnit.Relative));
            end.InsertKeyFrame(1f, new RelativePoint(0, 0, RelativeUnit.Relative));
            end.Target = "EndPoint";

            AnimateGradientAnimations(brush);

            var group = compositor.CreateAnimationGroup();
            group.Add(start);
            group.Add(end);

            brush?.StartAnimationGroup(group);
        }

        private void LinearBrushStopAnimate_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(LinearBrushHost);
            if (visual == null)
                return;

            LinearBrushHost.Background = Brushes.Gray;
        }

        private void RadialBrushStartAnimation_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(RadialBrushHost);
            if (visual == null)
                return;

            var compositor = visual.Compositor;

            var brush = RadialBrushHost.Background as CompositionRadialGradientBrush
                        ?? compositor.CreateRadialGradientBrush();

            brush.GradientStops.Clear();
            brush.GradientStops.Add(compositor.CreateGradientStop(0f, Colors.Yellow));
            brush.GradientStops.Add(compositor.CreateGradientStop(0.6f, Colors.Orange));
            brush.GradientStops.Add(compositor.CreateGradientStop(1f, Colors.Red));
            brush.Center = RelativePoint.Center;
            brush.GradientOrigin = RelativePoint.Center;

            RadialBrushHost.Background = brush;

            var centerAnim = compositor.CreateRelativePointKeyFrameAnimation();
            centerAnim.Duration = TimeSpan.FromSeconds(3);
            centerAnim.IterationBehavior = AnimationIterationBehavior.Forever;
            centerAnim.InsertKeyFrame(0f, new RelativePoint(0.2, 0.5, RelativeUnit.Relative));
            centerAnim.InsertKeyFrame(0.5f, new RelativePoint(0.8, 0.5, RelativeUnit.Relative));
            centerAnim.InsertKeyFrame(1f, new RelativePoint(0.2, 0.5, RelativeUnit.Relative));
            centerAnim.Target = "Center";

            var radiusXExpression = compositor.CreateExpressionAnimation(
                "RelativeScalar(Clamp(0.2 + Abs(this.Target.Center.X - 0.5) * 0.9, 0.2, 0.85), RelativeUnit.Relative)");
            var radiusYExpression = compositor.CreateExpressionAnimation(
                "RelativeScalar(Clamp(0.2 + Abs(this.Target.Center.Y - 0.5) * 0.9, 0.2, 0.85), RelativeUnit.Relative)");
            radiusXExpression.Target = "RadiusX";
            radiusYExpression.Target = "RadiusY";

            AnimateGradientAnimations(brush);

            var animationGroup = compositor.CreateAnimationGroup();

            animationGroup.Add(centerAnim);
            animationGroup.Add(radiusXExpression);
            animationGroup.Add(radiusYExpression);

            brush?.StartAnimationGroup(animationGroup);
        }

        private void RadialBrushStopAnimate_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(RadialBrushHost);
            if (visual == null)
                return;

            RadialBrushHost.Background = Brushes.Gray;
        }

        private void ConicBrushStartAnimation_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(ConicBrushHost);
            if (visual == null)
                return;

            var compositor = visual.Compositor;

            var brush = ConicBrushHost.Background as CompositionConicGradientBrush
                        ?? compositor.CreateConicGradientBrush();

            brush.GradientStops.Clear();
            brush.GradientStops.Add(compositor.CreateGradientStop(0f, Colors.Cyan));
            brush.GradientStops.Add(compositor.CreateGradientStop(0.25f, Colors.Magenta));
            brush.GradientStops.Add(compositor.CreateGradientStop(0.5f, Colors.Yellow));
            brush.GradientStops.Add(compositor.CreateGradientStop(0.75f, Colors.Lime));
            brush.GradientStops.Add(compositor.CreateGradientStop(1f, Colors.Cyan));

            brush.Center = RelativePoint.Center;

            ConicBrushHost.Background = brush;

            var angleAnim = compositor.CreateScalarKeyFrameAnimation();
            angleAnim.Duration = TimeSpan.FromSeconds(5);
            angleAnim.IterationBehavior = AnimationIterationBehavior.Forever;
            angleAnim.InsertKeyFrame(0f, 0f);
            angleAnim.InsertKeyFrame(1f, 360f);
            angleAnim.Target = "Angle";
            var centerExpression = compositor.CreateExpressionAnimation(
                "RelativePoint(Abs(Cos(ToRadians(this.Target.Angle))), Abs(Sin(ToRadians(this.Target.Angle))), RelativeUnit.Relative)");
            centerExpression.Target = "Center";

            AnimateGradientAnimations(brush, false);
            var group = compositor.CreateAnimationGroup();
            group.Add(angleAnim);
            group.Add(centerExpression);

            brush?.StartAnimationGroup(group);
        }

        private void ConicBrushStopAnimate_Click(object? sender, RoutedEventArgs e)
        {
            var visual = ElementComposition.GetElementVisual(ConicBrushHost);
            if (visual == null)
                return;

            ConicBrushHost.Background = Brushes.Gray;
        }

        private void AnimateGradientAnimations(CompositionGradientBrush? brush, bool animateOffsets = true)
        {
            if (brush == null)
                return;
            var compositor = brush.Compositor;

            foreach (var stop in brush.GradientStops)
            {
                if (stop is not CompositionGradientStop gs)
                    continue;
                var animations = compositor.CreateAnimationGroup();

                var colorAnim = compositor.CreateColorKeyFrameAnimation();
                colorAnim.Duration = TimeSpan.FromSeconds(4);
                colorAnim.IterationBehavior = AnimationIterationBehavior.Forever;
                colorAnim.InsertKeyFrame(0f, gs.Color);
                colorAnim.InsertKeyFrame(0.5f, ShiftColor(gs.Color));
                colorAnim.InsertKeyFrame(1f, gs.Color);
                colorAnim.Target = "Color";
                animations.Add(colorAnim);

                if (!animateOffsets)
                    continue;
                var offsetAnim = compositor.CreateScalarKeyFrameAnimation();
                offsetAnim.Duration = TimeSpan.FromSeconds(4);
                offsetAnim.IterationBehavior = AnimationIterationBehavior.Forever;
                var o = (float)gs.Offset;
                var o2 = Math.Clamp(gs.Offset + 0.1, 0, 1);
                offsetAnim.InsertKeyFrame(0f, o);
                offsetAnim.InsertKeyFrame(0.5f, (float)o2);
                offsetAnim.InsertKeyFrame(1f, o);
                offsetAnim.Target = "Offset";
                animations.Add(offsetAnim);

                gs.StartAnimationGroup(animations);
            }
        }
        private static Color ShiftColor(Color c)
        {
            var r = (byte)Math.Min(255, (int)(c.R * 1.15) + 30);
            var g = (byte)Math.Max(0, (int)(c.G * 0.75) - 10);
            var b = (byte)Math.Min(255, (int)(c.B * 1.2) + 20);
            return Color.FromArgb(c.A, r, g, b);
        }
    }
}
