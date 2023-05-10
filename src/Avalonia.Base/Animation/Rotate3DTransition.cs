using System;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation;

public class Rotate3DTransition : PageTransition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Rotate3DTransition"/>
    /// </summary>
    public Rotate3DTransition()
        : this(TimeSpan.Zero)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rotate3DTransition"/>
    /// </summary>
    /// <param name="duration">How long the rotation should take place</param>
    /// <param name="orientation">The orientation of the rotation</param>
    /// <param name="depth">Defines the depth of the 3D Effect. If null, depth will be calculated automatically from the width or height of the common parent of the visual being rotated</param>
    public Rotate3DTransition(TimeSpan duration, PageSlideAxis orientation = PageSlideAxis.Horizontal, double? depth = null)
        : base(duration)
    {
        Orientation = orientation;
        Depth = depth;
    }

    /// <summary>
    ///  Defines the depth of the 3D Effect. If null, depth will be calculated automatically from the width or height
    ///  of the common parent of the visual being rotated.
    /// </summary>
    public double? Depth { get; set; }

    /// <summary>
    /// Gets the direction of the animation.
    /// </summary>
    public PageSlideAxis Orientation { get; set; }

    /// <summary>
    /// Gets or sets element entrance easing.
    /// </summary>
    public Easing SlideInEasing { get; set; } = new LinearEasing();

    /// <summary>
    /// Gets or sets element exit easing.
    /// </summary>
    public Easing SlideOutEasing { get; set; } = new LinearEasing();

    protected override Animation GetHideAnimation(Visual? from, Visual? to, bool forward)
    {
        var parent = PageSlide.GetVisualParent(from, to);
        return new Animation
        {
            Easing = SlideOutEasing,
            Duration = Duration,
            Children =
            {
                CreateKeyFrame(parent, 0d, 0d, 2),
                CreateKeyFrame(parent, 0.5d, 45d * (forward ? -1 : 1), 1),
                CreateKeyFrame(parent, 1d, 90d * (forward ? -1 : 1), 1)
            },
        };
    }

    protected override Animation GetShowAnimation(Visual? from, Visual? to, bool forward)
    {
        var parent = PageSlide.GetVisualParent(from, to);
        return new Animation
        {
            Easing = SlideInEasing,
            Duration = Duration,
            Children =
            {
                CreateKeyFrame(parent, 0d, 90d * (forward ? 1 : -1), 1),
                CreateKeyFrame(parent, 0.5d, 45d * (forward ? 1 : -1), 1),
                CreateKeyFrame(parent, 1d, 0d, 2)
            },
        };
    }

    private KeyFrame CreateKeyFrame(Visual parent, double cue, double rotation, int zIndex)
    {
        var (rotateProperty, center) = Orientation switch
        {
            PageSlideAxis.Vertical => (Rotate3DTransform.AngleXProperty, parent.Bounds.Height),
            PageSlideAxis.Horizontal => (Rotate3DTransform.AngleYProperty, parent.Bounds.Width),
            _ => throw new ArgumentOutOfRangeException()
        };

        var depthSetter = new Setter { Property = Rotate3DTransform.DepthProperty, Value = Depth ?? center };
        var centerZSetter = new Setter { Property = Rotate3DTransform.CenterZProperty, Value = -center / 2 };

        return new()
        {
            Setters =
                {
                    new Setter { Property = rotateProperty, Value = rotation },
                    new Setter { Property = Visual.ZIndexProperty, Value = zIndex },
                    centerZSetter,
                    depthSetter
                },
            Cue = new Cue(cue)
        };
    }
}
