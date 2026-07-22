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
    /// Example custom IPageTransition: horizontal slide combined with cross-fade.
    /// Both pages slide and fade simultaneously for a smooth blended effect.
    /// </summary>
    public class CompositeTransition : IPageTransition
    {
        public CompositeTransition() { }

        public CompositeTransition(TimeSpan duration)
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
        public Easing TransitionEasing { get; set; } = new LinearEasing();

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
                    Easing = TransitionEasing,
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
                                new Setter(TranslateTransform.XProperty, forward ? -distance : distance),
                                new Setter(Visual.OpacityProperty, 0d)
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
                    Easing = TransitionEasing,
                    Duration = Duration,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(TranslateTransform.XProperty, forward ? distance : -distance),
                                new Setter(Visual.OpacityProperty, 0d)
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
