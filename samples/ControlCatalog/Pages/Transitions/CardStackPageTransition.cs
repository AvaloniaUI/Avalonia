using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace ControlCatalog.Pages.Transitions;

/// <summary>
/// Transitions between two pages with a card-stack effect:
/// the top page moves/rotates away while the next page scales up underneath.
/// </summary>
public class CardStackPageTransition : PageSlide
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CardStackPageTransition"/> class.
    /// </summary>
    public CardStackPageTransition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CardStackPageTransition"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="orientation">The axis on which the animation should occur.</param>
    public CardStackPageTransition(TimeSpan duration, PageSlide.SlideAxis orientation = PageSlide.SlideAxis.Horizontal)
        : base(duration, orientation)
    {
    }

    /// <summary>
    /// Gets or sets the maximum rotation angle (degrees) applied to the top card.
    /// </summary>
    public double MaxSwipeAngle { get; set; } = 15.0;

    /// <summary>
    /// Gets or sets the scale reduction applied to the back card (0.05 = 5%).
    /// </summary>
    public double BackCardScale { get; set; } = 0.05;

    /// <summary>
    /// Gets or sets the vertical offset (pixels) applied to the back card.
    /// </summary>
    public double BackCardOffset { get; set; } = 0.0;

    /// <inheritdoc />
    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var tasks = new List<Task>();
        var parent = GetVisualParent(from, to);
        var distance = Orientation == PageSlide.SlideAxis.Horizontal ? parent.Bounds.Width : parent.Bounds.Height;
        var translateProperty = Orientation == PageSlide.SlideAxis.Horizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty;
        var rotationTarget = Orientation == PageSlide.SlideAxis.Horizontal ? (forward ? -MaxSwipeAngle : MaxSwipeAngle) : 0.0;
        var startScale = 1.0 - BackCardScale;

        if (from != null)
        {
            var (rotate, translate) = EnsureTopTransforms(from);
            rotate.Angle = 0;
            translate.X = 0;
            translate.Y = 0;
            from.Opacity = 1;
            from.ZIndex = 1;

            var animation = new Animation
            {
                Easing = SlideOutEasing,
                Duration = Duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(translateProperty, 0d),
                            new Setter(RotateTransform.AngleProperty, 0d)
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(translateProperty, forward ? -distance : distance),
                            new Setter(RotateTransform.AngleProperty, rotationTarget)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };
            tasks.Add(animation.RunAsync(from, cancellationToken));
        }

        if (to != null)
        {
            var (scale, translate) = EnsureBackTransforms(to);
            scale.ScaleX = startScale;
            scale.ScaleY = startScale;
            translate.X = 0;
            translate.Y = BackCardOffset;
            to.IsVisible = true;
            to.Opacity = 1;
            to.ZIndex = 0;

            var animation = new Animation
            {
                Easing = SlideInEasing,
                Duration = Duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, startScale),
                            new Setter(ScaleTransform.ScaleYProperty, startScale),
                            new Setter(TranslateTransform.YProperty, BackCardOffset)
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 1d),
                            new Setter(ScaleTransform.ScaleYProperty, 1d),
                            new Setter(TranslateTransform.YProperty, 0d)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            tasks.Add(animation.RunAsync(to, cancellationToken));
        }

        await Task.WhenAll(tasks);

        if (from != null && !cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
        }

        if (!cancellationToken.IsCancellationRequested && to != null)
        {
            var (scale, translate) = EnsureBackTransforms(to);
            scale.ScaleX = 1;
            scale.ScaleY = 1;
            translate.X = 0;
            translate.Y = 0;
        }
    }

    /// <inheritdoc />
    public override void Update(double progress, Visual? from, Visual? to, bool forward, PageSlide.SlideAxis orientation, Size size)
    {
        var isHorizontal = orientation == PageSlide.SlideAxis.Horizontal;
        var distance = isHorizontal ? size.Width : size.Height;
        var rotationTarget = isHorizontal ? (forward ? -MaxSwipeAngle : MaxSwipeAngle) : 0.0;
        var startScale = 1.0 - BackCardScale;

        if (from != null)
        {
            var (rotate, translate) = EnsureTopTransforms(from);
            if (isHorizontal)
            {
                translate.X = forward ? -distance * progress : distance * progress;
                translate.Y = 0;
            }
            else
            {
                translate.X = 0;
                translate.Y = forward ? -distance * progress : distance * progress;
            }

            rotate.Angle = rotationTarget * progress;
            from.IsVisible = progress < 1.0;
            from.Opacity = 1;
            from.ZIndex = 1;
        }

        if (to != null)
        {
            var (scale, translate) = EnsureBackTransforms(to);
            var currentScale = startScale + (1.0 - startScale) * progress;
            var currentOffset = BackCardOffset * (1.0 - progress);

            scale.ScaleX = currentScale;
            scale.ScaleY = currentScale;
            translate.X = 0;
            translate.Y = currentOffset;

            to.IsVisible = true;
            to.Opacity = 1;
            to.ZIndex = 0;
        }
    }

    private static (RotateTransform rotate, TranslateTransform translate) EnsureTopTransforms(Visual visual)
    {
        if (visual.RenderTransform is TransformGroup group &&
            group.Children.Count == 2 &&
            group.Children[0] is RotateTransform rotateTransform &&
            group.Children[1] is TranslateTransform translateTransform)
        {
            visual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            return (rotateTransform, translateTransform);
        }

        var rotate = new RotateTransform();
        var translate = new TranslateTransform();
        visual.RenderTransform = new TransformGroup
        {
            Children =
            {
                rotate,
                translate
            }
        };
        visual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        return (rotate, translate);
    }

    private static (ScaleTransform scale, TranslateTransform translate) EnsureBackTransforms(Visual visual)
    {
        if (visual.RenderTransform is TransformGroup group &&
            group.Children.Count == 2 &&
            group.Children[0] is ScaleTransform scaleTransform &&
            group.Children[1] is TranslateTransform translateTransform)
        {
            visual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            return (scaleTransform, translateTransform);
        }

        var scale = new ScaleTransform();
        var translate = new TranslateTransform();
        visual.RenderTransform = new TransformGroup
        {
            Children =
            {
                scale,
                translate
            }
        };
        visual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        return (scale, translate);
    }
}
