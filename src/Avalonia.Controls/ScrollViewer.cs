using System;
using Avalonia.Reactive;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control which scrolls its content if the content is bigger than the space available.
    /// </summary>
    [TemplatePart("PART_HorizontalScrollBar", typeof(ScrollBar))]
    [TemplatePart("PART_VerticalScrollBar",   typeof(ScrollBar))]
    public class ScrollViewer : ContentControl, IScrollable, IScrollAnchorProvider, IInternalScroller
    {
        /// <summary>
        /// Defines the <see cref="BringIntoViewOnFocusChange "/> property.
        /// </summary>
        public static readonly AttachedProperty<bool> BringIntoViewOnFocusChangeProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, bool>(nameof(BringIntoViewOnFocusChange), true);

        /// <summary>
        /// Defines the <see cref="Extent"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Size> ExtentProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Size>(nameof(Extent),
                o => o.Extent);

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly StyledProperty<Vector> OffsetProperty =
            AvaloniaProperty.Register<ScrollViewer, Vector>(nameof(Offset), coerce: CoerceOffset);

        /// <summary>
        /// Defines the <see cref="Viewport"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Size> ViewportProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Size>(nameof(Viewport),
                o => o.Viewport);

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
        /// Defines the <see cref="ScrollBarMaximum"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, Vector> ScrollBarMaximumProperty =
            AvaloniaProperty.RegisterDirect<ScrollViewer, Vector>(
                nameof(ScrollBarMaximum),
                o => o.ScrollBarMaximum);

        /// <summary>
        /// Defines the <see cref="HorizontalScrollBarVisibility"/> property.
        /// </summary>
        public static readonly AttachedProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>(
                nameof(HorizontalScrollBarVisibility),
                ScrollBarVisibility.Disabled);

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsType"/> property.
        /// </summary>
        public static readonly AttachedProperty<SnapPointsType> HorizontalSnapPointsTypeProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, SnapPointsType>(
                nameof(HorizontalSnapPointsType));

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsType"/> property.
        /// </summary>
        public static readonly AttachedProperty<SnapPointsType> VerticalSnapPointsTypeProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, SnapPointsType>(
                nameof(VerticalSnapPointsType));

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsAlignment"/> property.
        /// </summary>
        public static readonly AttachedProperty<SnapPointsAlignment> HorizontalSnapPointsAlignmentProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, SnapPointsAlignment>(
                nameof(HorizontalSnapPointsAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsAlignment"/> property.
        /// </summary>
        public static readonly AttachedProperty<SnapPointsAlignment> VerticalSnapPointsAlignmentProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, SnapPointsAlignment>(
                nameof(VerticalSnapPointsAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalScrollBarVisibility"/> property.
        /// </summary>
        public static readonly AttachedProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>(
                nameof(VerticalScrollBarVisibility),
                ScrollBarVisibility.Auto);

        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollViewer, bool> IsExpandedProperty =
            ScrollBar.IsExpandedProperty.AddOwner<ScrollViewer>(o => o.IsExpanded);

        /// <summary>
        /// Defines the <see cref="AllowAutoHide"/> property.
        /// </summary>
        public static readonly AttachedProperty<bool> AllowAutoHideProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, bool>(
                nameof(AllowAutoHide),
                true);

        /// <summary>
        /// Defines the <see cref="IsScrollChainingEnabled"/> property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsScrollChainingEnabledProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, bool>(
                nameof(IsScrollChainingEnabled),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="IsScrollInertiaEnabled"/> property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsScrollInertiaEnabledProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, bool>(
                nameof(IsScrollInertiaEnabled),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="IsDeferredScrollingEnabled"/> property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsDeferredScrollingEnabledProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, Control, bool>(
                nameof(IsDeferredScrollingEnabled));

        /// <summary>
        /// Defines the <see cref="ScrollChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<ScrollChangedEventArgs> ScrollChangedEvent =
            RoutedEvent.Register<ScrollViewer, ScrollChangedEventArgs>(
                nameof(ScrollChanged),
                RoutingStrategies.Bubble);

        internal const double DefaultSmallChange = 16;

        private IDisposable? _childSubscription;
        private ILogicalScrollable? _logicalScrollable;
        private Size _extent;
        private Size _viewport;
        private Size _oldExtent;
        private Vector _oldOffset;
        private Vector _oldMaximum;
        private Size _oldViewport;
        private Size _largeChange;
        private Size _smallChange = new Size(DefaultSmallChange, DefaultSmallChange);
        private bool _isExpanded;
        private IDisposable? _scrollBarExpandSubscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollViewer"/> class.
        /// </summary>
        public ScrollViewer()
        {
            LayoutUpdated += OnLayoutUpdated;
        }

        /// <summary>
        /// Occurs when changes are detected to the scroll position, extent, or viewport size.
        /// </summary>
        public event EventHandler<ScrollChangedEventArgs>? ScrollChanged
        {
            add => AddHandler(ScrollChangedEvent, value);
            remove => RemoveHandler(ScrollChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets a value that determines whether the <see cref="ScrollViewer"/> uses a
        /// bring-into-view scroll behavior when an item in the view gets focus.
        /// </summary>
        /// <value>
        /// true to use a behavior that brings focused items into view. false to use a behavior
        /// that focused items do not automatically scroll into view. The default is true.
        /// </value>
        /// <remarks>
        /// <see cref="BringIntoViewOnFocusChange"/> can either be set explicitly on a
        /// <see cref="ScrollViewer"/>, or a the attached 
        /// <code>ScrollViewer.BringIntoViewOnFocusChange</code> property can be set on an element
        /// that hosts a <see cref="ScrollViewer"/>.
        /// </remarks>
        public bool BringIntoViewOnFocusChange
        {
            get => GetValue(BringIntoViewOnFocusChangeProperty);
            set => SetValue(BringIntoViewOnFocusChangeProperty, value);
        }

        /// <summary>
        /// Gets the extent of the scrollable content.
        /// </summary>
        public Size Extent
        {
            get => _extent;

            internal set
            {
                if (SetAndRaise(ExtentProperty, ref _extent, value))
                {
                    CalculatedPropertiesChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current scroll offset.
        /// </summary>
        public Vector Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        /// <summary>
        /// Gets the size of the viewport on the scrollable content.
        /// </summary>
        public Size Viewport
        {
            get => _viewport;

            internal set
            {
                if (SetAndRaise(ViewportProperty, ref _viewport, value))
                {
                    CalculatedPropertiesChanged();
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
            get => GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the viewer can scroll horizontally.
        /// </summary>
        protected bool CanHorizontallyScroll
        {
            get => HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
        }

        bool IInternalScroller.CanHorizontallyScroll => CanHorizontallyScroll;

        /// <summary>
        /// Gets a value indicating whether the viewer can scroll vertically.
        /// </summary>
        protected bool CanVerticallyScroll
        {
            get => VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
        }

        bool IInternalScroller.CanVerticallyScroll => CanVerticallyScroll;

        /// <inheritdoc/>
        public Control? CurrentAnchor => (Presenter as IScrollAnchorProvider)?.CurrentAnchor;

        /// <summary>
        /// Gets the maximum scrolling distance (which is <see cref="Extent"/> - <see cref="Viewport"/>).
        /// </summary>
        public Vector ScrollBarMaximum => new(Max(_extent.Width - _viewport.Width, 0), Max(_extent.Height - _viewport.Height, 0));

        /// <summary>
        /// Gets a value that indicates whether any scrollbar is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            private set => SetAndRaise(ScrollBar.IsExpandedProperty, ref _isExpanded, value);
        }

        /// <summary>
        /// Gets or sets how scroll gesture reacts to the snap points along the horizontal axis.
        /// </summary>
        public SnapPointsType HorizontalSnapPointsType
        {
            get => GetValue(HorizontalSnapPointsTypeProperty);
            set => SetValue(HorizontalSnapPointsTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets how scroll gesture reacts to the snap points along the vertical axis.
        /// </summary>
        public SnapPointsType VerticalSnapPointsType
        {
            get => GetValue(VerticalSnapPointsTypeProperty);
            set => SetValue(VerticalSnapPointsTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets how the existing snap points are horizontally aligned versus the initial viewport.
        /// </summary>
        public SnapPointsAlignment HorizontalSnapPointsAlignment
        {
            get => GetValue(HorizontalSnapPointsAlignmentProperty); 
            set => SetValue(HorizontalSnapPointsAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets how the existing snap points are vertically aligned versus the initial viewport.
        /// </summary>
        public SnapPointsAlignment VerticalSnapPointsAlignment
        {
            get => GetValue(VerticalSnapPointsAlignmentProperty); 
            set => SetValue(VerticalSnapPointsAlignmentProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether scrollbars can hide itself when user is not interacting with it.
        /// </summary>
        public bool AllowAutoHide
        {
            get => GetValue(AllowAutoHideProperty);
            set => SetValue(AllowAutoHideProperty, value);
        }

        /// <summary>
        ///  Gets or sets if scroll chaining is enabled. The default value is true.
        /// </summary>
        /// <remarks>
        ///  After a user hits a scroll limit on an element that has been nested within another scrollable element,
        /// you can specify whether that parent element should continue the scrolling operation begun in its child element.
        /// This is called scroll chaining.
        /// </remarks>
        public bool IsScrollChainingEnabled
        {
            get => GetValue(IsScrollChainingEnabledProperty);
            set => SetValue(IsScrollChainingEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether scroll gestures should include inertia in their behavior and value.
        /// </summary>
        public bool IsScrollInertiaEnabled
        {
            get => GetValue(IsScrollInertiaEnabledProperty);
            set => SetValue(IsScrollInertiaEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether dragging of <see cref="Thumb"/> elements should update the <see cref="ScrollViewer"/> only when the user releases the mouse.
        /// </summary>
        public bool IsDeferredScrollingEnabled
        {
            get => GetValue(IsDeferredScrollingEnabledProperty);
            set => SetValue(IsDeferredScrollingEnabledProperty, value);
        }

        /// <summary>
        /// Scrolls the content up one line.
        /// </summary>
        public void LineUp() => SetCurrentValue(OffsetProperty, Offset - new Vector(0, _smallChange.Height));

        /// <summary>
        /// Scrolls the content down one line.
        /// </summary>
        public void LineDown() => SetCurrentValue(OffsetProperty, Offset + new Vector(0, _smallChange.Height));

        /// <summary>
        /// Scrolls the content left one line.
        /// </summary>
        public void LineLeft() => SetCurrentValue(OffsetProperty, Offset - new Vector(_smallChange.Width, 0));

        /// <summary>
        /// Scrolls the content right one line.
        /// </summary>
        public void LineRight() => SetCurrentValue(OffsetProperty, Offset + new Vector(_smallChange.Width, 0));

        /// <summary>
        /// Scrolls the content upward by one page.
        /// </summary>
        public void PageUp() => SetCurrentValue(OffsetProperty, Offset.WithY(Math.Max(Offset.Y - _viewport.Height, 0)));

        /// <summary>
        /// Scrolls the content downward by one page.
        /// </summary>
        public void PageDown() => SetCurrentValue(OffsetProperty, Offset.WithY(Math.Min(Offset.Y + _viewport.Height, ScrollBarMaximum.Y)));

        /// <summary>
        /// Scrolls the content left by one page.
        /// </summary>
        public void PageLeft() => SetCurrentValue(OffsetProperty, Offset.WithX(Math.Max(Offset.X - _viewport.Width, 0)));

        /// <summary>
        /// Scrolls the content tight by one page.
        /// </summary>
        public void PageRight() => SetCurrentValue(OffsetProperty, Offset.WithX(Math.Min(Offset.X + _viewport.Width, ScrollBarMaximum.X)));

        /// <summary>
        /// Scrolls to the top-left corner of the content.
        /// </summary>
        public void ScrollToHome() => SetCurrentValue(OffsetProperty, new Vector(double.NegativeInfinity, double.NegativeInfinity));

        /// <summary>
        /// Scrolls to the bottom-left corner of the content.
        /// </summary>
        public void ScrollToEnd() => SetCurrentValue(OffsetProperty, new Vector(double.NegativeInfinity, double.PositiveInfinity));

        /// <summary>
        /// Gets the value of the <see cref="BringIntoViewOnFocusChange"/> attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static bool GetBringIntoViewOnFocusChange(Control control)
        {
            return control.GetValue(BringIntoViewOnFocusChangeProperty);
        }

        /// <summary>
        /// Gets the value of the <see cref="BringIntoViewOnFocusChange"/> attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetBringIntoViewOnFocusChange(Control control, bool value)
        {
            control.SetValue(BringIntoViewOnFocusChangeProperty, value);
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
        /// Gets the value of the HorizontalSnapPointsType attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static SnapPointsType GetHorizontalSnapPointsType(Control control)
        {
            return control.GetValue(HorizontalSnapPointsTypeProperty);
        }

        /// <summary>
        /// Gets the value of the HorizontalSnapPointsType attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetHorizontalSnapPointsType(Control control, SnapPointsType value)
        {
            control.SetValue(HorizontalSnapPointsTypeProperty, value);
        }

        /// <summary>
        /// Gets the value of the VerticalSnapPointsType attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static SnapPointsType GetVerticalSnapPointsType(Control control)
        {
            return control.GetValue(VerticalSnapPointsTypeProperty);
        }

        /// <summary>
        /// Gets the value of the VerticalSnapPointsType attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetVerticalSnapPointsType(Control control, SnapPointsType value)
        {
            control.SetValue(VerticalSnapPointsTypeProperty, value);
        }

        /// <summary>
        /// Gets the value of the HorizontalSnapPointsAlignment attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static SnapPointsAlignment GetHorizontalSnapPointsAlignment(Control control)
        {
            return control.GetValue(HorizontalSnapPointsAlignmentProperty);
        }

        /// <summary>
        /// Gets the value of the HorizontalSnapPointsAlignment attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetHorizontalSnapPointsAlignment(Control control, SnapPointsAlignment value)
        {
            control.SetValue(HorizontalSnapPointsAlignmentProperty, value);
        }

        /// <summary>
        /// Gets the value of the VerticalSnapPointsAlignment attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static SnapPointsAlignment GetVerticalSnapPointsAlignment(Control control)
        {
            return control.GetValue(VerticalSnapPointsAlignmentProperty);
        }

        /// <summary>
        /// Gets the value of the VerticalSnapPointsAlignment attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetVerticalSnapPointsAlignment(Control control, SnapPointsAlignment value)
        {
            control.SetValue(VerticalSnapPointsAlignmentProperty, value);
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
        /// Gets the value of the AllowAutoHideProperty attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        public static void SetAllowAutoHide(Control control, bool value)
        {
            control.SetValue(AllowAutoHideProperty, value);
        }

        /// <summary>
        /// Gets the value of the AllowAutoHideProperty attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        public static bool GetAllowAutoHide(Control control)
        {
            return control.GetValue(AllowAutoHideProperty);
        }

        /// <summary>
        /// Sets the value of the IsScrollChainingEnabled attached property.
        /// </summary>
        /// <param name="control">The control to set the value on.</param>
        /// <param name="value">The value of the property.</param>
        /// <remarks>
        ///  After a user hits a scroll limit on an element that has been nested within another scrollable element,
        /// you can specify whether that parent element should continue the scrolling operation begun in its child element.
        /// This is called scroll chaining.
        /// </remarks>
        public static void SetIsScrollChainingEnabled(Control control, bool value)
        {
            control.SetValue(IsScrollChainingEnabledProperty, value);
        }

        /// <summary>
        ///  Gets the value of the IsScrollChainingEnabled attached property.
        /// </summary>
        /// <param name="control">The control to read the value from.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        ///  After a user hits a scroll limit on an element that has been nested within another scrollable element,
        /// you can specify whether that parent element should continue the scrolling operation begun in its child element.
        /// This is called scroll chaining.
        /// </remarks>
        public static bool GetIsScrollChainingEnabled(Control control)
        {
            return control.GetValue(IsScrollChainingEnabledProperty);
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

        /// <summary>
        /// Gets whether scroll gestures should include inertia in their behavior and value.
        /// </summary>
        public static bool GetIsScrollInertiaEnabled(Control control)
        {
            return control.GetValue(IsScrollInertiaEnabledProperty);
        }

        /// <summary>
        /// Sets whether scroll gestures should include inertia in their behavior and value.
        /// </summary>
        public static void SetIsScrollInertiaEnabled(Control control, bool value)
        {
            control.SetValue(IsScrollInertiaEnabledProperty, value);
        }

        /// <summary>
        /// Gets whether dragging of <see cref="Thumb"/> elements should update the <see cref="ScrollViewer"/> only when the user releases the mouse.
        /// </summary>
        public static bool GetIsDeferredScrollingEnabled(Control control) => control.GetValue(IsDeferredScrollingEnabledProperty);

        /// <summary>
        /// Sets whether dragging of <see cref="Thumb"/> elements should update the <see cref="ScrollViewer"/> only when the user releases the mouse.
        /// </summary>
        public static void SetIsDeferredScrollingEnabled(Control control, bool value) => control.SetValue(IsDeferredScrollingEnabledProperty, value);

        /// <inheritdoc/>
        public void RegisterAnchorCandidate(Control element)
        {
            (Presenter as IScrollAnchorProvider)?.RegisterAnchorCandidate(element);
        }

        /// <inheritdoc/>
        public void UnregisterAnchorCandidate(Control element)
        {
            (Presenter as IScrollAnchorProvider)?.UnregisterAnchorCandidate(element);
        }

        protected override bool RegisterContentPresenter(ContentPresenter presenter)
        {
            _childSubscription?.Dispose();
            _childSubscription = null;

            if (base.RegisterContentPresenter(presenter))
            {
                _childSubscription = ((Control?)Presenter)?
                    .GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(ChildChanged);
                return true;
            }

            return false;
        }

        internal static Vector CoerceOffset(AvaloniaObject sender, Vector value)
        {
            var extent = sender.GetValue(ExtentProperty);
            var viewport = sender.GetValue(ViewportProperty);

            var maxX = Math.Max(extent.Width - viewport.Width, 0);
            var maxY = Math.Max(extent.Height - viewport.Height, 0);
            return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
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

        private void ChildChanged(Control? child)
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

        private void LogicalScrollInvalidated(object? sender, EventArgs e)
        {
            CalculatedPropertiesChanged();
        }

        private void CalculatedPropertiesChanged()
        {
            var newMaximum = ScrollBarMaximum;
            if (newMaximum != _oldMaximum)
            {
                RaisePropertyChanged(ScrollBarMaximumProperty, _oldMaximum, newMaximum);
                _oldMaximum = newMaximum;
            }

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
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OffsetProperty)
            {
                CalculatedPropertiesChanged();
            }
            else if (change.Property == ExtentProperty)
            {
                CoerceValue(OffsetProperty);
            }
            else if (change.Property == ViewportProperty)
            {
                CoerceValue(OffsetProperty);
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.Source != this && e.Source is Control c && BringIntoViewOnFocusChange)
                c.BringIntoView();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                PageDown();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when a change in scrolling state is detected, such as a change in scroll
        /// position, extent, or viewport size.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// If you override this method, call `base.OnScrollChanged(ScrollChangedEventArgs)` to
        /// ensure that this event is raised.
        /// </remarks>
        protected virtual void OnScrollChanged(ScrollChangedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _scrollBarExpandSubscription?.Dispose();

            _scrollBarExpandSubscription = SubscribeToScrollBars(e);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ScrollViewerAutomationPeer(this);
        }

        private IDisposable? SubscribeToScrollBars(TemplateAppliedEventArgs e)
        {
            static IObservable<bool>? GetExpandedObservable(ScrollBar? scrollBar)
            {
                return scrollBar?.GetObservable(ScrollBar.IsExpandedProperty);
            }

            var horizontalScrollBar = e.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar");
            var verticalScrollBar = e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar");

            var horizontalExpanded = GetExpandedObservable(horizontalScrollBar);
            var verticalExpanded = GetExpandedObservable(verticalScrollBar);

            IObservable<bool>? actualExpanded = null;

            if (horizontalExpanded != null && verticalExpanded != null)
            {
                actualExpanded = horizontalExpanded.CombineLatest(verticalExpanded, (h, v) => h || v);
            }
            else
            {
                if (horizontalExpanded != null)
                {
                    actualExpanded = horizontalExpanded;
                }
                else if (verticalExpanded != null)
                {
                    actualExpanded = verticalExpanded;
                }
            }

            return actualExpanded?.Subscribe(OnScrollBarExpandedChanged);
        }

        private void OnScrollBarExpandedChanged(bool isExpanded)
        {
            IsExpanded = isExpanded;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => RaiseScrollChanged();

        private void RaiseScrollChanged()
        {
            var extentDelta = new Vector(Extent.Width - _oldExtent.Width, Extent.Height - _oldExtent.Height);
            var offsetDelta = Offset - _oldOffset;
            var viewportDelta = new Vector(Viewport.Width - _oldViewport.Width, Viewport.Height - _oldViewport.Height);

            if (!extentDelta.NearlyEquals(default) || !offsetDelta.NearlyEquals(default) || !viewportDelta.NearlyEquals(default))
            {
                var e = new ScrollChangedEventArgs(extentDelta, offsetDelta, viewportDelta);
                OnScrollChanged(e);

                _oldExtent = Extent;
                _oldOffset = Offset;
                _oldViewport = Viewport;
            }
        }
    }
}
