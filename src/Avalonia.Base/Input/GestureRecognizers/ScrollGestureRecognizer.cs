using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Input.GestureRecognizers
{
    public class ScrollGestureRecognizer : GestureRecognizer
    {
        // Pixels per second speed that is considered to be the stop of inertial scroll
        internal const double InertialScrollSpeedEnd = 5;
        public const double InertialResistance = 0.15;

        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private bool _isScrollInertiaEnabled;
        private readonly static int s_defaultScrollStartDistance = (int)((AvaloniaLocator.Current?.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Touch).Height ?? 10) / 2);
        private int _scrollStartDistance = s_defaultScrollStartDistance;

        private bool _scrolling;
        private Point _trackedRootPoint;
        private IPointer? _tracking;
        private Stopwatch? _stopWatch;
        private int _gestureId;
        private Point _pointerPressedPoint;
        private VelocityTracker? _velocityTracker;

        // Movement per second
        private Vector? _inertia;
        private ulong? _lastMoveTimestamp;
        private TimeSpan _lastTime;
        private TimeSpan _inertiaStartTime;
        private int _currentInertiaGestureId;

        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(nameof(CanHorizontallyScroll),
                o => o.CanHorizontallyScroll, (o, v) => o.CanHorizontallyScroll = v);

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanVerticallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(nameof(CanVerticallyScroll),
                o => o.CanVerticallyScroll, (o, v) => o.CanVerticallyScroll = v);

        /// <summary>
        /// Defines the <see cref="IsScrollInertiaEnabled"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> IsScrollInertiaEnabledProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(nameof(IsScrollInertiaEnabled),
                o => o.IsScrollInertiaEnabled, (o, v) => o.IsScrollInertiaEnabled = v);

        /// <summary>
        /// Defines the <see cref="ScrollStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, int> ScrollStartDistanceProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, int>(nameof(ScrollStartDistance),
                o => o.ScrollStartDistance, (o, v) => o.ScrollStartDistance = v,
                unsetValue: s_defaultScrollStartDistance);

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => SetAndRaise(CanHorizontallyScrollProperty, ref _canHorizontallyScroll, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled vertically.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set => SetAndRaise(CanVerticallyScrollProperty, ref _canVerticallyScroll, value);
        }

        /// <summary>
        /// Gets or sets whether the gesture should include inertia in it's behavior.
        /// </summary>
        public bool IsScrollInertiaEnabled
        {
            get => _isScrollInertiaEnabled;
            set => SetAndRaise(IsScrollInertiaEnabledProperty, ref _isScrollInertiaEnabled, value);
        }

        /// <summary>
        /// Gets or sets a value indicating the distance the pointer moves before scrolling is started
        /// </summary>
        public int ScrollStartDistance
        {
            get => _scrollStartDistance;
            set => SetAndRaise(ScrollStartDistanceProperty, ref _scrollStartDistance, value);
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(null);

            if (e.Pointer.Type is PointerType.Touch or PointerType.Pen
                && point.Properties.IsLeftButtonPressed)
            {
                EndGesture();
                _tracking = e.Pointer;
                _inertia = null;
                _gestureId = ScrollGestureEventArgs.GetNextFreeId();
                _trackedRootPoint = _pointerPressedPoint = point.Position;
                _velocityTracker = new VelocityTracker();
                _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), default);
            }
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer == _tracking)
            {
                var rootPoint = e.GetPosition(null);
                if (!_scrolling)
                {
                    if (CanHorizontallyScroll && Math.Abs(_trackedRootPoint.X - rootPoint.X) > ScrollStartDistance)
                        _scrolling = true;
                    if (CanVerticallyScroll && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > ScrollStartDistance)
                        _scrolling = true;
                    if (_scrolling)
                    {
                        // Correct _trackedRootPoint with ScrollStartDistance, so scrolling does not start with a skip of ScrollStartDistance
                        _trackedRootPoint = new Point(
                            _trackedRootPoint.X - (_trackedRootPoint.X >= rootPoint.X ? ScrollStartDistance : -ScrollStartDistance),
                            _trackedRootPoint.Y - (_trackedRootPoint.Y >= rootPoint.Y ? ScrollStartDistance : -ScrollStartDistance));

                        Capture(e.Pointer);
                    }
                }

                if (_scrolling)
                {
                    var vector = _trackedRootPoint - rootPoint;

                    _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), _pointerPressedPoint - rootPoint);

                    _lastMoveTimestamp = e.Timestamp;
                    Target!.RaiseEvent(new ScrollGestureEventArgs(_gestureId, vector));
                    _trackedRootPoint = rootPoint;
                    e.Handled = true;
                }
            }
        }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (pointer == _tracking)
                EndGesture();
        }

        void EndGesture()
        {
            _tracking = null;
            if (_scrolling)
            {
                _stopWatch?.Stop();
                _stopWatch = null;
                _inertia = default;
                _scrolling = false;
                Target!.RaiseEvent(new ScrollGestureEndedEventArgs(_gestureId));
                _gestureId = 0;
                _lastMoveTimestamp = null;
            }

        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (e.Pointer == _tracking && _scrolling)
            {
                _inertia = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;

                e.Handled = true;
                if (_inertia == null
                    || _inertia == Vector.Zero
                    || e.Timestamp == 0
                    || _lastMoveTimestamp == 0
                    || e.Timestamp - _lastMoveTimestamp > 200
                    || !IsScrollInertiaEnabled)
                    EndGesture();
                else
                {
                    _tracking = null;
                    _stopWatch = Stopwatch.StartNew();
                    _lastTime = _stopWatch.Elapsed;
                    _inertiaStartTime = _lastTime;
                    _currentInertiaGestureId = _gestureId;
                    Target!.RaiseEvent(new ScrollGestureInertiaStartingEventArgs(_gestureId, _inertia.Value));
                    MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
                }
            }
        }

        private void OnAnimationRequested(TimeSpan _)
        {
            // Calculate the current speed and dispatch the next inertia event. This is done asynchronously so we have run the events
            // with Input priority
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Another gesture has started, finish the current one
                if (_gestureId != _currentInertiaGestureId || _stopWatch == null || _inertia is not Vector inertia)
                {
                    return;
                }

                var timeSpan = _stopWatch.Elapsed;
                var elapsedSinceLastTick = timeSpan - _lastTime;
                _lastTime = timeSpan;

                var speed = inertia * Math.Pow(InertialResistance, (_lastTime - _inertiaStartTime).TotalSeconds);
                var distance = speed * elapsedSinceLastTick.TotalSeconds;
                var scrollGestureEventArgs = new ScrollGestureEventArgs(_gestureId, distance);
                Target!.RaiseEvent(scrollGestureEventArgs);

                if (!scrollGestureEventArgs.Handled || scrollGestureEventArgs.ShouldEndScrollGesture)
                {
                    EndGesture();
                    return;
                }

                // EndGesture using InertialScrollSpeedEnd only in the direction of scrolling
                if (CanVerticallyScroll && CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                {
                    // NO-OP 
                }
                else if (CanVerticallyScroll && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                {
                    EndGesture();
                    return;
                }
                else if (CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd)
                {
                    EndGesture();
                    return;
                }

                // Reschedule on the next animation frame. TopLevel.RequestAnimationFrame isn't available on the Base project, so we use the global MediaContext
                MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
            }, DispatcherPriority.Input);

        }
    }
}
