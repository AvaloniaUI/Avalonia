using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace ControlCatalog.Pages.Transitions;

/// <summary>
/// Transitions between two pages using a wave clip that reveals the next page.
/// </summary>
public class WaveRevealPageTransition : PageSlide
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WaveRevealPageTransition"/> class.
    /// </summary>
    public WaveRevealPageTransition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveRevealPageTransition"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="orientation">The axis on which the animation should occur.</param>
    public WaveRevealPageTransition(TimeSpan duration, PageSlide.SlideAxis orientation = PageSlide.SlideAxis.Horizontal)
        : base(duration, orientation)
    {
    }

    /// <summary>
    /// Gets or sets the maximum wave bulge (pixels) along the movement axis.
    /// </summary>
    public double MaxBulge { get; set; } = 120.0;

    /// <summary>
    /// Gets or sets the bulge factor along the movement axis (0-1).
    /// </summary>
    public double BulgeFactor { get; set; } = 0.35;

    /// <summary>
    /// Gets or sets the bulge factor along the cross axis (0-1).
    /// </summary>
    public double CrossBulgeFactor { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets a cross-axis offset (pixels) to shift the wave center.
    /// </summary>
    public double WaveCenterOffset { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets how strongly the wave center follows the provided offset.
    /// </summary>
    public double CenterSensitivity { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the bulge exponent used to shape the wave (1.0 = linear).
    /// Higher values tighten the bulge; lower values broaden it.
    /// </summary>
    public double BulgeExponent { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the easing applied to the wave progress (clip only).
    /// </summary>
    public Easing WaveEasing { get; set; } = new CubicEaseOut();

    /// <inheritdoc />
    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (to != null)
        {
            to.IsVisible = true;
            to.ZIndex = 1;
        }

        if (from != null)
        {
            from.ZIndex = 0;
        }

        await AnimateProgress(0.0, 1.0, from, to, forward, cancellationToken);

        if (to != null && !cancellationToken.IsCancellationRequested)
        {
            to.Clip = null;
        }

        if (from != null && !cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
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
            UpdateVisibleItems(from, to, forward, pageLength, visibleItems);
            return;
        }

        if (from is null && to is null)
            return;
        var parent = GetVisualParent(from, to);
        var size = parent.Bounds.Size;
        var centerOffset = WaveCenterOffset * CenterSensitivity;
        var isHorizontal = Orientation == PageSlide.SlideAxis.Horizontal;

        if (to != null)
        {
            to.IsVisible = progress > 0.0;
            to.ZIndex = 1;
            to.Opacity = 1;

            if (progress >= 1.0)
            {
                to.Clip = null;
            }
            else
            {
                var waveProgress = WaveEasing?.Ease(progress) ?? progress;
                var clip = LiquidSwipeClipper.CreateWavePath(
                    waveProgress,
                    size,
                    centerOffset,
                    forward,
                    isHorizontal,
                    MaxBulge,
                    BulgeFactor,
                    CrossBulgeFactor,
                    BulgeExponent);
                to.Clip = clip;
            }
        }

        if (from != null)
        {
            from.IsVisible = true;
            from.ZIndex = 0;
            from.Opacity = 1;
        }
    }

    private void UpdateVisibleItems(
        Visual? from,
        Visual? to,
        bool forward,
        double pageLength,
        IReadOnlyList<PageTransitionItem> visibleItems)
    {
        if (from is null && to is null)
            return;

        var parent = GetVisualParent(from, to);
        var size = parent.Bounds.Size;
        var centerOffset = WaveCenterOffset * CenterSensitivity;
        var isHorizontal = Orientation == PageSlide.SlideAxis.Horizontal;
        var resolvedPageLength = pageLength > 0
            ? pageLength
            : (isHorizontal ? size.Width : size.Height);
        foreach (var item in visibleItems)
        {
            var visual = item.Visual;
            visual.IsVisible = true;
            visual.Opacity = 1;
            visual.Clip = null;
            visual.ZIndex = ReferenceEquals(visual, to) ? 1 : 0;

            if (!ReferenceEquals(visual, to))
                continue;

            var visibleFraction = GetVisibleFraction(item.ViewportCenterOffset, size, resolvedPageLength, isHorizontal);
            if (visibleFraction >= 1.0)
                continue;

            visual.Clip = LiquidSwipeClipper.CreateWavePath(
                visibleFraction,
                size,
                centerOffset,
                forward,
                isHorizontal,
                MaxBulge,
                BulgeFactor,
                CrossBulgeFactor,
                BulgeExponent);
        }
    }

    private static double GetVisibleFraction(double offsetFromCenter, Size viewportSize, double pageLength, bool isHorizontal)
    {
        if (pageLength <= 0)
            return 1.0;

        var viewportLength = isHorizontal ? viewportSize.Width : viewportSize.Height;
        if (viewportLength <= 0)
            return 0.0;

        var viewportUnits = viewportLength / pageLength;
        var edgePeek = Math.Max(0.0, (viewportUnits - 1.0) / 2.0);
        return Math.Clamp(1.0 + edgePeek - Math.Abs(offsetFromCenter), 0.0, 1.0);
    }

    /// <inheritdoc />
    public override void Reset(Visual visual)
    {
        visual.Clip = null;
        visual.ZIndex = 0;
        visual.Opacity = 1;
    }

    private async Task AnimateProgress(
        double from,
        double to,
        Visual? fromVisual,
        Visual? toVisual,
        bool forward,
        CancellationToken cancellationToken)
    {
        var parent = GetVisualParent(fromVisual, toVisual);
        var pageLength = Orientation == PageSlide.SlideAxis.Horizontal
            ? parent.Bounds.Width
            : parent.Bounds.Height;
        var durationMs = Math.Max(Duration.TotalMilliseconds * Math.Abs(to - from), 50);
        var startTicks = Stopwatch.GetTimestamp();
        var tickFreq = Stopwatch.Frequency;

        while (!cancellationToken.IsCancellationRequested)
        {
            var elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / tickFreq;
            var t = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
            var eased = SlideInEasing?.Ease(t) ?? t;
            var progress = from + (to - from) * eased;

            Update(progress, fromVisual, toVisual, forward, pageLength, Array.Empty<PageTransitionItem>());

            if (t >= 1.0)
                break;

            await Task.Delay(16, cancellationToken);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            Update(to, fromVisual, toVisual, forward, pageLength, Array.Empty<PageTransitionItem>());
        }
    }

    private static class LiquidSwipeClipper
    {
        public static Geometry CreateWavePath(
            double progress,
            Size size,
            double waveCenterOffset,
            bool forward,
            bool isHorizontal,
            double maxBulge,
            double bulgeFactor,
            double crossBulgeFactor,
            double bulgeExponent)
        {
            var width = size.Width;
            var height = size.Height;

            if (progress <= 0)
                return new RectangleGeometry(new Rect(0, 0, 0, 0));

            if (progress >= 1)
                return new RectangleGeometry(new Rect(0, 0, width, height));

            if (width <= 0 || height <= 0)
                return new RectangleGeometry(new Rect(0, 0, 0, 0));

            var mainLength = isHorizontal ? width : height;
            var crossLength = isHorizontal ? height : width;

            var wavePhase = Math.Sin(progress * Math.PI);
            var bulgeProgress = bulgeExponent == 1.0 ? wavePhase : Math.Pow(wavePhase, bulgeExponent);
            var revealedLength = mainLength * progress;
            var bulgeMain = Math.Min(mainLength * bulgeFactor, maxBulge) * bulgeProgress;
            bulgeMain = Math.Min(bulgeMain, revealedLength * 0.45);
            var bulgeCross = crossLength * crossBulgeFactor;

            var waveCenter = crossLength / 2 + waveCenterOffset;
            waveCenter = Math.Clamp(waveCenter, bulgeCross, crossLength - bulgeCross);

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                if (isHorizontal)
                {
                    if (forward)
                    {
                        var waveX = width * (1 - progress);
                        context.BeginFigure(new Point(width, 0), true);
                        context.LineTo(new Point(waveX, 0));
                        context.CubicBezierTo(
                            new Point(waveX, waveCenter - bulgeCross),
                            new Point(waveX - bulgeMain, waveCenter - bulgeCross * 0.5),
                            new Point(waveX - bulgeMain, waveCenter));
                        context.CubicBezierTo(
                            new Point(waveX - bulgeMain, waveCenter + bulgeCross * 0.5),
                            new Point(waveX, waveCenter + bulgeCross),
                            new Point(waveX, height));
                        context.LineTo(new Point(width, height));
                        context.EndFigure(true);
                    }
                    else
                    {
                        var waveX = width * progress;
                        context.BeginFigure(new Point(0, 0), true);
                        context.LineTo(new Point(waveX, 0));
                        context.CubicBezierTo(
                            new Point(waveX, waveCenter - bulgeCross),
                            new Point(waveX + bulgeMain, waveCenter - bulgeCross * 0.5),
                            new Point(waveX + bulgeMain, waveCenter));
                        context.CubicBezierTo(
                            new Point(waveX + bulgeMain, waveCenter + bulgeCross * 0.5),
                            new Point(waveX, waveCenter + bulgeCross),
                            new Point(waveX, height));
                        context.LineTo(new Point(0, height));
                        context.EndFigure(true);
                    }
                }
                else
                {
                    if (forward)
                    {
                        var waveY = height * (1 - progress);
                        context.BeginFigure(new Point(0, height), true);
                        context.LineTo(new Point(0, waveY));
                        context.CubicBezierTo(
                            new Point(waveCenter - bulgeCross, waveY),
                            new Point(waveCenter - bulgeCross * 0.5, waveY - bulgeMain),
                            new Point(waveCenter, waveY - bulgeMain));
                        context.CubicBezierTo(
                            new Point(waveCenter + bulgeCross * 0.5, waveY - bulgeMain),
                            new Point(waveCenter + bulgeCross, waveY),
                            new Point(width, waveY));
                        context.LineTo(new Point(width, height));
                        context.EndFigure(true);
                    }
                    else
                    {
                        var waveY = height * progress;
                        context.BeginFigure(new Point(0, 0), true);
                        context.LineTo(new Point(0, waveY));
                        context.CubicBezierTo(
                            new Point(waveCenter - bulgeCross, waveY),
                            new Point(waveCenter - bulgeCross * 0.5, waveY + bulgeMain),
                            new Point(waveCenter, waveY + bulgeMain));
                        context.CubicBezierTo(
                            new Point(waveCenter + bulgeCross * 0.5, waveY + bulgeMain),
                            new Point(waveCenter + bulgeCross, waveY),
                            new Point(width, waveY));
                        context.LineTo(new Point(width, 0));
                        context.EndFigure(true);
                    }
                }
            }

            return geometry;
        }
    }
}
