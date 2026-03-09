using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation;

public class Rotate3DTransition : PageSlide
{
    /// <summary>
    ///  Creates a new instance of the <see cref="Rotate3DTransition"/>
    /// </summary>
    /// <param name="duration">How long the rotation should take place</param>
    /// <param name="orientation">The orientation of the rotation</param>
    /// <param name="depth">Defines the depth of the 3D Effect. If null, depth will be calculated automatically from the width or height of the common parent of the visual being rotated</param>
    public Rotate3DTransition(TimeSpan duration, SlideAxis orientation = SlideAxis.Horizontal, double? depth = null)
        : base(duration, orientation)
    {
        Depth = depth;
    }

    /// <summary>
    ///  Defines the depth of the 3D Effect. If null, depth will be calculated automatically from the width or height
    ///  of the common parent of the visual being rotated.
    /// </summary>
    public double? Depth { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rotate3DTransition"/> class.
    /// </summary>
    public Rotate3DTransition() { }

    /// <inheritdoc />
    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var tasks = new Task[from != null && to != null ? 2 : 1];
        var parent = GetVisualParent(from, to);
        var (rotateProperty, center) = Orientation switch
        {
            SlideAxis.Vertical => (Rotate3DTransform.AngleXProperty, parent.Bounds.Height),
            SlideAxis.Horizontal => (Rotate3DTransform.AngleYProperty, parent.Bounds.Width),
            _ => throw new ArgumentOutOfRangeException()
        };

        var depthSetter = new Setter { Property = Rotate3DTransform.DepthProperty, Value = Depth ?? center };
        var centerZSetter = new Setter { Property = Rotate3DTransform.CenterZProperty, Value = -center / 2 };

        KeyFrame CreateKeyFrame(double cue, double rotation, int zIndex, bool isVisible = true) =>
            new()
            {
                Setters =
                {
                    new Setter { Property = rotateProperty, Value = rotation },
                    new Setter { Property = Visual.ZIndexProperty, Value = zIndex },
                    new Setter { Property = Visual.IsVisibleProperty, Value = isVisible },
                    centerZSetter,
                    depthSetter
                },
                Cue = new Cue(cue)
            };

        if (from != null)
        {
            var animation = new Animation
            {
                Easing = SlideOutEasing,
                Duration = Duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    CreateKeyFrame(0d, 0d, 2),
                    CreateKeyFrame(0.5d, 45d * (forward ? -1 : 1), 1),
                    CreateKeyFrame(1d, 90d * (forward ? -1 : 1), 1, isVisible: false)
                }
            };

            tasks[0] = animation.RunAsync(from, null, cancellationToken);
        }

        if (to != null)
        {
            to.IsVisible = true;
            var animation = new Animation
            {
                Easing = SlideInEasing,
                Duration = Duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    CreateKeyFrame(0d, 90d * (forward ? 1 : -1), 1),
                    CreateKeyFrame(0.5d, 45d * (forward ? 1 : -1), 1),
                    CreateKeyFrame(1d, 0d, 2)
                }
            };

            tasks[from != null ? 1 : 0] = animation.RunAsync(to, null, cancellationToken);
        }

        await Task.WhenAll(tasks);

        if (!cancellationToken.IsCancellationRequested)
        {
            if (to != null)
                to.ZIndex = 2;

            if (from != null)
            {
                from.IsVisible = false;
                from.ZIndex = 1;
            }
        }
    }

    /// <inheritdoc/>
    public override void Update(double progress, Visual? from, Visual? to, bool forward)
    {
        var parent = GetVisualParent(from, to);
        var center = Orientation == SlideAxis.Vertical ? parent.Bounds.Height : parent.Bounds.Width;
        var depth = Depth ?? center;
        var sign = forward ? 1.0 : -1.0;

        if (from != null)
        {
            if (from.RenderTransform is not Rotate3DTransform ft)
            {
                from.RenderTransform = ft = new Rotate3DTransform();
            }

            ft.Depth = depth;
            ft.CenterZ = -center / 2;
            from.ZIndex = progress < 0.5 ? 2 : 1;
            if (Orientation == SlideAxis.Horizontal)
            {
                ft.AngleY = -sign * 90.0 * progress;
            }
            else
            {
                ft.AngleX = -sign * 90.0 * progress;
            }
        }

        if (to != null)
        {
            to.IsVisible = true;
            if (to.RenderTransform is not Rotate3DTransform tt)
            {
                to.RenderTransform = tt = new Rotate3DTransform();
            }

            tt.Depth = depth;
            tt.CenterZ = -center / 2;
            to.ZIndex = progress < 0.5 ? 1 : 2;
            if (Orientation == SlideAxis.Horizontal)
            {
                tt.AngleY = sign * 90.0 * (1.0 - progress);
            }
            else
            {
                tt.AngleX = sign * 90.0 * (1.0 - progress);
            }
        }
    }
}
