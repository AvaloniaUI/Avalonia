using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Example custom IPageTransition: a directional page slide.
    /// Both pages slide together in the specified axis direction.
    /// Demonstrates how to implement a custom horizontal or vertical slide from scratch.
    /// </summary>
    public class PageSlideTransition : IPageTransition
    {
        public enum SlideAxis { Horizontal, Vertical }

        public PageSlideTransition() { }

        public PageSlideTransition(TimeSpan duration, SlideAxis axis = SlideAxis.Horizontal)
        {
            Duration = duration;
            Axis = axis;
        }

        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
        public SlideAxis Axis { get; set; } = SlideAxis.Horizontal;
        public Easing SlideEasing { get; set; } = new LinearEasing();

        public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken) =>
            Axis == SlideAxis.Horizontal
                ? StartAxis(from, to, forward, cancellationToken, TranslateTransform.XProperty, () => GetVisualParent(from, to).Bounds.Width)
                : StartAxis(from, to, forward, cancellationToken, TranslateTransform.YProperty, () => GetVisualParent(from, to).Bounds.Height);

        private async Task StartAxis(
            Visual? from, Visual? to, bool forward, CancellationToken cancellationToken,
            Avalonia.AvaloniaProperty<double> prop, Func<double> getDistance)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            var tasks = new List<Task>();
            var distance = getDistance() is > 0 and var d ? d : 500d;

            if (from != null)
            {
                var anim = new Avalonia.Animation.Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = SlideEasing,
                    Duration = Duration,
                    Children =
                    {
                        new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(prop, 0d) } },
                        new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(prop, forward ? -distance : distance) } }
                    }
                };
                tasks.Add(anim.RunAsync(from, cancellationToken));
            }

            if (to != null)
            {
                to.IsVisible = true;
                var anim = new Avalonia.Animation.Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = SlideEasing,
                    Duration = Duration,
                    Children =
                    {
                        new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(prop, forward ? distance : -distance) } },
                        new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(prop, 0d) } }
                    }
                };
                tasks.Add(anim.RunAsync(to, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (from != null && !cancellationToken.IsCancellationRequested)
                from.IsVisible = false;
        }

        private static Visual GetVisualParent(Visual? from, Visual? to)
        {
            var p1 = (from ?? to)!.GetVisualParent();
            if (from != null && to != null &&
                !ReferenceEquals(from.GetVisualParent(), to.GetVisualParent()))
                throw new ArgumentException("Transition elements have different parents.");
            return p1 ?? throw new ArgumentException("Transition elements have no parent.");
        }
    }
}
