using System;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Controls.Metadata;
using Avalonia.Automation.Peers;
using Avalonia.VisualTree;
using Avalonia.Reactive;
using System.Linq;

namespace Avalonia.Controls.Primitives
{
    public class ScrollEventArgs : EventArgs
    {
        public ScrollEventArgs(ScrollEventType eventType, double newValue)
        {
            ScrollEventType = eventType;
            NewValue = newValue;
        }
        public double NewValue { get; private set; }
        public ScrollEventType ScrollEventType { get; private set; }
    }

    /// <summary>
    /// A scrollbar control.
    /// </summary>
    [TemplatePart("PART_LineDownButton", typeof(Button))]
    [TemplatePart("PART_LineUpButton",   typeof(Button))]
    [TemplatePart("PART_PageDownButton", typeof(Button))]
    [TemplatePart("PART_PageUpButton",   typeof(Button))]
    [PseudoClasses(":vertical", ":horizontal")]
    public class ScrollBar : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="ViewportSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ViewportSizeProperty =
            AvaloniaProperty.Register<ScrollBar, double>(nameof(ViewportSize), defaultValue: double.NaN);

        /// <summary>
        /// Defines the <see cref="Visibility"/> property.
        /// </summary>
        public static readonly StyledProperty<ScrollBarVisibility> VisibilityProperty =
            AvaloniaProperty.Register<ScrollBar, ScrollBarVisibility>(nameof(Visibility), ScrollBarVisibility.Visible);

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ScrollBar, Orientation>(nameof(Orientation), Orientation.Vertical);

        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollBar, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<ScrollBar, bool>(
                nameof(IsExpanded),
                o => o.IsExpanded);

        /// <summary>
        /// Defines the <see cref="AllowAutoHide"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowAutoHideProperty =
            AvaloniaProperty.Register<ScrollBar, bool>(nameof(AllowAutoHide), true);

        /// <summary>
        /// Defines the <see cref="HideDelay"/> property.
        /// </summary>
        public static readonly StyledProperty<TimeSpan> HideDelayProperty =
            AvaloniaProperty.Register<ScrollBar, TimeSpan>(nameof(HideDelay), TimeSpan.FromSeconds(2));

        /// <summary>
        /// Defines the <see cref="ShowDelay"/> property.
        /// </summary>
        public static readonly StyledProperty<TimeSpan> ShowDelayProperty =
            AvaloniaProperty.Register<ScrollBar, TimeSpan>(nameof(ShowDelay), TimeSpan.FromSeconds(0.5));

        private Button? _lineUpButton;
        private Button? _lineDownButton;
        private Button? _pageUpButton;
        private Button? _pageDownButton;
        private DispatcherTimer? _timer;
        private bool _isExpanded;
        private CompositeDisposable? _ownerSubscriptions;
        private ScrollViewer? _owner;

