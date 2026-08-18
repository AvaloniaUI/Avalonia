using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Example custom IPageTransition: a "fade through" with scale.
    /// The outgoing page fades out while scaling down; the incoming page fades in while
    /// scaling up, producing a smooth depth-aware transition.
    /// </summary>
    public class FadeThroughTransition : IPageTransition
    {
        public FadeThroughTransition() { }

        public FadeThroughTransition(TimeSpan duration)
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
        public Easing FadeEasing { get; set; } = new CubicEaseOut();

        public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var tasks = new List<Task>();

            if (from != null)
            {
                from.RenderTransformOrigin = RelativePoint.Center;
                from.RenderTransform = new ScaleTransform(1, 1);
            }

            if (to != null)
            {
                to.RenderTransformOrigin = RelativePoint.Center;
                to.RenderTransform = new ScaleTransform(1, 1);
                to.Opacity = 0;
            }

            if (from != null)
            {
                var outAnim = new Avalonia.Animation.Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = FadeEasing,
                    Duration = Duration,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(Visual.OpacityProperty, 1d),
                                new Setter(ScaleTransform.ScaleXProperty, 1d),
                                new Setter(ScaleTransform.ScaleYProperty, 1d)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters =
                            {
                                new Setter(Visual.OpacityProperty, 0d),
                                new Setter(ScaleTransform.ScaleXProperty, forward ? 0.92 : 1.08),
                                new Setter(ScaleTransform.ScaleYProperty, forward ? 0.92 : 1.08)
                            }
                        }
                    }
                };
                tasks.Add(outAnim.RunAsync(from, cancellationToken));
            }

            if (to != null)
            {
                to.IsVisible = true;

                var inAnim = new Avalonia.Animation.Animation
                {
                    FillMode = FillMode.Forward,
                    Easing = FadeEasing,
                    Duration = Duration,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(Visual.OpacityProperty, 0d),
                                new Setter(ScaleTransform.ScaleXProperty, forward ? 1.08 : 0.92),
                                new Setter(ScaleTransform.ScaleYProperty, forward ? 1.08 : 0.92)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters =
                            {
                                new Setter(Visual.OpacityProperty, 1d),
                                new Setter(ScaleTransform.ScaleXProperty, 1d),
                                new Setter(ScaleTransform.ScaleYProperty, 1d)
                            }
                        }
                    }
                };
                tasks.Add(inAnim.RunAsync(to, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (to != null && !cancellationToken.IsCancellationRequested)
            {
                to.Opacity = 1;
                to.RenderTransform = null;
            }

            if (from != null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    from.IsVisible = false;
                from.Opacity = 1;
                from.RenderTransform = null;
            }
        }
    }
}
