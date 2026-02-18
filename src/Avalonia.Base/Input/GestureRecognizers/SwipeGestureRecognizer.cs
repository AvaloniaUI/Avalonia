using System;
using System.Diagnostics;
using Avalonia.Platform;

namespace Avalonia.Input.GestureRecognizers
{
    /// <summary>
    /// A gesture recognizer that detects swipe gestures for paging interactions.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ScrollGestureRecognizer"/>, this recognizer is optimized for discrete
    /// paging interactions (e.g., carousel navigation) rather than continuous scrolling.
    /// It does not include inertia or friction physics.
    /// </remarks>
    public class SwipeGestureRecognizer : GestureRecognizer
    {
        private static readonly int s_defaultSwipeStartDistance =
            (int)((AvaloniaLocator.Current?.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Touch).Height ?? 10) / 2);

        private bool _canHorizontallySwipe;
        private bool _canVerticallySwipe;
        private int _swipeStartDistance = s_defaultSwipeStartDistance;

        private bool _swiping;
        private Point _trackedRootPoint;
        private IPointer? _tracking;
        private int _id;

        private Vector _velocity;
        private long _lastTimestamp;

        /// <summary>
        /// Defines the <see cref="CanHorizontallySwipe"/> property.
        /// </summary>
        public static readonly DirectProperty<SwipeGestureRecognizer, bool> CanHorizontallySwipeProperty =
            AvaloniaProperty.RegisterDirect<SwipeGestureRecognizer, bool>(nameof(CanHorizontallySwipe),
                o => o.CanHorizontallySwipe, (o, v) => o.CanHorizontallySwipe = v);

        /// <summary>
        /// Defines the <see cref="CanVerticallySwipe"/> property.
        /// </summary>
        public static readonly DirectProperty<SwipeGestureRecognizer, bool> CanVerticallySwipeProperty =
            AvaloniaProperty.RegisterDirect<SwipeGestureRecognizer, bool>(nameof(CanVerticallySwipe),
                o => o.CanVerticallySwipe, (o, v) => o.CanVerticallySwipe = v);

        /// <summary>
        /// Defines the <see cref="SwipeStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<SwipeGestureRecognizer, int> SwipeStartDistanceProperty =
            AvaloniaProperty.RegisterDirect<SwipeGestureRecognizer, int>(nameof(SwipeStartDistance),
                o => o.SwipeStartDistance, (o, v) => o.SwipeStartDistance = v,
                unsetValue: s_defaultSwipeStartDistance);

        /// <summary>
        /// Gets or sets a value indicating whether horizontal swipes are tracked.
        /// </summary>
        public bool CanHorizontallySwipe
        {
            get => _canHorizontallySwipe;
            set => SetAndRaise(CanHorizontallySwipeProperty, ref _canHorizontallySwipe, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether vertical swipes are tracked.
        /// </summary>
        public bool CanVerticallySwipe
        {
            get => _canVerticallySwipe;
            set => SetAndRaise(CanVerticallySwipeProperty, ref _canVerticallySwipe, value);
        }

        /// <summary>
        /// Gets or sets the distance the pointer must move before a swipe is recognized.
        /// </summary>
        public int SwipeStartDistance
        {
            get => _swipeStartDistance;
            set => SetAndRaise(SwipeStartDistanceProperty, ref _swipeStartDistance, value);
        }

        /// <inheritdoc/>
        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(null);

            if (e.Pointer.Type is PointerType.Touch or PointerType.Pen or PointerType.Mouse
                && point.Properties.IsLeftButtonPressed)
            {
                EndGesture();
                _tracking = e.Pointer;
                _id = SwipeGestureEventArgs.GetNextFreeId();
                _trackedRootPoint = point.Position;
                _velocity = default;
                _lastTimestamp = 0;
            }
        }

        /// <inheritdoc/>
        protected override void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer == _tracking)
            {
                var rootPoint = e.GetPosition(null);

                if (!_swiping)
                {
                    var horizontalTriggered = CanHorizontallySwipe && Math.Abs(_trackedRootPoint.X - rootPoint.X) > SwipeStartDistance;
                    var verticalTriggered = CanVerticallySwipe && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > SwipeStartDistance;

                    if (horizontalTriggered || verticalTriggered)
                    {
                        _swiping = true;
                        
                        _trackedRootPoint = new Point(
                            horizontalTriggered
                                ? _trackedRootPoint.X - (_trackedRootPoint.X >= rootPoint.X ? SwipeStartDistance : -SwipeStartDistance)
                                : rootPoint.X,
                            verticalTriggered
                                ? _trackedRootPoint.Y - (_trackedRootPoint.Y >= rootPoint.Y ? SwipeStartDistance : -SwipeStartDistance)
                                : rootPoint.Y);

                        Capture(e.Pointer);
                    }
                }

                if (_swiping)
                {
                    var delta = _trackedRootPoint - rootPoint;

                    var now = Stopwatch.GetTimestamp();
                    if (_lastTimestamp > 0)
                    {
                        var elapsedSeconds = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
                        if (elapsedSeconds > 0)
                        {
                            var instantVelocity = delta / elapsedSeconds;
                            _velocity = _velocity * 0.5 + instantVelocity * 0.5;
                        }
                    }
                    _lastTimestamp = now;

                    Target!.RaiseEvent(new SwipeGestureEventArgs(_id, delta, _velocity));
                    _trackedRootPoint = rootPoint;
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (pointer == _tracking)
                EndGesture();
        }

        /// <inheritdoc/>
        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (e.Pointer == _tracking && _swiping)
            {
                e.Handled = true;
                EndGesture();
            }
        }

        private void EndGesture()
        {
            _tracking = null;
            if (_swiping)
            {
                _swiping = false;
                Target!.RaiseEvent(new SwipeGestureEndedEventArgs(_id, _velocity));
                _velocity = default;
                _lastTimestamp = 0;
                _id = 0;
            }
        }
    }
}