        /// <summary>
        /// Initializes static members of the <see cref="ScrollBar"/> class. 
        /// </summary>
        static ScrollBar()
        {
            Thumb.DragDeltaEvent.AddClassHandler<ScrollBar>((x, e) => x.OnThumbDragDelta(e), RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<ScrollBar>((x, e) => x.OnThumbDragComplete(e), RoutingStrategies.Bubble);

            FocusableProperty.OverrideMetadata<ScrollBar>(new(false));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollBar"/> class.
        /// </summary>
        public ScrollBar()
        {
            UpdatePseudoClasses(Orientation);
        }

        /// <summary>
        /// Gets or sets the amount of the scrollable content that is currently visible.
        /// </summary>
        public double ViewportSize
        {
            get => GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the scrollbar should hide itself when it
        /// is not needed.
        /// </summary>
        public ScrollBarVisibility Visibility
        {
            get => GetValue(VisibilityProperty);
            set => SetValue(VisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation of the scrollbar.
        /// </summary>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the scrollbar is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            private set => SetAndRaise(IsExpandedProperty, ref _isExpanded, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the scrollbar can hide itself when user is not interacting with it.
        /// </summary>
        public bool AllowAutoHide
        {
            get => GetValue(AllowAutoHideProperty);
            set => SetValue(AllowAutoHideProperty, value);
        }
        
        /// <summary>
        /// Gets a value that determines how long will be the hide delay after user stops interacting with the scrollbar.
        /// </summary>
        public TimeSpan HideDelay
        {
            get => GetValue(HideDelayProperty);
            set => SetValue(HideDelayProperty, value);
        }
        
        /// <summary>
        /// Gets a value that determines how long will be the show delay when user starts interacting with the scrollbar.
        /// </summary>
        public TimeSpan ShowDelay
        {
            get => GetValue(ShowDelayProperty);
            set => SetValue(ShowDelayProperty, value);
        }

        public event EventHandler<ScrollEventArgs>? Scroll;

        /// <summary>
        /// Calculates and updates whether the scrollbar should be visible.
        /// </summary>
        private void UpdateIsVisible()
        {
            var isVisible = Visibility switch
            {
                ScrollBarVisibility.Visible => true,
                ScrollBarVisibility.Disabled => false,
                ScrollBarVisibility.Hidden => false,
                ScrollBarVisibility.Auto => (double.IsNaN(ViewportSize) || Maximum > 0),
                _ => throw new InvalidOperationException("Invalid value for ScrollBar.Visibility.")
            };

            SetCurrentValue(IsVisibleProperty, isVisible);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AttachToScrollViewer();
        }

        /// <summary>
        /// Try to attach to TemplatedParent if it is a <see cref="ScrollViewer"/> and binds to its properties.
        /// Properties which have been set through other means are not bound.
        /// </summary>
        /// <remarks>
        /// This method is automatically called when the control is attached to a visual tree.
        /// </remarks>
        internal void AttachToScrollViewer()
        {
            var owner = this.TemplatedParent as ScrollViewer;

            if (owner == null)
            {
                _owner = null;
                _ownerSubscriptions?.Dispose();
                _ownerSubscriptions = null;
                return;
            }

            if (owner == _owner)
            {
                return;
            }

            _ownerSubscriptions?.Dispose();

            var visibilitySource = Orientation == Orientation.Horizontal ? ScrollViewer.HorizontalScrollBarVisibilityProperty : ScrollViewer.VerticalScrollBarVisibilityProperty;

            var subscriptionDisposables = new IDisposable?[]
            {
                IfUnset(MaximumProperty, p => Bind(p, owner.GetObservable(ScrollViewer.ScrollBarMaximumProperty, ExtractOrdinate), BindingPriority.Template)),
                IfUnset(ValueProperty, p => Bind(p, owner.GetObservable(ScrollViewer.OffsetProperty, ExtractOrdinate), BindingPriority.Template)),
                IfUnset(ScrollViewer.IsDeferredScrollingEnabledProperty, p => Bind(p, owner.GetObservable(ScrollViewer.IsDeferredScrollingEnabledProperty), BindingPriority.Template)),
                IfUnset(ViewportSizeProperty, p => Bind(p, owner.GetObservable(ScrollViewer.ViewportProperty, ExtractOrdinate), BindingPriority.Template)),
                IfUnset(VisibilityProperty, p => Bind(p, owner.GetObservable(visibilitySource), BindingPriority.Template)),
                IfUnset(AllowAutoHideProperty, p => Bind(p, owner.GetObservable(ScrollViewer.AllowAutoHideProperty), BindingPriority.Template)),
                IfUnset(LargeChangeProperty, p => Bind(p, owner.GetObservable(ScrollViewer.LargeChangeProperty).Select(ExtractOrdinate), BindingPriority.Template)),
                IfUnset(SmallChangeProperty, p => Bind(p, owner.GetObservable(ScrollViewer.SmallChangeProperty).Select(ExtractOrdinate), BindingPriority.Template))
            }.Where(d => d != null).Cast<IDisposable>().ToArray();

            _owner = owner;
            _ownerSubscriptions = new CompositeDisposable(subscriptionDisposables);

            IDisposable? IfUnset<T>(T property, Func<T, IDisposable> func) where T : AvaloniaProperty => IsSet(property) ? null : func(property);
        }

        private double ExtractOrdinate(Vector v) => Orientation == Orientation.Horizontal ? v.X : v.Y;
        private double ExtractOrdinate(Size v) => Orientation == Orientation.Horizontal ? v.Width : v.Height;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                LargeDecrement();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                LargeIncrement();
                e.Handled = true;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<Orientation>());
                if (IsAttachedToVisualTree)
                {
                    AttachToScrollViewer(); // there's no way to manually refresh bindings, so reapply them
                }
            }
            else if (change.Property == AllowAutoHideProperty)
            {
                UpdateIsExpandedState();
            }
            else if (change.Property == ValueProperty)
            {
                var value = change.GetNewValue<double>();
                _owner?.SetCurrentValue(ScrollViewer.OffsetProperty, Orientation == Orientation.Horizontal ? _owner.Offset.WithX(value) : _owner.Offset.WithY(value));
            }
            else
            {
                if (change.Property == MinimumProperty ||
                    change.Property == MaximumProperty ||
                    change.Property == ViewportSizeProperty ||
                    change.Property == VisibilityProperty)
                {
                    UpdateIsVisible();
                }
            }
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);

            if (AllowAutoHide)
            {
                ExpandAfterDelay();
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);

            if (AllowAutoHide)
            {
                CollapseAfterDelay();
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_lineUpButton != null)
            {
                _lineUpButton.Click -= LineUpClick;
            }

            if (_lineDownButton != null)
            {
                _lineDownButton.Click -= LineDownClick;
            }

            if (_pageUpButton != null)
            {
                _pageUpButton.Click -= PageUpClick;
            }

            if (_pageDownButton != null)
            {
                _pageDownButton.Click -= PageDownClick;
            }

            _lineUpButton = e.NameScope.Find<Button>("PART_LineUpButton");
            _lineDownButton = e.NameScope.Find<Button>("PART_LineDownButton");
            _pageUpButton = e.NameScope.Find<Button>("PART_PageUpButton");
            _pageDownButton = e.NameScope.Find<Button>("PART_PageDownButton");

            if (_lineUpButton != null)
            {
                _lineUpButton.Click += LineUpClick;
            }

            if (_lineDownButton != null)
            {
                _lineDownButton.Click += LineDownClick;
            }

            if (_pageUpButton != null)
            {
                _pageUpButton.Click += PageUpClick;
            }

            if (_pageDownButton != null)
            {
                _pageDownButton.Click += PageDownClick;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ScrollBarAutomationPeer(this);

        private void InvokeAfterDelay(Action handler, TimeSpan delay)
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
            else
            {
                _timer = new DispatcherTimer(DispatcherPriority.Normal);
                _timer.Tick += (sender, args) =>
                {
                    var senderTimer = (DispatcherTimer)sender!;

                    if (senderTimer.Tag is Action action)
                    {
                        action();
                    }

                    senderTimer.Stop();
                };
            }

            _timer.Tag = handler;
            _timer.Interval = delay;

            _timer.Start();
        }

        private void UpdateIsExpandedState()
        {
            if (!AllowAutoHide)
            {
                _timer?.Stop();

                IsExpanded = true;
            }
            else
            {
                IsExpanded = IsPointerOver;
            }
        }

        private void CollapseAfterDelay()
        {
            InvokeAfterDelay(Collapse, HideDelay);
        }

        private void ExpandAfterDelay()
        {
            InvokeAfterDelay(Expand, ShowDelay);
        }

        private void Collapse()
        {
            IsExpanded = false;
        }

        private void Expand()
        {
            IsExpanded = true;
        }

        private void LineUpClick(object? sender, RoutedEventArgs e)
        {
            SmallDecrement();
        }

        private void LineDownClick(object? sender, RoutedEventArgs e)
        {
            SmallIncrement();
        }

        private void PageUpClick(object? sender, RoutedEventArgs e)
        {
            LargeDecrement();
        }

        private void PageDownClick(object? sender, RoutedEventArgs e)
        {
            LargeIncrement();
        }

        private void SmallDecrement()
        {
            SetCurrentValue(ValueProperty, Math.Max(Value - SmallChange, Minimum));
            OnScroll(ScrollEventType.SmallDecrement);
        }

        private void SmallIncrement()
        {
            SetCurrentValue(ValueProperty, Math.Min(Value + SmallChange, Maximum));
            OnScroll(ScrollEventType.SmallIncrement);
        }

        private void LargeDecrement()
        {
            SetCurrentValue(ValueProperty, Math.Max(Value - LargeChange, Minimum));
            OnScroll(ScrollEventType.LargeDecrement);
        }

        private void LargeIncrement()
        {
            SetCurrentValue(ValueProperty, Math.Min(Value + LargeChange, Maximum));
            OnScroll(ScrollEventType.LargeIncrement);
        }

        private void OnThumbDragDelta(VectorEventArgs e)
        {
            OnScroll(ScrollEventType.ThumbTrack);
        }
        private void OnThumbDragComplete(VectorEventArgs e)
        {
            OnScroll(ScrollEventType.EndScroll);
        }

        protected void OnScroll(ScrollEventType scrollEventType)
        {
            Scroll?.Invoke(this, new ScrollEventArgs(scrollEventType, Value));
        }

        private void UpdatePseudoClasses(Orientation o)
        {
            PseudoClasses.Set(":vertical", o == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
        }
    }
}
