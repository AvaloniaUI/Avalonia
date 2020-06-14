using System;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Utilities;

namespace Avalonia.Controls
{

    /// <summary>
    /// Enum which describes how to position the ticks in a <see cref="Slider"/>.
    /// </summary>
    public enum TickPlacement
    {
        /// <summary>
        /// No tick marks will appear.
        /// </summary>
        None,

        /// <summary>
        /// Tick marks  will appear above the track for a horizontal <see cref="Slider"/>, or to the left of the track for a vertical <see cref="Slider"/>.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Tick marks will appear below the track for a horizontal <see cref="Slider"/>, or to the right of the track for a vertical <see cref="Slider"/>.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Tick marks appear on both sides of either a horizontal or vertical <see cref="Slider"/>.
        /// </summary>
        Outside
    }

    /// <summary>
    /// A control that lets the user select from a range of values by moving a Thumb control along a Track.
    /// </summary>
    public class Slider : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            ScrollBar.OrientationProperty.AddOwner<Slider>();

        /// <summary>
        /// Defines the <see cref="IsSnapToTickEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSnapToTickEnabledProperty =
            AvaloniaProperty.Register<Slider, bool>(nameof(IsSnapToTickEnabled), false);

        /// <summary>
        /// Defines the <see cref="TickFrequency"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TickFrequencyProperty =
            AvaloniaProperty.Register<Slider, double>(nameof(TickFrequency), 0.0);

        /// <summary>
        /// Defines the <see cref="TickPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<TickPlacement> TickPlacementProperty =
            AvaloniaProperty.Register<TickBar, TickPlacement>(nameof(TickPlacement), 0d);

        // Slider required parts
        private bool _isDragging = false;
        private Track _track;
        private Button _decreaseButton;
        private Button _increaseButton;
        private IDisposable _decreaseButtonPressDispose;
        private IDisposable _decreaseButtonReleaseDispose;
        private IDisposable _increaseButtonSubscription;
        private IDisposable _increaseButtonReleaseDispose;
        private IDisposable _pointerMovedDispose;

        /// <summary>
        /// Initializes static members of the <see cref="Slider"/> class. 
        /// </summary>
        static Slider()
        {
            PressedMixin.Attach<Slider>();
            OrientationProperty.OverrideDefaultValue(typeof(Slider), Orientation.Horizontal);
            Thumb.DragStartedEvent.AddClassHandler<Slider>((x, e) => x.OnThumbDragStarted(e), RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<Slider>((x, e) => x.OnThumbDragCompleted(e), RoutingStrategies.Bubble);
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="Slider"/> class. 
        /// </summary>
        public Slider()
        {
            UpdatePseudoClasses(Orientation);
        }

        /// <summary>
        /// Gets or sets the orientation of a <see cref="Slider"/>.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="Slider"/> automatically moves the <see cref="Thumb"/> to the closest tick mark.
        /// </summary>
        public bool IsSnapToTickEnabled
        {
            get { return GetValue(IsSnapToTickEnabledProperty); }
            set { SetValue(IsSnapToTickEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the interval between tick marks.
        /// </summary>
        public double TickFrequency
        {
            get { return GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates where to draw 
        /// tick marks in relation to the track.
        /// </summary>
        public TickPlacement TickPlacement
        {
            get { return GetValue(TickPlacementProperty); }
            set { SetValue(TickPlacementProperty, value); }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            _decreaseButtonPressDispose?.Dispose();
            _decreaseButtonReleaseDispose?.Dispose();
            _increaseButtonSubscription?.Dispose();
            _increaseButtonReleaseDispose?.Dispose();
            _pointerMovedDispose?.Dispose();

            _decreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
            _track = e.NameScope.Find<Track>("PART_Track");
            _increaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");

            if (_track != null)
            {
                _track.IsThumbDragHandled = true;
            }

            if (_decreaseButton != null)
            {
                _decreaseButtonPressDispose = _decreaseButton.AddDisposableHandler(PointerPressedEvent, TrackPressed, RoutingStrategies.Tunnel);
                _decreaseButtonReleaseDispose = _decreaseButton.AddDisposableHandler(PointerReleasedEvent, TrackReleased, RoutingStrategies.Tunnel);
            }

            if (_increaseButton != null)
            {
                _increaseButtonSubscription = _increaseButton.AddDisposableHandler(PointerPressedEvent, TrackPressed, RoutingStrategies.Tunnel);
                _increaseButtonReleaseDispose = _increaseButton.AddDisposableHandler(PointerReleasedEvent, TrackReleased, RoutingStrategies.Tunnel);
            }

            _pointerMovedDispose = this.AddDisposableHandler(PointerMovedEvent, TrackMoved, RoutingStrategies.Tunnel);
        }

        private void TrackMoved(object sender, PointerEventArgs e)
        {
            if (_isDragging)
            {
                MoveToPoint(e.GetCurrentPoint(_track));
            }
        }

        private void TrackReleased(object sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
        }

        private void TrackPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                MoveToPoint(e.GetCurrentPoint(_track));
                _isDragging = true;
            }
        }

        private void MoveToPoint(PointerPoint x)
        {
            var orient = Orientation == Orientation.Horizontal;

            var pointDen = orient ? _track.Bounds.Width : _track.Bounds.Height;
            // Just add epsilon to avoid NaN in case 0/0
            pointDen += double.Epsilon;

            var pointNum = orient ? x.Position.X : x.Position.Y;
            var logicalPos = MathUtilities.Clamp(pointNum / pointDen, 0.0d, 1.0d);
            var invert = orient ? 0 : 1;
            var calcVal = Math.Abs(invert - logicalPos);
            var range = Maximum - Minimum;
            var finalValue = calcVal * range;

            Value = IsSnapToTickEnabled ? SnapToTick(finalValue) : finalValue;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<Orientation>());
            }
        }

        /// <summary>
        /// Called when user start dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragStarted(VectorEventArgs e)
        {
            _isDragging = true;
        }

        /// <summary>
        /// Called when user stop dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragCompleted(VectorEventArgs e)
        {
            _isDragging = false;
        }

        /// <summary>
        /// Snap the input 'value' to the closest tick.
        /// </summary>
        /// <param name="value">Value that want to snap to closest Tick.</param>
        private double SnapToTick(double value)
        {
            var previous = Minimum;
            var next = Maximum;

            if (TickFrequency > 0.0)
            {
                previous = Minimum + (Math.Round((value - Minimum) / TickFrequency) * TickFrequency);
                next = Math.Min(Maximum, previous + TickFrequency);
            }

            // Choose the closest value between previous and next. If tie, snap to 'next'.
            return MathUtilities.GreaterThanOrClose(value, (previous + next) * 0.5) ? next : previous;
        }

        private void UpdatePseudoClasses(Orientation o)
        {
            PseudoClasses.Set(":vertical", o == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
        }
    }
}
