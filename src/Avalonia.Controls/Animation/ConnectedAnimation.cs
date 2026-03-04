using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides data for the <see cref="ConnectedAnimation.Completed"/> event.
    /// </summary>
    internal sealed class ConnectedAnimationCompletedEventArgs : EventArgs
    {
        internal ConnectedAnimationCompletedEventArgs(bool cancelled) => Cancelled = cancelled;

        /// <summary>
        /// Gets a value indicating whether the animation was cancelled before it completed.
        /// When <see langword="true"/> the destination element's opacity has already been
        /// restored but no visual transition was shown.
        /// </summary>
        public bool Cancelled { get; }
    }

    /// <summary>
    /// Animates an element seamlessly between two views during navigation by flying a
    /// proxy over the <see cref="OverlayLayer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Obtain an instance via <see cref="ConnectedAnimationService.PrepareToAnimate"/>,
    /// then start it with <see cref="TryStart(Visual)"/> after navigation.
    /// </para>
    /// <para>
    /// The animation auto-disposes after three seconds if not consumed (matching UWP behaviour).
    /// </para>
    /// </remarks>
    internal class ConnectedAnimation : IDisposable
    {
        private readonly string _key;
        private readonly ConnectedAnimationService _service;

        private Rect _sourceBounds;
        private CornerRadius _sourceCornerRadius;
        private IBrush? _sourceBackground;
        private Thickness _sourceBorderThickness;
        private IBrush? _sourceBorderBrush;
        private RenderTargetBitmap? _sourceSnapshot;

        private bool _isConsumed;
        private bool _disposed;

        private CancellationTokenSource? _timeoutCts;
        private IDisposable? _timeoutTimerDisposable;
        private CancellationTokenSource? _animationCts;
        private DispatcherTimer? _animationTimer;

        private static readonly SplineEasing s_directEasing = new(0, 0, 0.58, 1.0);
        private static readonly SplineEasing s_basicEasing = new(0.42, 0, 0.58, 1);
        private static readonly SplineEasing s_gravityEasing = new(0.1, 0.9, 0.2, 1.0);

        // Active-flight state used by Dispose to clean up if cancelled mid-animation.
        private Visual? _activeDestination;
        private double _activeDestOriginalOpacity;
        private Border? _activeProxy;
        private OverlayLayer? _activeOverlayLayer;

        internal ConnectedAnimation(string key, Visual source, ConnectedAnimationService service)
        {
            _key = key;
            _service = service;

            _sourceCornerRadius = GetCornerRadius(source);
            _sourceBackground = GetBackground(source);
            _sourceBorderThickness = GetBorderThickness(source);
            _sourceBorderBrush = GetBorderBrush(source);

            var topLevel = source.FindAncestorOfType<TopLevel>();
            if (topLevel != null && source.Bounds.Width > 0 && source.Bounds.Height > 0)
            {
                var transform = source.TransformToVisual(topLevel);
                if (transform.HasValue)
                {
                    _sourceBounds = new Rect(
                        transform.Value.Transform(new Point(0, 0)),
                        new Size(source.Bounds.Width, source.Bounds.Height));
                }

                CaptureSnapshot(source, topLevel);
            }

            // Auto-dispose after 3 s if not consumed (matches UWP behaviour).
            _timeoutCts = new CancellationTokenSource();
            var token = _timeoutCts.Token;
            _timeoutTimerDisposable = DispatcherTimer.RunOnce(() =>
            {
                if (!token.IsCancellationRequested && !_isConsumed)
                    Dispose();
            }, TimeSpan.FromSeconds(3), DispatcherPriority.Background);
        }

        /// <summary>Gets the key that identifies this animation.</summary>
        public string Key => _key;

        /// <summary>Gets a value indicating whether <c>TryStart</c> has been called.</summary>
        public bool IsConsumed => _isConsumed;

        /// <summary>
        /// Gets or sets the configuration that controls timing and visual style.
        /// Set this before calling <c>TryStart</c>.
        /// </summary>
        public ConnectedAnimationConfiguration? Configuration { get; set; }

        /// <summary>
        /// Raised when the animation finishes or is cancelled.
        /// Check <see cref="ConnectedAnimationCompletedEventArgs.Cancelled"/> to distinguish the cases.
        /// </summary>
        public event EventHandler<ConnectedAnimationCompletedEventArgs>? Completed;

        /// <summary>
        /// Starts the animation towards <paramref name="destination"/>.
        /// Returns <see langword="false"/> if the animation has already been consumed or disposed.
        /// </summary>
        public bool TryStart(Visual destination) =>
            TryStart(destination, Array.Empty<Visual>());

        /// <summary>
        /// Starts the animation towards <paramref name="destination"/> with optional
        /// <paramref name="coordinatedElements"/> that fade in during the last 40 % of the animation.
        /// Returns <see langword="false"/> if the animation has already been consumed or disposed.
        /// </summary>
        public bool TryStart(Visual destination, IReadOnlyList<Visual> coordinatedElements)
        {
            if (_isConsumed || _disposed)
                return false;

            _isConsumed = true;
            CancelTimeout();

            _ = RunAnimationAsync(destination, coordinatedElements);
            return true;
        }

        // Exposed internally so tests can verify disposal state without reflection.
        internal bool IsDisposed => _disposed;

        /// <summary>
        /// Releases all resources and cancels the animation if it is in flight.
        /// The <see cref="Completed"/> event is raised with <c>Cancelled = true</c>
        /// only when the animation was actively running at dispose time.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CancelTimeout();
            _service.RemoveAnimation(_key);
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
            _animationTimer?.Stop();
            _animationTimer = null;

            var wasMidFlight = _activeDestination != null;

            if (_activeDestination != null)
            {
                _activeDestination.Opacity = _activeDestOriginalOpacity;
                _activeDestination = null;
            }

            if (_activeProxy != null && _activeOverlayLayer != null)
            {
                _activeOverlayLayer.Children.Remove(_activeProxy);
                _activeProxy = null;
                _activeOverlayLayer = null;
            }

            _sourceSnapshot?.Dispose();
            _sourceSnapshot = null;

            if (wasMidFlight)
                Completed?.Invoke(this, new ConnectedAnimationCompletedEventArgs(cancelled: true));
        }

        private void CaptureSnapshot(Visual source, TopLevel topLevel)
        {
            try
            {
                var dpi = topLevel.RenderScaling;
                var w = (int)Math.Ceiling(source.Bounds.Width * dpi);
                var h = (int)Math.Ceiling(source.Bounds.Height * dpi);
                if (w > 0 && h > 0)
                {
                    _sourceSnapshot = new RenderTargetBitmap(
                        new PixelSize(w, h),
                        new Vector(96 * dpi, 96 * dpi));
                    _sourceSnapshot.Render(source);
                }
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)
                    ?.Log(this, "ConnectedAnimation snapshot failed for key '{Key}': {Exception}", Key, ex);
                _sourceSnapshot?.Dispose();
                _sourceSnapshot = null;
            }
        }

        private void CancelTimeout()
        {
            _timeoutTimerDisposable?.Dispose();
            _timeoutTimerDisposable = null;
            _timeoutCts?.Cancel();
            _timeoutCts?.Dispose();
            _timeoutCts = null;
        }

        private async Task RunAnimationAsync(Visual destination, IReadOnlyList<Visual> coordinatedElements)
        {
            try
            {
                await RunAnimationCoreAsync(destination, coordinatedElements);
            }
            catch (OperationCanceledException)
            {
                // Dispose already handles cleanup.
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)
                    ?.Log(this, "ConnectedAnimation failed for key '{Key}': {Exception}", Key, ex);
                Dispose();
            }
        }

        private async Task RunAnimationCoreAsync(Visual destination, IReadOnlyList<Visual> coordinatedElements)
        {
            ResolveTimingAndEasing(_service, out var duration, out var easing,
                out var useGravityDip, out var useShadow);

            var topLevel = destination.FindAncestorOfType<TopLevel>();
            if (topLevel == null)
            {
                OnAnimationComplete();
                return;
            }

            var overlayLayer = OverlayLayer.GetOverlayLayer(topLevel);
            if (overlayLayer == null)
            {
                await RunFallbackAnimationAsync(destination, coordinatedElements,
                    topLevel, duration, easing, useGravityDip, useShadow);
                return;
            }

            // Wait for destination layout if bounds are not yet valid.
            if (destination.Bounds.Width <= 0 || destination.Bounds.Height <= 0 ||
                !destination.TransformToVisual(topLevel).HasValue)
            {
                if (destination is Layoutable layoutable)
                {
                    var layoutTcs = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

                    EventHandler? handler = null;
                    handler = (_, _) =>
                    {
                        if (destination.Bounds.Width > 0 && destination.Bounds.Height > 0 &&
                            destination.TransformToVisual(topLevel).HasValue)
                        {
                            layoutable.LayoutUpdated -= handler;
                            layoutTcs.TrySetResult(true);
                        }
                    };
                    layoutable.LayoutUpdated += handler;

                    using var reg = timeoutCts.Token.Register(() =>
                    {
                        layoutable.LayoutUpdated -= handler;
                        layoutTcs.TrySetResult(false);
                    });

                    await layoutTcs.Task;
                }
            }

            var destTransform = destination.TransformToVisual(topLevel);
            if (!destTransform.HasValue)
            {
                OnAnimationComplete();
                return;
            }

            var destBounds = new Rect(
                destTransform.Value.Transform(new Point(0, 0)),
                new Size(destination.Bounds.Width, destination.Bounds.Height));

            var destCornerRadius = GetCornerRadius(destination);
            var destBorderThickness = GetBorderThickness(destination);
            var destBorderBrush = GetBorderBrush(destination);

            var proxy = new ConnectedAnimationProxy
            {
                Width = _sourceBounds.Width,
                Height = _sourceBounds.Height,
                CornerRadius = _sourceCornerRadius,
                BorderThickness = _sourceBorderThickness,
                BorderBrush = _sourceBorderBrush,
                ClipToBounds = true,
                IsHitTestVisible = false,
            };

            if (_sourceBackground != null)
                proxy.Background = _sourceBackground;
            else if (_sourceSnapshot != null)
                proxy.Background = new ImageBrush(_sourceSnapshot) { Stretch = Stretch.Fill };

            Canvas.SetLeft(proxy, _sourceBounds.X);
            Canvas.SetTop(proxy, _sourceBounds.Y);

            var destOriginalOpacity = destination.Opacity;
            destination.Opacity = 0;

            _activeDestination = destination;
            _activeDestOriginalOpacity = destOriginalOpacity;
            _activeProxy = proxy;
            _activeOverlayLayer = overlayLayer;

            var originalOpacities = new double[coordinatedElements.Count];
            for (int i = 0; i < coordinatedElements.Count; i++)
            {
                if (ReferenceEquals(coordinatedElements[i], destination))
                    continue;
                originalOpacities[i] = coordinatedElements[i].Opacity;
                coordinatedElements[i].Opacity = 0;
            }

            var destBackground = GetBackground(destination);
            var needsCrossFade = destBackground != null
                && _sourceBackground != null
                && !BrushesEqual(_sourceBackground, destBackground);

            overlayLayer.Children.Add(proxy);

            Border? crossFadeOverlay = null;
            if (needsCrossFade)
            {
                crossFadeOverlay = new Border
                {
                    Background = destBackground,
                    Opacity = 0,
                    IsHitTestVisible = false,
                };
                proxy.Child = crossFadeOverlay;
            }

            var startX = _sourceBounds.X;  var endX = destBounds.X;
            var startY = _sourceBounds.Y;  var endY = destBounds.Y;
            var startW = _sourceBounds.Width;  var endW = destBounds.Width;
            var startH = _sourceBounds.Height; var endH = destBounds.Height;

            var srcTL = _sourceCornerRadius.TopLeft;
            var srcTR = _sourceCornerRadius.TopRight;
            var srcBR = _sourceCornerRadius.BottomRight;
            var srcBL = _sourceCornerRadius.BottomLeft;
            var dstTL = destCornerRadius.TopLeft;
            var dstTR = destCornerRadius.TopRight;
            var dstBR = destCornerRadius.BottomRight;
            var dstBL = destCornerRadius.BottomLeft;

            var srcBT = _sourceBorderThickness;
            var dstBT = destBorderThickness;

            var canLerpBorderBrush = _sourceBorderBrush is ISolidColorBrush && destBorderBrush is ISolidColorBrush;
            var srcBC = (_sourceBorderBrush as ISolidColorBrush)?.Color ?? default;
            var dstBC = (destBorderBrush as ISolidColorBrush)?.Color ?? default;
            SolidColorBrush? lerpBrush = canLerpBorderBrush ? new SolidColorBrush(srcBC) : null;
            var snapBorderBrush = !canLerpBorderBrush && destBorderBrush != null;

            double dipAmplitude = 0, scaleAmplitude = 0;
            if (useGravityDip)
            {
                var travel = Math.Max(Math.Abs(endX - startX), Math.Abs(endY - startY));
                dipAmplitude = Math.Clamp(travel * 0.12, 8, 50);
                scaleAmplitude = 0.05;
            }

            var animateShadow = useShadow && useGravityDip;

            _animationCts = new CancellationTokenSource();

            proxy.ProgressCallback = progress =>
            {
                var ep = easing.Ease(progress);

                var bx = startX + (endX - startX) * ep;
                var by = startY + (endY - startY) * ep;
                var bw = startW + (endW - startW) * ep;
                var bh = startH + (endH - startH) * ep;

                if (useGravityDip)
                {
                    var dipCurve   = Math.Sin(Math.PI * progress);
                    var scaleBoost = 1.0 + scaleAmplitude * dipCurve;
                    var sw = bw * scaleBoost;
                    var sh = bh * scaleBoost;

                    Canvas.SetLeft(proxy, bx - (sw - bw) / 2);
                    Canvas.SetTop(proxy, by - (sh - bh) / 2 + dipAmplitude * dipCurve);
                    proxy.Width  = Math.Max(1, sw);
                    proxy.Height = Math.Max(1, sh);

                    if (animateShadow)
                    {
                        var alpha   = (byte)(100 * dipCurve);
                        var blur    = 24 * dipCurve;
                        var offsetY = 10 * dipCurve;
                        proxy.BoxShadow = new BoxShadows(new BoxShadow
                        {
                            OffsetX = 0, OffsetY = offsetY,
                            Blur = blur,
                            Color = Color.FromArgb(alpha, 0, 0, 0)
                        });
                    }
                }
                else
                {
                    Canvas.SetLeft(proxy, bx);
                    Canvas.SetTop(proxy, by);
                    proxy.Width  = Math.Max(1, bw);
                    proxy.Height = Math.Max(1, bh);
                }

                proxy.CornerRadius = new CornerRadius(
                    srcTL + (dstTL - srcTL) * ep,
                    srcTR + (dstTR - srcTR) * ep,
                    srcBR + (dstBR - srcBR) * ep,
                    srcBL + (dstBL - srcBL) * ep);

                proxy.BorderThickness = new Thickness(
                    srcBT.Left + (dstBT.Left - srcBT.Left) * ep,
                    srcBT.Top + (dstBT.Top - srcBT.Top) * ep,
                    srcBT.Right + (dstBT.Right - srcBT.Right) * ep,
                    srcBT.Bottom + (dstBT.Bottom - srcBT.Bottom) * ep);

                if (lerpBrush != null)
                {
                    lerpBrush.Color = Color.FromArgb(
                        (byte)(srcBC.A + (dstBC.A - srcBC.A) * ep),
                        (byte)(srcBC.R + (dstBC.R - srcBC.R) * ep),
                        (byte)(srcBC.G + (dstBC.G - srcBC.G) * ep),
                        (byte)(srcBC.B + (dstBC.B - srcBC.B) * ep));
                    proxy.BorderBrush = lerpBrush;
                }
                else if (snapBorderBrush && progress >= 0.5)
                {
                    proxy.BorderBrush = destBorderBrush;
                    snapBorderBrush = false;
                }

                if (crossFadeOverlay != null)
                    crossFadeOverlay.Opacity = ep;

                if (progress > 0.6)
                {
                    var cp = (progress - 0.6) / 0.4;
                    for (int j = 0; j < coordinatedElements.Count; j++)
                    {
                        if (ReferenceEquals(coordinatedElements[j], destination))
                            continue;
                        coordinatedElements[j].Opacity = originalOpacities[j] * cp;
                    }
                }
            };

            var animation = new Avalonia.Animation.Animation
            {
                Duration = duration,
                Easing = new LinearEasing(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(ConnectedAnimationProxy.ProgressProperty, 0.0) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(ConnectedAnimationProxy.ProgressProperty, 1.0) } },
                }
            };

            await animation.RunAsync(proxy, _animationCts.Token);

            _animationCts?.Dispose();
            _animationCts = null;

            destination.Opacity = destOriginalOpacity;

            _activeDestination = null;
            _activeProxy = null;
            _activeOverlayLayer = null;

            overlayLayer.Children.Remove(proxy);

            for (int i = 0; i < coordinatedElements.Count; i++)
            {
                if (ReferenceEquals(coordinatedElements[i], destination))
                    continue;
                coordinatedElements[i].Opacity = originalOpacities[i];
            }

            _sourceSnapshot?.Dispose();
            _sourceSnapshot = null;

            OnAnimationComplete();
        }

        private async Task RunFallbackAnimationAsync(
            Visual destination, IReadOnlyList<Visual> coordinatedElements,
            TopLevel topLevel, TimeSpan duration, Easing easing,
            bool useGravityDip, bool useShadow)
        {
            var destTransform = destination.TransformToVisual(topLevel);
            if (!destTransform.HasValue) { OnAnimationComplete(); return; }

            var destBounds = new Rect(
                destTransform.Value.Transform(new Point(0, 0)),
                new Size(destination.Bounds.Width, destination.Bounds.Height));

            var dx = _sourceBounds.X - destBounds.X;
            var dy = _sourceBounds.Y - destBounds.Y;
            var sx = _sourceBounds.Width  > 0 && destBounds.Width  > 0 ? _sourceBounds.Width  / destBounds.Width  : 1.0;
            var sy = _sourceBounds.Height > 0 && destBounds.Height > 0 ? _sourceBounds.Height / destBounds.Height : 1.0;

            var group = new TransformGroup();
            var scaleT = new ScaleTransform(sx, sy);
            var transT = new TranslateTransform(dx, dy);
            group.Children.Add(scaleT);
            group.Children.Add(transT);

            var origTransform = destination.RenderTransform;
            var origOrigin    = destination.RenderTransformOrigin;
            destination.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
            destination.RenderTransform = group;

            double dipAmp = 0, scaleAmp = 0;
            if (useGravityDip)
            {
                var travel = Math.Max(Math.Abs(dx), Math.Abs(dy));
                dipAmp   = Math.Clamp(travel * 0.12, 8, 50);
                scaleAmp = 0.05;
            }

            var shadowBorder     = useShadow && useGravityDip && destination is Border b ? b : null;
            var origShadow       = shadowBorder?.BoxShadow ?? default;

            var originalOpacities = new double[coordinatedElements.Count];
            for (int i = 0; i < coordinatedElements.Count; i++)
            {
                if (ReferenceEquals(coordinatedElements[i], destination))
                    continue;
                originalOpacities[i] = coordinatedElements[i].Opacity;
                coordinatedElements[i].Opacity = 0;
            }

            var startTimestamp = Stopwatch.GetTimestamp();
            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _animationTimer.Tick += (_, _) =>
            {
                var elapsed  = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
                var progress = Math.Min(1.0, elapsed / duration.TotalMilliseconds);
                var ep       = easing.Ease(progress);

                var bsx = sx + (1.0 - sx) * ep;
                var bsy = sy + (1.0 - sy) * ep;
                var btx = dx * (1.0 - ep);
                var bty = dy * (1.0 - ep);

                if (useGravityDip)
                {
                    var dipCurve   = Math.Sin(Math.PI * progress);
                    var scaleBoost = 1.0 + scaleAmp * dipCurve;
                    scaleT.ScaleX = bsx * scaleBoost;
                    scaleT.ScaleY = bsy * scaleBoost;
                    transT.X = btx;
                    transT.Y = bty + dipAmp * dipCurve;

                    if (shadowBorder != null)
                    {
                        var alpha   = (byte)(100 * dipCurve);
                        var blur    = 24 * dipCurve;
                        var offsetY = 10 * dipCurve;
                        shadowBorder.BoxShadow = new BoxShadows(new BoxShadow
                        {
                            OffsetX = 0, OffsetY = offsetY,
                            Blur = blur,
                            Color = Color.FromArgb(alpha, 0, 0, 0)
                        });
                    }
                }
                else
                {
                    scaleT.ScaleX = bsx;
                    scaleT.ScaleY = bsy;
                    transT.X = btx;
                    transT.Y = bty;
                }

                if (progress > 0.6)
                {
                    var cp = (progress - 0.6) / 0.4;
                    for (int j = 0; j < coordinatedElements.Count; j++)
                    {
                        if (ReferenceEquals(coordinatedElements[j], destination))
                            continue;
                        coordinatedElements[j].Opacity = originalOpacities[j] * cp;
                    }
                }

                if (progress >= 1.0)
                {
                    _animationTimer!.Stop();
                    _animationTimer = null;
                    tcs.TrySetResult(true);
                }
            };

            _animationCts = new CancellationTokenSource();
            using var reg = _animationCts.Token.Register(() => tcs.TrySetCanceled());

            _animationTimer.Start();

            bool cancelled = false;
            try
            {
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                _animationTimer?.Stop();
                _animationTimer = null;
                _animationCts?.Dispose();
                _animationCts = null;
            }

            destination.RenderTransform = origTransform;
            destination.RenderTransformOrigin = origOrigin;

            if (shadowBorder != null)
                shadowBorder.BoxShadow = origShadow;

            for (int i = 0; i < coordinatedElements.Count; i++)
            {
                if (ReferenceEquals(coordinatedElements[i], destination))
                    continue;
                coordinatedElements[i].Opacity = originalOpacities[i];
            }

            _sourceSnapshot?.Dispose();
            _sourceSnapshot = null;

            if (cancelled)
            {
                Completed?.Invoke(this, new ConnectedAnimationCompletedEventArgs(cancelled: true));
                return;
            }

            OnAnimationComplete();
        }

        internal void ResolveTimingAndEasing(ConnectedAnimationService service,
            out TimeSpan duration, out Easing easing,
            out bool useGravityDip, out bool useShadow)
        {
            if (Configuration is DirectConnectedAnimationConfiguration direct)
            {
                duration      = direct.Duration ?? TimeSpan.FromMilliseconds(150);
                easing        = s_directEasing;
                useGravityDip = false;
                useShadow     = false;
            }
            else if (Configuration is BasicConnectedAnimationConfiguration)
            {
                duration      = service.DefaultDuration;
                easing        = service.DefaultEasingFunction ?? s_basicEasing;
                useGravityDip = false;
                useShadow     = false;
            }
            else
            {
                duration      = service.DefaultDuration;
                easing        = service.DefaultEasingFunction ?? s_gravityEasing;
                useGravityDip = true;
                useShadow     = Configuration is GravityConnectedAnimationConfiguration g
                    ? g.IsShadowEnabled
                    : true;
            }
        }

        private void OnAnimationComplete()
        {
            _service.RemoveAnimation(_key);
            Completed?.Invoke(this, new ConnectedAnimationCompletedEventArgs(cancelled: false));
        }

        private static IBrush? GetBackground(Visual visual) => visual switch
        {
            Border b             => b.Background,
            Panel p              => p.Background,
            ContentPresenter cp  => cp.Background,
            ContentControl cc    => cc.Background,
            TemplatedControl tc  => tc.Background,
            _                    => null,
        };

        private static CornerRadius GetCornerRadius(Visual visual) => visual switch
        {
            Border b             => b.CornerRadius,
            TemplatedControl tc  => tc.CornerRadius,
            ContentPresenter cp  => cp.CornerRadius,
            _                    => default,
        };

        private static Thickness GetBorderThickness(Visual visual) => visual switch
        {
            Border b             => b.BorderThickness,
            TemplatedControl tc  => tc.BorderThickness,
            ContentPresenter cp  => cp.BorderThickness,
            _                    => default,
        };

        private static IBrush? GetBorderBrush(Visual visual) => visual switch
        {
            Border b             => b.BorderBrush,
            TemplatedControl tc  => tc.BorderBrush,
            ContentPresenter cp  => cp.BorderBrush,
            _                    => null,
        };

        private static bool BrushesEqual(IBrush a, IBrush b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is ISolidColorBrush sa && b is ISolidColorBrush sb)
                return sa.Color == sb.Color && Math.Abs(sa.Opacity - sb.Opacity) < 0.001;
            return false;
        }

        private class ConnectedAnimationProxy : Border
        {
            public static readonly StyledProperty<double> ProgressProperty =
                AvaloniaProperty.Register<ConnectedAnimationProxy, double>(nameof(Progress));

            public double Progress
            {
                get => GetValue(ProgressProperty);
                set => SetValue(ProgressProperty, value);
            }

            internal Action<double>? ProgressCallback { get; set; }

            protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
            {
                base.OnPropertyChanged(change);
                if (change.Property == ProgressProperty)
                    ProgressCallback?.Invoke(change.GetNewValue<double>());
            }
        }
    }
}
