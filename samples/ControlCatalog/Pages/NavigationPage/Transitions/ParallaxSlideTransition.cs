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
    /// Example custom IPageTransition: a parallax slide.
    /// The incoming page slides full-width from the right while the outgoing page shifts ~30%
    /// to the left with a subtle opacity fade, producing a depth-layered push effect.
    /// </summary>
    public class ParallaxSlideTransition : IPageTransition
    {
        public ParallaxSlideTransition() { }

        public ParallaxSlideTransition(TimeSpan duration)
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(350);
        public Easing SlideEasing { get; set; } = new CubicEaseOut();

        public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var tasks = new List<Task>();
            var parent = GetVisualParent(from, to);
            var distance = parent.Bounds.Width > 0 ? parent.Bounds.Width : 500d;

            if (from != null)
            {
                var anim = new Avalonia.Animation.Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = SlideEasing,
                    Duration = Duration,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(TranslateTransform.XProperty, 0d),
                                new Setter(Visual.OpacityProperty, 1d)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters =
                            {
                                new Setter(TranslateTransform.XProperty, forward ? -distance * 0.3 : distance),
                                new Setter(Visual.OpacityProperty, forward ? 0.7 : 1d)
                            }
                        }
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
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(TranslateTransform.XProperty, forward ? distance : -distance * 0.3),
                                new Setter(Visual.OpacityProperty, forward ? 1d : 0.7)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters =
                            {
                                new Setter(TranslateTransform.XProperty, 0d),
                                new Setter(Visual.OpacityProperty, 1d)
                            }
                        }
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
