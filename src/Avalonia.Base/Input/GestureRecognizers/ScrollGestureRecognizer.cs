using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace Avalonia.Input.GestureRecognizers
{
    public class ScrollGestureRecognizer 
        : StyledElement, // It's not an "element" in any way, shape or form, but TemplateBinding refuse to work otherwise
            IGestureRecognizer
    {
        // Pixels per second speed that is considered to be the stop of inertial scroll
        internal const double InertialScrollSpeedEnd = 5;
        public const double InertialResistance = 0.15;

        private bool _scrolling;
        private Point _trackedRootPoint;
        private IPointer? _tracking;
        private IInputElement? _target;
        private IGestureRecognizerActionsDispatcher? _actions;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private int _gestureId;
        private int _scrollStartDistance = 30;
        private Point _pointerPressedPoint;
        private VelocityTracker? _velocityTracker;

        // Movement per second
        private Vector _inertia;
        private ulong? _lastMoveTimestamp;
        private bool _isScrollInertiaEnabled;

        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(CanHorizontallyScroll),
                o => o.CanHorizontallyScroll,
                (o, v) => o.CanHorizontallyScroll = v);

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> CanVerticallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(CanVerticallyScroll),
                o => o.CanVerticallyScroll,
                (o, v) => o.CanVerticallyScroll = v);

        /// <summary>
        /// Defines the <see cref="IsScrollInertiaEnabled"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, bool> IsScrollInertiaEnabledProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, bool>(
                nameof(IsScrollInertiaEnabled),
                o => o.IsScrollInertiaEnabled,
                (o, v) => o.IsScrollInertiaEnabled = v);

        /// <summary>
        /// Defines the <see cref="ScrollStartDistance"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollGestureRecognizer, int> ScrollStartDistanceProperty =
            AvaloniaProperty.RegisterDirect<ScrollGestureRecognizer, int>(
                nameof(ScrollStartDistance),
                o => o.ScrollStartDistance,
                (o, v) => o.ScrollStartDistance = v);
        
        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => SetAndRaise(CanHorizontallyScrollProperty, ref _canHorizontallyScroll, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
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
        

        public void Initialize(IInputElement target, IGestureRecognizerActionsDispatcher actions)
        {
            _target = target;
            _actions = actions;
        }
        
        public void PointerPressed(PointerPressedEventArgs e)
        {
            if (e.Pointer.IsPrimary && 
                (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                EndGesture();
                _tracking = e.Pointer;
                _gestureId = ScrollGestureEventArgs.GetNextFreeId();
                _trackedRootPoint = _pointerPressedPoint = e.GetPosition((Visual?)_target);
            }
        }
        
        public void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer == _tracking)
            {
                var rootPoint = e.GetPosition((Visual?)_target);
                if (!_scrolling)
                {
                    if (CanHorizontallyScroll && Math.Abs(_trackedRootPoint.X - rootPoint.X) > ScrollStartDistance)
                        _scrolling = true;
                    if (CanVerticallyScroll && Math.Abs(_trackedRootPoint.Y - rootPoint.Y) > ScrollStartDistance)
                        _scrolling = true;
                    if (_scrolling)
                    {
                        _velocityTracker = new VelocityTracker();
                        
                        // Correct _trackedRootPoint with ScrollStartDistance, so scrolling does not start with a skip of ScrollStartDistance
                        _trackedRootPoint = new Point(
                            _trackedRootPoint.X - (_trackedRootPoint.X >= rootPoint.X ? _scrollStartDistance : -_scrollStartDistance),
                            _trackedRootPoint.Y - (_trackedRootPoint.Y >= rootPoint.Y ? _scrollStartDistance : -_scrollStartDistance));

                        _actions!.Capture(e.Pointer, this);
                    }
                }

                if (_scrolling)
                {
                    var vector = _trackedRootPoint - rootPoint;

                    _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), _pointerPressedPoint - rootPoint);

                    _lastMoveTimestamp = e.Timestamp;
                    _trackedRootPoint = rootPoint;
                    _target!.RaiseEvent(new ScrollGestureEventArgs(_gestureId, vector));
                    e.Handled = true;
                }
            }
        }

        public void PointerCaptureLost(IPointer pointer)
        {
            if (pointer == _tracking) EndGesture();
        }

        void EndGesture()
        {
            _tracking = null;
            if (_scrolling)
            {
                _inertia = default;
                _scrolling = false;
                _target!.RaiseEvent(new ScrollGestureEndedEventArgs(_gestureId));
                _gestureId = 0;
                _lastMoveTimestamp = null;
            }
            
        }


        public void PointerReleased(PointerReleasedEventArgs e)
        {
            if (e.Pointer == _tracking && _scrolling)
            {
                _inertia = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;

                e.Handled = true;
                if (_inertia == default
                    || e.Timestamp == 0
                    || _lastMoveTimestamp == 0
                    || e.Timestamp - _lastMoveTimestamp > 200
                    || !IsScrollInertiaEnabled)
                    EndGesture();
                else
                {
                    _tracking = null;
                    var savedGestureId = _gestureId;
                    var st = Stopwatch.StartNew();
                    var lastTime = TimeSpan.Zero;
                    _target!.RaiseEvent(new ScrollGestureInertiaStartingEventArgs(_gestureId, _inertia));
                    DispatcherTimer.Run(() =>
                    {
                        // Another gesture has started, finish the current one
                        if (_gestureId != savedGestureId)
                        {
                            return false;
                        }

                        var elapsedSinceLastTick = st.Elapsed - lastTime;
                        lastTime = st.Elapsed;

                        var speed = _inertia * Math.Pow(InertialResistance, st.Elapsed.TotalSeconds);
                        var distance = speed * elapsedSinceLastTick.TotalSeconds;
                        var scrollGestureEventArgs = new ScrollGestureEventArgs(_gestureId, distance);
                        _target!.RaiseEvent(scrollGestureEventArgs);

                        if (!scrollGestureEventArgs.Handled || scrollGestureEventArgs.ShouldEndScrollGesture)
                        {
                            EndGesture();
                            return false;
                        }

                        // EndGesture using InertialScrollSpeedEnd only in the direction of scrolling
                        if (CanVerticallyScroll && CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }
                        else if (CanVerticallyScroll && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }
                        else if (CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd)
                        {
                            EndGesture();
                            return false;
                        }

                        return true;
                    }, TimeSpan.FromMilliseconds(16), DispatcherPriority.Background);
                }
            }
        }
    }
}
