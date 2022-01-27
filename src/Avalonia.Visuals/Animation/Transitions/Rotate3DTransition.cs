using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation;

public class Rotate3DTransition: PageSlide
{
    
    /// <summary>
    ///  Creates a new instance if the <see cref="Rotate3DTransition"/>
    /// </summary>
    /// <param name="duration">How long the rotation should take place</param>
    /// <param name="orientation">The orientation of the rotation</param>
    public Rotate3DTransition(TimeSpan duration, SlideAxis orientation = SlideAxis.Horizontal) 
        : base(duration, orientation)
    {}

    /// <summary>
    ///  Creates a new instance if the <see cref="Rotate3DTransition"/>
    /// </summary>
    public Rotate3DTransition() { }

    /// <inheritdoc />
    public override async Task Start(Visual? @from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var tasks = new List<Task>();
        var parent = GetVisualParent(from, to);
        var (rotateProperty, center) = Orientation switch
        {
            SlideAxis.Vertical => (Rotate3DTransform.AngleXProperty, parent.Bounds.Height),
            SlideAxis.Horizontal => (Rotate3DTransform.AngleYProperty, parent.Bounds.Width),
            _ => throw new ArgumentOutOfRangeException()
        };

        var depthSetter = new Setter {Property = Rotate3DTransform.DepthProperty, Value = center};
        var centerZSetter = new Setter {Property = Rotate3DTransform.CenterZProperty, Value = -center / 2};

        if (from != null)
        {
            var animation = new Animation
            {
                Duration = Duration,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter { Property = rotateProperty, Value = 0d },
                            new Setter { Property = Visual.ZIndexProperty, Value = 2 },
                            centerZSetter,
                            depthSetter,
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter { Property = rotateProperty, Value = 45d * (forward ? -1 : 1) },
                            new Setter { Property = Visual.ZIndexProperty, Value = 1 },
                            centerZSetter,
                            depthSetter
                        },
                        Cue = new Cue(0.5d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter { Property = rotateProperty, Value = 90d * (forward ? -1 : 1) },
                            new Setter { Property = Visual.ZIndexProperty, Value = 1 },
                            centerZSetter,
                            depthSetter
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            tasks.Add(animation.RunAsync(from, null, cancellationToken));
        }

        if (to != null)
        {
            to.IsVisible = true;
            var animation = new Animation
            {
                Duration = Duration,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter { Property = rotateProperty, Value = 90d * (forward ? 1 : -1) },
                            new Setter { Property = Visual.ZIndexProperty, Value = 1 },
                            centerZSetter,
                            depthSetter
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter { Property = Visual.ZIndexProperty, Value = 1 },
                            new Setter { Property = rotateProperty, Value = 45d * (forward ? 1 : -1) },
                            centerZSetter,
                            depthSetter
                        },
                        Cue = new Cue(0.5d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter { Property = rotateProperty, Value = 0d },
                            new Setter { Property = Visual.ZIndexProperty, Value = 2 },
                            centerZSetter,
                            depthSetter,
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            tasks.Add(animation.RunAsync(to, null, cancellationToken));
        }

        await Task.WhenAll(tasks);

        if (from != null && !cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
            from.ZIndex = 1;
        }

        if (to != null && !cancellationToken.IsCancellationRequested)
        {
            to.ZIndex = 2;
        }
    }
}
