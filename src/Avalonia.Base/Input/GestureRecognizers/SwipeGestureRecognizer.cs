using System;
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
        private int _gestureId;

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
                _gestureId = SwipeGestureEventArgs.GetNextFreeId();
                _trackedRootPoint = point.Position;
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
                    if (CanHorizontallySwipe && Math.Abs(_trackedRootPoint.X - rootPoint.X) > SwipeStartDistance)
                        _swiping = true;
                    if (CanVerticallySwipe && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > SwipeStartDistance)
                        _swiping = true;

                    if (_swiping)
                    {
                        _trackedRootPoint = new Point(
                            _trackedRootPoint.X - (_trackedRootPoint.X >= rootPoint.X ? SwipeStartDistance : -SwipeStartDistance),
                            _trackedRootPoint.Y - (_trackedRootPoint.Y >= rootPoint.Y ? SwipeStartDistance : -SwipeStartDistance));

                        Capture(e.Pointer);
                    }
                }

                if (_swiping)
                {
                    var delta = _trackedRootPoint - rootPoint;
                    Target!.RaiseEvent(new SwipeGestureEventArgs(_gestureId, delta));
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
                Target!.RaiseEvent(new SwipeGestureEndedEventArgs(_gestureId));
                _gestureId = 0;
            }
        }
    }
}
