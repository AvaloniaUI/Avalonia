using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation;

public class Rotate3DTransition: PageSlide
{
    private const double SidePeekAngle = 24.0;
    private const double FarPeekAngle = 38.0;

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
                FillMode = FillMode,
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
                FillMode = FillMode,
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
    public override void Update(
        double progress,
        Visual? from,
        Visual? to,
        bool forward,
        double pageLength,
        IReadOnlyList<PageTransitionItem> visibleItems)
    {
        if (visibleItems.Count > 0)
        {
            UpdateVisibleItems(progress, from, to, pageLength, visibleItems);
            return;
        }

        if (from is null && to is null)
            return;

        var parent = GetVisualParent(from, to);
        var center = pageLength > 0
            ? pageLength
            : (Orientation == SlideAxis.Vertical ? parent.Bounds.Height : parent.Bounds.Width);
        var depth = Depth ?? center;
        var sign = forward ? 1.0 : -1.0;

        if (from != null)
        {
            if (from.RenderTransform is not Rotate3DTransform ft)
                from.RenderTransform = ft = new Rotate3DTransform();
            ft.Depth = depth;
            ft.CenterZ = -center / 2;
            from.ZIndex = progress < 0.5 ? 2 : 1;
            if (Orientation == SlideAxis.Horizontal)
                ft.AngleY = -sign * 90.0 * progress;
            else
                ft.AngleX = -sign * 90.0 * progress;
        }

        if (to != null)
        {
            to.IsVisible = true;
            if (to.RenderTransform is not Rotate3DTransform tt)
                to.RenderTransform = tt = new Rotate3DTransform();
            tt.Depth = depth;
            tt.CenterZ = -center / 2;
            to.ZIndex = progress < 0.5 ? 1 : 2;
            if (Orientation == SlideAxis.Horizontal)
                tt.AngleY = sign * 90.0 * (1.0 - progress);
            else
                tt.AngleX = sign * 90.0 * (1.0 - progress);
        }
    }

    private void UpdateVisibleItems(
        double progress,
        Visual? from,
        Visual? to,
        double pageLength,
        IReadOnlyList<PageTransitionItem> visibleItems)
    {
        var anchor = from ?? to ?? visibleItems[0].Visual;
        if (anchor.VisualParent is not Visual parent)
            return;

        var center = pageLength > 0
            ? pageLength
            : (Orientation == SlideAxis.Vertical ? parent.Bounds.Height : parent.Bounds.Width);
        var depth = Depth ?? center;
        var angleStrength = Math.Sin(Math.Clamp(progress, 0.0, 1.0) * Math.PI);

        foreach (var item in visibleItems)
        {
            var visual = item.Visual;
            visual.IsVisible = true;
            visual.ZIndex = GetZIndex(item.ViewportCenterOffset);

            if (visual.RenderTransform is not Rotate3DTransform transform)
                visual.RenderTransform = transform = new Rotate3DTransform();

            transform.Depth = depth;
            transform.CenterZ = -center / 2;

            var angle = GetAngleForOffset(item.ViewportCenterOffset) * angleStrength;
            if (Orientation == SlideAxis.Horizontal)
            {
                transform.AngleY = angle;
                transform.AngleX = 0;
            }
            else
            {
                transform.AngleX = angle;
                transform.AngleY = 0;
            }
        }
    }

    private static double GetAngleForOffset(double offsetFromCenter)
    {
        var sign = Math.Sign(offsetFromCenter);
        if (sign == 0)
            return 0;

        var distance = Math.Abs(offsetFromCenter);
        if (distance <= 1.0)
            return sign * Lerp(0.0, SidePeekAngle, distance);

        if (distance <= 2.0)
            return sign * Lerp(SidePeekAngle, FarPeekAngle, distance - 1.0);

        return sign * FarPeekAngle;
    }

    private static int GetZIndex(double offsetFromCenter)
    {
        var distance = Math.Abs(offsetFromCenter);

        if (distance < 0.5)
            return 3;
        if (distance < 1.5)
            return 2;
        return 1;
    }

    private static double Lerp(double from, double to, double t)
    {
        return from + ((to - from) * Math.Clamp(t, 0.0, 1.0));
    }

    /// <inheritdoc/>
    public override void Reset(Visual visual)
    {
        visual.RenderTransform = null;
        visual.ZIndex = 0;
    }
}
