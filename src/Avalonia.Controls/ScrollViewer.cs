using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control scrolls its content if the content is bigger than the space available.
    /// </summary>
    public class ScrollViewer : ContentControl, IScrollable, IScrollAnchorProvider
    {
        /// <summary>
        /// Defines the <see cref="CanHorizontallyScroll"/> property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, bool> CanHorizontallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, bool>(
                nameof(CanHorizontallyScroll),
                o => o.CanHorizontallyScroll);

        /// <summary>
        /// Defines the <see cref="CanVerticallyScroll"/> property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, bool> CanVerticallyScrollProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, bool>(
                nameof(CanVerticallyScroll),
                o => o.CanVerticallyScroll);

        /// <summary>
        /// Defines the <see cref="Extent"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Size> ExtentProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Size>(nameof(Extent),
                o => o.Extent,
                (o, v) => o.Extent = v);

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Vector> OffsetProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Vector>(
                nameof(Offset),
                o => o.Offset,
                (o, v) => o.Offset = v);

        /// <summary>
        /// Defines the <see cref="Viewport"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Size> ViewportProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Size>(nameof(Viewport),
                o => o.Viewport,
                (o, v) => o.Viewport = v);

        /// <summary>
        /// Defines the <see cref="LargeChange"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Size> LargeChangeProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Size>(
                nameof(LargeChange),
                o => o.LargeChange);

        /// <summary>
        /// Defines the <see cref="SmallChange"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Size> SmallChangeProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Size>(
                nameof(SmallChange),
                o => o.SmallChange);

        /// <summary>
        /// Defines the HorizontalScrollBarMaximum property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, double> HorizontalScrollBarMaximumProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, double>(
                nameof(HorizontalScrollBarMaximum),
                o => o.HorizontalScrollBarMaximum);

        /// <summary>
        /// Defines the HorizontalScrollBarValue property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, double> HorizontalScrollBarValueProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, double>(
                nameof(HorizontalScrollBarValue),
                o => o.HorizontalScrollBarValue,
                (o, v) => o.HorizontalScrollBarValue = v);

        /// <summary>
        /// Defines the HorizontalScrollBarViewportSize property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, double> HorizontalScrollBarViewportSizeProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, double>(
                nameof(HorizontalScrollBarViewportSize),
                o => o.HorizontalScrollBarViewportSize);

        /// <summary>
        /// Defines the <see cref="HorizontalScrollBarVisibility"/> property.
        /// </summary>
        public static readonly AttachedProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>(
                nameof(HorizontalScrollBarVisibility),
                ScrollBarVisibility.Hidden);

        /// <summary>
        /// Defines the VerticalScrollBarMaximum property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, double> VerticalScrollBarMaximumProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, double>(
                nameof(VerticalScrollBarMaximum),
                o => o.VerticalScrollBarMaximum);

        /// <summary>
        /// Defines the VerticalScrollBarValue property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, double> VerticalScrollBarValueProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, double>(
                nameof(VerticalScrollBarValue),
                o => o.VerticalScrollBarValue,
                (o, v) => o.VerticalScrollBarValue = v);

        /// <summary>
        /// Defines the VerticalScrollBarViewportSize property.
        /// </summary>
        /// <remarks>
        /// There is no public C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly DirectProperty<ScrollViewer, double> VerticalScrollBarViewportSizeProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, double>(
                nameof(VerticalScrollBarViewportSize),
                o => o.VerticalScrollBarViewportSize);

        /// <summary>
        /// Defines the <see cref="VerticalScrollBarVisibility"/> property.
        /// </summary>
        public static readonly AttachedProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>(
                nameof(VerticalScrollBarVisibility),
                ScrollBarVisibility.Auto);

        /// <summary>
        /// Defines the <see cref="ScrollChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<ScrollChangedEventArgs> ScrollChangedEvent =
            RoutedEvent.Register<ScrollViewer, ScrollChangedEventArgs>(
                nameof(ScrollChanged),
                RoutingStrategies.Bubble);

        internal const double DefaultSmallChange = 16;

        private IDisposable _childSubscription;
        private ILogicalScrollable _logicalScrollable;
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Size _largeChange;
        private Size _smallChange = new Size(DefaultSmallChange, DefaultSmallChange);

        /// <summary>
        /// Initializes static members of the <see cref="ScrollViewer"/> class.
        /// </summary>
        static ScrollViewer()
        {
            HorizontalScrollBarVisibilityProperty.Changed.AddClassHandler<ScrollViewer>((x, e) => x.ScrollBarVisibilityChanged(e));
            VerticalScrollBarVisibilityProperty.Changed.AddClassHandler<ScrollViewer>((x, e) => x.ScrollBarVisibilityChanged(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollViewer"/> class.
        /// </summary>
        public ScrollViewer()
        {
        }

        /// <summary>
        /// Occurs when changes are detected to the scroll position, extent, or viewport size.
        /// </summary>
        public event EventHandler<ScrollChangedEventArgs> ScrollChanged
        {
            add => AddHandler(ScrollChangedEvent, value);
            remove => RemoveHandler(ScrollChangedEvent, value);
        }

        /// <summary>
        /// Gets the extent of the scrollable content.
        /// </summary>
        public Size Extent
        {
            get
            {
                return _extent;
            }

            private set
            {
                var old = _extent;

                if (SetAndRaise(ExtentProperty, ref _extent, value))
                {
                    CalculatedPropertiesChanged(extentDelta: value - old);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current scroll offset.
        /// </summary>
        public Vector Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                var old = _offset;

                value = ValidateOffset(this, value);

                if (SetAndRaise(OffsetProperty, ref _offset, value))
                {
                    CalculatedPropertiesChanged(offsetDelta: value - old);
                }
            }
        }

        /// <summary>
        /// Gets the size of the viewport on the scrollable content.
        /// </summary>
        public Size Viewport
        {
            get
            {
                return _viewport;
            }

            private set
            {
                var old = _viewport;

                if (SetAndRaise(ViewportProperty, ref _viewport, value))
                {
                    CalculatedPropertiesChanged(viewportDelta: value - old);
                }
            }
        }

        /// <summary>
        /// Gets the large (page) change value for the scroll viewer.
        /// </summary>
        public Size LargeChange => _largeChange;

        /// <summary>
        /// Gets the small (line) change value for the scroll viewer.
        /// </summary>
        public Size SmallChange => _smallChange;

        /// <summary>
        /// Gets or sets the horizontal scrollbar visibility.
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the viewer can scroll horizontally.
        /// </summary>
        protected bool CanHorizontallyScroll
        {
            get { return HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled; }
        }

        /// <summary>
        /// Gets a value indicating whether the viewer can scroll vertically.
        /// </summary>
        protected bool CanVerticallyScroll
        {
            get { return VerticalScrollBarVisibility != ScrollBarVisibility.Disabled; }
        }

        /// <summary>
        /// Gets the maximum horizontal scrollbar value.
        /// </summary>
        protected double HorizontalScrollBarMaximum
        {
            get { return Max(_extent.Width - _viewport.Width, 0); }
        }

        /// <summary>
        /// Gets or sets the horizontal scrollbar value.
        /// </summary>
        protected double HorizontalScrollBarValue
        {
            get { return _offset.X; }
            set
            {
                if (_offset.X != value)
                {
                    var old = Offset.X;
                    Offset = Offset.WithX(value);
                    RaisePropertyChanged(HorizontalScrollBarValueProperty, old, value);
                }
            }
        }

        /// <summary>
        /// Gets the size of the horizontal scrollbar viewport.
        /// </summary>
        protected double HorizontalScrollBarViewportSize
        {
            get { return _viewport.Width; }
        }

        /// <summary>
        /// Gets the maximum vertical scrollbar value.
        /// </summary>
        protected double VerticalScrollBarMaximum
        {
            get { return Max(_extent.Height - _viewport.Height, 0); }
        }

        /// <summary>
        /// Gets or sets the vertical scrollbar value.
        /// </summary>
        protected double VerticalScrollBarValue
        {
            get { return _offset.Y; }
            set
            {
                if (_offset.Y != value)
                {
                    var old = Offset.Y;
                    Offset = Offset.WithY(value);
                    RaisePropertyChanged(VerticalScrollBarValueProperty, old, value);
                }
            }
        }

        /// <summary>
        /// Gets the size of the vertical scrollbar viewport.
        /// </summary>
        protected double VerticalScrollBarViewportSize
        {
            get { return _viewport.Height; }
        }

        /// <inheritdoc/>
        IControl IScrollAnchorProvider.CurrentAnchor => null; // TODO: Implement

        /// <summary>
        /// Scrolls the content up one line.
        /// </summary>
        public void LineUp()
        {
            Offset -= new Vector(0, _smallChange.Height);
        }

        /// <summary>
        /// Scrolls the content down one line.
        /// </summary>
        public void LineDown()
        {
            Offset += new Vector(0, _smallChange.Height);
        }

        /// <summary>
        /// Scrolls the content left one line.
        /// </summary>
        public void LineLeft()
        {
            Offset -= new Vector(_smallChange.Width, 0);
        }

        /// <summary>
        /// Scrolls the content right one line.
        /// </summary>
        public void LineRight()
        {
            Offset += new Vector(_smallChange.Width, 0);
        }

        /// <summary>
        /// Scrolls to the top-left corner of the content.
        /// </summary>
        public void ScrollToHome()
        {
            Offset = new Vector(double.NegativeInfinity, double.NegativeInfinity);
        }

        /// <summary>
        /// Scrolls to the bottom-left corner of the content.
        /// </summary>
        public void ScrollToEnd()
        {
            Offset = new Vector(double.NegativeInfinity, double.PositiveInfinity);
        }

        /// <summary>
        /// Gets the value of the HorizontalScrollBarVisibility attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static ScrollBarVisibility GetHorizontalScrollBarVisibility(Control control)
        {
            return control.GetValue(HorizontalScrollBarVisibilityProperty);
        }

        /// <summary>
        /// Gets the value of the HorizontalScrollBarVisibility attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetHorizontalScrollBarVisibility(Control control, ScrollBarVisibility value)
        {
            control.SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Gets the value of the VerticalScrollBarVisibility attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static ScrollBarVisibility GetVerticalScrollBarVisibility(Control control)
        {
            return control.GetValue(VerticalScrollBarVisibilityProperty);
        }

        /// <summary>
        /// Gets the value of the VerticalScrollBarVisibility attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetVerticalScrollBarVisibility(Control control, ScrollBarVisibility value)
        {
            control.SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        void IScrollAnchorProvider.RegisterAnchorCandidate(IControl element)
        {
            // TODO: Implement
        }

        void IScrollAnchorProvider.UnregisterAnchorCandidate(IControl element)
        {
            // TODO: Implement
        }

        protected override bool RegisterContentPresenter(IContentPresenter presenter)
        {
            _childSubscription?.Dispose();
            _childSubscription = null;

            if (base.RegisterContentPresenter(presenter))
            {
                _childSubscription = Presenter?
                    .GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(ChildChanged);
                return true;
            }

            return false;
        }

        internal static Vector CoerceOffset(Size extent, Size viewport, Vector offset)
        {
            var maxX = Math.Max(extent.Width - viewport.Width, 0);
            var maxY = Math.Max(extent.Height - viewport.Height, 0);
            return new Vector(Clamp(offset.X, 0, maxX), Clamp(offset.Y, 0, maxY));
        }

        private static double Clamp(double value, double min, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private static double Max(double x, double y)
        {
            var result = Math.Max(x, y);
            return double.IsNaN(result) ? 0 : result;
        }

        private static Vector ValidateOffset(AvaloniaObject o, Vector value)
        {
            ScrollViewer scrollViewer = o as ScrollViewer;

            if (scrollViewer != null)
            {
                var extent = scrollViewer.Extent;
                var viewport = scrollViewer.Viewport;
                return CoerceOffset(extent, viewport, value);
            }
            else
            {
                return value;
            }
        }

        private void ChildChanged(IControl child)
        {
            if (_logicalScrollable is object)
            {
                _logicalScrollable.ScrollInvalidated -= LogicalScrollInvalidated;
                _logicalScrollable = null;
            }

            if (child is ILogicalScrollable logical)
            {
                _logicalScrollable = logical;
                logical.ScrollInvalidated += LogicalScrollInvalidated;
            }

            CalculatedPropertiesChanged();
        }

        private void LogicalScrollInvalidated(object sender, EventArgs e)
        {
            CalculatedPropertiesChanged();
        }

        private void ScrollBarVisibilityChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var wasEnabled = !ScrollBarVisibility.Disabled.Equals(e.OldValue);
            var isEnabled = !ScrollBarVisibility.Disabled.Equals(e.NewValue);

            if (wasEnabled != isEnabled)
            {
                if (e.Property == HorizontalScrollBarVisibilityProperty)
                {
                    RaisePropertyChanged(
                        CanHorizontallyScrollProperty,
                        wasEnabled,
                        isEnabled);
                }
                else if (e.Property == VerticalScrollBarVisibilityProperty)
                {
                    RaisePropertyChanged(
                        CanVerticallyScrollProperty,
                        wasEnabled,
                        isEnabled);
                }
            }
        }

        private void CalculatedPropertiesChanged(
            Size extentDelta = default,
            Vector offsetDelta = default,
            Size viewportDelta = default)
        {
            // Pass old values of 0 here because we don't have the old values at this point,
            // and it shouldn't matter as only the template uses these properies.
            RaisePropertyChanged(HorizontalScrollBarMaximumProperty, 0, HorizontalScrollBarMaximum);
            RaisePropertyChanged(HorizontalScrollBarValueProperty, 0, HorizontalScrollBarValue);
            RaisePropertyChanged(HorizontalScrollBarViewportSizeProperty, 0, HorizontalScrollBarViewportSize);
            RaisePropertyChanged(VerticalScrollBarMaximumProperty, 0, VerticalScrollBarMaximum);
            RaisePropertyChanged(VerticalScrollBarValueProperty, 0, VerticalScrollBarValue);
            RaisePropertyChanged(VerticalScrollBarViewportSizeProperty, 0, VerticalScrollBarViewportSize);

            if (_logicalScrollable?.IsLogicalScrollEnabled == true)
            {
                SetAndRaise(SmallChangeProperty, ref _smallChange, _logicalScrollable.ScrollSize);
                SetAndRaise(LargeChangeProperty, ref _largeChange, _logicalScrollable.PageScrollSize);
            }
            else
            {
                SetAndRaise(SmallChangeProperty, ref _smallChange, new Size(DefaultSmallChange, DefaultSmallChange));
                SetAndRaise(LargeChangeProperty, ref _largeChange, Viewport);
            }

            if (extentDelta != default || offsetDelta != default || viewportDelta != default)
            {
                using var route = BuildEventRoute(ScrollChangedEvent);

                if (route.HasHandlers)
                {
                    var e = new ScrollChangedEventArgs(
                        new Vector(extentDelta.Width, extentDelta.Height),
                        offsetDelta,
                        new Vector(viewportDelta.Width, viewportDelta.Height));
                    route.RaiseEvent(this, e);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                VerticalScrollBarValue = Math.Max(_offset.Y - _viewport.Height, 0);
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                VerticalScrollBarValue = Math.Min(_offset.Y + _viewport.Height, VerticalScrollBarMaximum);
                e.Handled = true;
            }
        }
    }
}
