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
    private const double ViewportLiftScale = 0.03;
    private const double ViewportPromotionScale = 0.02;
    private const double ViewportDepthOpacityFalloff = 0.08;
    private const double SidePeekAngle = 4.0;
    private const double FarPeekAngle = 7.0;

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
            UpdateVisibleItems(progress, from, to, forward, pageLength, visibleItems);
            return;
        }

        if (from is null && to is null)
            return;

        var parent = GetVisualParent(from, to);
        var size = parent.Bounds.Size;
        var isHorizontal = Orientation == PageSlide.SlideAxis.Horizontal;
        var distance = pageLength > 0
            ? pageLength
            : (isHorizontal ? size.Width : size.Height);
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
            from.IsVisible = true;
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
            if (isHorizontal)
            {
                translate.X = 0;
                translate.Y = currentOffset;
            }
            else
            {
                translate.X = currentOffset;
                translate.Y = 0;
            }

            to.IsVisible = true;
            to.Opacity = 1;
            to.ZIndex = 0;
        }
    }

    /// <inheritdoc />
    public override void Reset(Visual visual)
    {
        visual.RenderTransform = null;
        visual.RenderTransformOrigin = default;
        visual.Opacity = 1;
        visual.ZIndex = 0;
    }

    private void UpdateVisibleItems(
        double progress,
        Visual? from,
        Visual? to,
        bool forward,
        double pageLength,
        IReadOnlyList<PageTransitionItem> visibleItems)
    {
        var isHorizontal = Orientation == PageSlide.SlideAxis.Horizontal;
        var rotationTarget = isHorizontal
            ? (forward ? -MaxSwipeAngle : MaxSwipeAngle)
            : 0.0;
        var stackOffset = GetViewportStackOffset(pageLength);
        var lift = Math.Sin(Math.Clamp(progress, 0.0, 1.0) * Math.PI);

        foreach (var item in visibleItems)
        {
            var visual = item.Visual;
            var (rotate, scale, translate) = EnsureViewportTransforms(visual);
            var depth = GetViewportDepth(item.ViewportCenterOffset);
            var scaleValue = Math.Max(0.84, 1.0 - (BackCardScale * depth));
            var stackValue = stackOffset * depth;
            var baseOpacity = Math.Max(0.8, 1.0 - (ViewportDepthOpacityFalloff * depth));
            var restingAngle = isHorizontal ? GetViewportRestingAngle(item.ViewportCenterOffset) : 0.0;

            rotate.Angle = restingAngle;
            scale.ScaleX = scaleValue;
            scale.ScaleY = scaleValue;
            translate.X = 0;
            translate.Y = 0;

            if (ReferenceEquals(visual, from))
            {
                rotate.Angle = restingAngle + (rotationTarget * progress);
                stackValue -= stackOffset * 0.2 * lift;
                baseOpacity = Math.Min(1.0, baseOpacity + 0.08);
            }

            if (ReferenceEquals(visual, to))
            {
                var promotedScale = Math.Min(1.0, scaleValue + (ViewportLiftScale * lift) + (ViewportPromotionScale * progress));
                scale.ScaleX = promotedScale;
                scale.ScaleY = promotedScale;
                rotate.Angle = restingAngle * (1.0 - progress);
                stackValue = Math.Max(0.0, stackValue - (stackOffset * (0.45 + (0.2 * lift)) * progress));
                baseOpacity = Math.Min(1.0, baseOpacity + (0.12 * lift));
            }

            if (isHorizontal)
                translate.Y = stackValue;
            else
                translate.X = stackValue;

            visual.IsVisible = true;
            visual.Opacity = baseOpacity;
            visual.ZIndex = GetViewportZIndex(item.ViewportCenterOffset, visual, from, to);
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

    private static (RotateTransform rotate, ScaleTransform scale, TranslateTransform translate) EnsureViewportTransforms(Visual visual)
    {
        if (visual.RenderTransform is TransformGroup group &&
            group.Children.Count == 3 &&
            group.Children[0] is RotateTransform rotateTransform &&
            group.Children[1] is ScaleTransform scaleTransform &&
            group.Children[2] is TranslateTransform translateTransform)
        {
            visual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            return (rotateTransform, scaleTransform, translateTransform);
        }

        var rotate = new RotateTransform();
        var scale = new ScaleTransform(1, 1);
        var translate = new TranslateTransform();
        visual.RenderTransform = new TransformGroup
        {
            Children =
            {
                rotate,
                scale,
                translate
            }
        };
        visual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        return (rotate, scale, translate);
    }

    private double GetViewportStackOffset(double pageLength)
    {
        if (BackCardOffset > 0)
            return BackCardOffset;

        return Math.Clamp(pageLength * 0.045, 10.0, 18.0);
    }

    private static double GetViewportDepth(double offsetFromCenter)
    {
        var distance = Math.Abs(offsetFromCenter);

        if (distance <= 1.0)
            return distance;

        if (distance <= 2.0)
            return 1.0 + ((distance - 1.0) * 0.8);

        return 1.8;
    }

    private static double GetViewportRestingAngle(double offsetFromCenter)
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

    private static double Lerp(double from, double to, double t)
    {
        return from + ((to - from) * Math.Clamp(t, 0.0, 1.0));
    }

    private static int GetViewportZIndex(double offsetFromCenter, Visual visual, Visual? from, Visual? to)
    {
        if (ReferenceEquals(visual, from))
            return 5;

        if (ReferenceEquals(visual, to))
            return 4;

        var distance = Math.Abs(offsetFromCenter);
        if (distance < 0.5)
            return 4;
        if (distance < 1.5)
            return 3;
        return 2;
    }
}
