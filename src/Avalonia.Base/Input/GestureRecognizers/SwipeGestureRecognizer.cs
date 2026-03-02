using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Input.GestureRecognizers
{
    /// <summary>
    /// A gesture recognizer that detects swipe gestures and raises
    /// <see cref="Gestures.SwipeGestureEvent"/> on the target element when a swipe is confirmed.
    /// </summary>
    public class SwipeGestureRecognizer : GestureRecognizer
    {
        private IPointer? _tracking;
        private Point _initialPosition;
        private int _gestureId;

        /// <summary>
        /// Defines the <see cref="Threshold"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ThresholdProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, double>(nameof(Threshold), 30d);

        /// <summary>
        /// Defines the <see cref="CrossAxisCancelThreshold"/> property.
        /// </summary>
        public static readonly StyledProperty<double> CrossAxisCancelThresholdProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, double>(
                nameof(CrossAxisCancelThreshold), 8d);

        /// <summary>
        /// Defines the <see cref="EdgeSize"/> property.
        /// Leading-edge start zone in px. 0 (default) = full area.
        /// When &gt; 0, only starts tracking if the pointer is within this many px
        /// of the leading edge (LTR: left; RTL: right).
        /// </summary>
        public static readonly StyledProperty<double> EdgeSizeProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, double>(nameof(EdgeSize), 0d);

        /// <summary>
        /// Defines the <see cref="IsEnabled"/> property.
        /// When false, the recognizer ignores all pointer events.
        /// Lets callers toggle the recognizer at runtime without needing to remove it from the
        /// collection (GestureRecognizerCollection has Add but no Remove).
        /// Default: true.
        /// </summary>
        public static readonly StyledProperty<bool> IsEnabledProperty =
            AvaloniaProperty.Register<SwipeGestureRecognizer, bool>(nameof(IsEnabled), true);

        /// <summary>
        /// Gets or sets the minimum distance in pixels the pointer must travel before a swipe
        /// is recognized. Default is 30px.
        /// </summary>
        public double Threshold
        {
            get => GetValue(ThresholdProperty);
            set => SetValue(ThresholdProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum cross-axis drift in pixels allowed before the gesture is
        /// cancelled. Default is 8px.
        /// </summary>
        public double CrossAxisCancelThreshold
        {
            get => GetValue(CrossAxisCancelThresholdProperty);
            set => SetValue(CrossAxisCancelThresholdProperty, value);
        }

        /// <summary>
        /// Gets or sets the leading-edge start zone in pixels. When greater than zero, tracking
        /// only begins if the pointer is within this distance of the leading edge. Default is 0
        /// (full area).
        /// </summary>
        public double EdgeSize
        {
            get => GetValue(EdgeSizeProperty);
            set => SetValue(EdgeSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the recognizer responds to pointer events.
        /// Setting this to false is a lightweight alternative to removing the recognizer from
        /// the collection. Default is true.
        /// </summary>
        public bool IsEnabled
        {
            get => GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (!IsEnabled) return;
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
            if (Target is not Visual visual) return;

            var pos = e.GetPosition(visual);
            var edgeSize = EdgeSize;

            if (edgeSize > 0)
            {
                bool isRtl = visual.FlowDirection == FlowDirection.RightToLeft;
                bool inEdge = isRtl
                    ? pos.X >= visual.Bounds.Width - edgeSize
                    : pos.X <= edgeSize;
                if (!inEdge)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(
                        this, "SwipeGestureRecognizer: press at {Pos} outside edge zone ({EdgeSize}px), ignoring",
                        pos, edgeSize);
                    return;
                }
            }

            _gestureId = SwipeGestureEventArgs.GetNextFreeId();
            _tracking = e.Pointer;
            _initialPosition = pos;

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(
                this, "SwipeGestureRecognizer: tracking started at {Pos} (pointer={PointerType})",
                pos, e.Pointer.Type);
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (_tracking != e.Pointer || Target is not Visual visual) return;

            var pos = e.GetPosition(visual);
            double dx = pos.X - _initialPosition.X;
            double dy = pos.Y - _initialPosition.Y;
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);
            double threshold = Threshold;

            if (absDx < threshold && absDy < threshold)
                return;

            SwipeDirection dir;
            Vector delta;
            if (absDx >= absDy)
            {
                dir = dx > 0 ? SwipeDirection.Right : SwipeDirection.Left;
                delta = new Vector(dx, 0);
            }
            else
            {
                dir = dy > 0 ? SwipeDirection.Down : SwipeDirection.Up;
                delta = new Vector(0, dy);
            }

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(
                this, "SwipeGestureRecognizer: swipe recognized — direction={Direction}, delta={Delta}",
                dir, delta);

            _tracking = null;
            Capture(e.Pointer);
            e.Handled = true;

            var args = new SwipeGestureEventArgs(_gestureId, dir, delta, _initialPosition);
            Target?.RaiseEvent(args);
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (_tracking == e.Pointer)
            {
                Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(
                    this, "SwipeGestureRecognizer: pointer released without crossing threshold — gesture discarded");
                _tracking = null;
            }
        }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (_tracking == pointer)
            {
                Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(
                    this, "SwipeGestureRecognizer: capture lost — gesture cancelled");
                _tracking = null;
            }
        }
    }
}
