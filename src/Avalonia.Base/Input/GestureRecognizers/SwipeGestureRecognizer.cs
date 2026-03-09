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
        private bool _swiping;
        private Point _trackedRootPoint;
        private IPointer? _tracking;
        private int _id;

        private Vector _velocity;
        private long _lastTimestamp;

        /// <summary>
        /// Defines the <see cref="CanHorizontallySwipe"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanHorizontallySwipeProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, bool>(nameof(CanHorizontallySwipe));

        /// <summary>
        /// Defines the <see cref="CanVerticallySwipe"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanVerticallySwipeProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, bool>(nameof(CanVerticallySwipe));

        /// <summary>
        /// Defines the <see cref="Threshold"/> property.
        /// </summary>
        /// <remarks>
        /// A value of 0 (the default) causes the distance to be read from
        /// <see cref="IPlatformSettings"/> at the time of the first gesture.
        /// </remarks>
        public static readonly StyledProperty<int> ThresholdProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, int>(nameof(Threshold), defaultValue: 0);

        /// <summary>
        /// Defines the <see cref="IsMouseEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsMouseEnabledProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, bool>(nameof(IsMouseEnabled), defaultValue: false);

        /// <summary>
        /// Defines the <see cref="IsEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsEnabledProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, bool>(nameof(IsEnabled), defaultValue: true);


        /// <summary>
        /// Gets or sets a value indicating whether horizontal swipes are tracked.
        /// </summary>
        public bool CanHorizontallySwipe
        {
            get => GetValue(CanHorizontallySwipeProperty);
            set => SetValue(CanHorizontallySwipeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether vertical swipes are tracked.
        /// </summary>
        public bool CanVerticallySwipe
        {
            get => GetValue(CanVerticallySwipeProperty);
            set => SetValue(CanVerticallySwipeProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum pointer movement in pixels before a swipe is recognized.
        /// A value of 0 reads the threshold from <see cref="IPlatformSettings"/> at gesture time.
        /// </summary>
        public int Threshold
        {
            get => GetValue(ThresholdProperty);
            set => SetValue(ThresholdProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether mouse pointer events trigger swipe gestures.
        /// Defaults to <see langword="false"/>; touch and pen are always enabled.
        /// </summary>
        public bool IsMouseEnabled
        {
            get => GetValue(IsMouseEnabledProperty);
            set => SetValue(IsMouseEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this recognizer responds to pointer events.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool IsEnabled
        {
            get => GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }


        /// <inheritdoc/>
        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (!IsEnabled)
                return;

            var point = e.GetCurrentPoint(null);

            if ((e.Pointer.Type is PointerType.Touch or PointerType.Pen ||
                 (IsMouseEnabled && e.Pointer.Type == PointerType.Mouse))
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
                var threshold = GetEffectiveThreshold();

                if (!_swiping)
                {
                    var horizontalTriggered = CanHorizontallySwipe && Math.Abs(_trackedRootPoint.X - rootPoint.X) > threshold;
                    var verticalTriggered = CanVerticallySwipe && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > threshold;

                    if (horizontalTriggered || verticalTriggered)
                    {
                        _swiping = true;

                        _trackedRootPoint = new Point(
                            horizontalTriggered
                                ? _trackedRootPoint.X - (_trackedRootPoint.X >= rootPoint.X ? threshold : -threshold)
                                : rootPoint.X,
                            verticalTriggered
                                ? _trackedRootPoint.Y - (_trackedRootPoint.Y >= rootPoint.Y ? threshold : -threshold)
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

        private const double DefaultTapSize = 10;

        private int GetEffectiveThreshold()
        {
            var configured = Threshold;
            if (configured > 0)
                return configured;

            var tapSize = AvaloniaLocator.Current?.GetService<IPlatformSettings>()
                ?.GetTapSize(PointerType.Touch).Height ?? DefaultTapSize;

            return (int)(tapSize / 2);
        }
    }
}
