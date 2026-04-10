using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control with two views: A collapsible pane and an area for content
    /// </summary>
    [TemplatePart("PART_PaneRoot", typeof(Panel))]
    [PseudoClasses(pcOpen, pcClosed)]
    [PseudoClasses(pcCompactOverlay, pcCompactInline, pcOverlay, pcInline)]
    [PseudoClasses(pcLeft, pcRight, pcTop, pcBottom)]
    [PseudoClasses(pcLightDismiss)]
    public class SplitView : ContentControl
    {
        private const string pcOpen = ":open";
        private const string pcClosed = ":closed";
        private const string pcCompactOverlay = ":compactoverlay";
        private const string pcCompactInline = ":compactinline";
        private const string pcOverlay = ":overlay";
        private const string pcInline = ":inline";
        private const string pcLeft = ":left";
        private const string pcRight = ":right";
        private const string pcTop = ":top";
        private const string pcBottom = ":bottom";
        private const string pcLightDismiss = ":lightDismiss";
        #region Swipe gesture constants

        private const double SwipeEdgeZoneFraction = 1.0 / 3.0;
        private const double SwipeDirectionLockThreshold = 10;
        private const double SwipeVelocityThreshold = 800;
        private const double SwipeOpenPositionThreshold = 0.4;
        private const double SwipeClosePositionThreshold = 0.6;
        private const int SwipeSnapDurationMs = 200;
        private const int SwipeVelocitySampleCount = 5;

        #endregion

        /// <summary>
        /// Defines the <see cref="CompactPaneLength"/> property
        /// </summary>
        public static readonly StyledProperty<double> CompactPaneLengthProperty =
            AvaloniaProperty.Register<SplitView, double>(
                nameof(CompactPaneLength),
                defaultValue: 48);

        /// <summary>
        /// Defines the <see cref="DisplayMode"/> property
        /// </summary>
        public static readonly StyledProperty<SplitViewDisplayMode> DisplayModeProperty =
            AvaloniaProperty.Register<SplitView, SplitViewDisplayMode>(
                nameof(DisplayMode),
                defaultValue: SplitViewDisplayMode.Overlay);

        /// <summary>
        /// Defines the <see cref="IsPaneOpen"/> property
        /// </summary>
        public static readonly StyledProperty<bool> IsPaneOpenProperty =
            AvaloniaProperty.Register<SplitView, bool>(
                nameof(IsPaneOpen),
                defaultValue: false,
                coerce: CoerceIsPaneOpen);

        /// <summary>
        /// Defines the <see cref="OpenPaneLength"/> property
        /// </summary>
        public static readonly StyledProperty<double> OpenPaneLengthProperty =
            AvaloniaProperty.Register<SplitView, double>(
                nameof(OpenPaneLength),
                defaultValue: 320);

        /// <summary>
        /// Defines the <see cref="PaneBackground"/> property
        /// </summary>
        public static readonly StyledProperty<IBrush?> PaneBackgroundProperty =
            AvaloniaProperty.Register<SplitView, IBrush?>(nameof(PaneBackground));

        /// <summary>
        /// Defines the <see cref="PanePlacement"/> property
        /// </summary>
        public static readonly StyledProperty<SplitViewPanePlacement> PanePlacementProperty =
            AvaloniaProperty.Register<SplitView, SplitViewPanePlacement>(nameof(PanePlacement));

        /// <summary>
        /// Defines the <see cref="Pane"/> property
        /// </summary>
        public static readonly StyledProperty<object?> PaneProperty =
            AvaloniaProperty.Register<SplitView, object?>(nameof(Pane));

        /// <summary>
        /// Defines the <see cref="PaneTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> PaneTemplateProperty =
            AvaloniaProperty.Register<SplitView, IDataTemplate>(nameof(PaneTemplate));

        /// <summary>
        /// Defines the <see cref="UseLightDismissOverlayMode"/> property
        /// </summary>
        public static readonly StyledProperty<bool> UseLightDismissOverlayModeProperty =
            AvaloniaProperty.Register<SplitView, bool>(nameof(UseLightDismissOverlayMode));

        /// <summary>
        /// Defines the <see cref="IsSwipeToOpenEnabled"/> property
        /// </summary>
        public static readonly StyledProperty<bool> IsSwipeToOpenEnabledProperty =
            AvaloniaProperty.Register<SplitView, bool>(nameof(IsSwipeToOpenEnabled), defaultValue: false);

        /// <summary>
        /// Defines the <see cref="TemplateSettings"/> property
        /// </summary>
        public static readonly DirectProperty<SplitView, SplitViewTemplateSettings> TemplateSettingsProperty =
            AvaloniaProperty.RegisterDirect<SplitView, SplitViewTemplateSettings>(nameof(TemplateSettings),
                x => x.TemplateSettings);

        /// <summary>
        /// Defines the <see cref="PaneClosed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PaneClosedEvent =
            RoutedEvent.Register<SplitView, RoutedEventArgs>(
                nameof(PaneClosed),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PaneClosing"/> event.
        /// </summary>
        public static readonly RoutedEvent<CancelRoutedEventArgs> PaneClosingEvent =
            RoutedEvent.Register<SplitView, CancelRoutedEventArgs>(
                nameof(PaneClosing),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PaneOpened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PaneOpenedEvent =
            RoutedEvent.Register<SplitView, RoutedEventArgs>(
                nameof(PaneOpened),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PaneOpening"/> event.
        /// </summary>
        public static readonly RoutedEvent<CancelRoutedEventArgs> PaneOpeningEvent =
            RoutedEvent.Register<SplitView, CancelRoutedEventArgs>(
                nameof(PaneOpening),
                RoutingStrategies.Bubble);

        private Panel? _pane;
        private IDisposable? _pointerDisposable;
        private SplitViewTemplateSettings _templateSettings = new SplitViewTemplateSettings();
        private string? _lastDisplayModePseudoclass;
        private string? _lastPlacementPseudoclass;

        #region Swipe gesture state

        private bool _isSwipeDragging;
        private bool _isSwipeDirectionLocked;
        private bool _isSwipeAnimating;
        private bool _isSwipeToClose;
        private Point _swipeStartPoint;
        private double _swipeCurrentPaneSize;
        private bool _swipeHandlersAttached;
        private readonly List<(DateTime time, double position)> _swipeVelocitySamples = new();

        #endregion

        /// <summary>
        /// Gets or sets the length of the pane when in <see cref="SplitViewDisplayMode.CompactOverlay"/>
        /// or <see cref="SplitViewDisplayMode.CompactInline"/> mode
        /// </summary>
        public double CompactPaneLength
        {
            get => GetValue(CompactPaneLengthProperty);
            set => SetValue(CompactPaneLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="SplitViewDisplayMode"/> for the SplitView
        /// </summary>
        public SplitViewDisplayMode DisplayMode
        {
            get => GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the pane is open or closed
        /// </summary>
        public bool IsPaneOpen
        {
            get => GetValue(IsPaneOpenProperty);
            set => SetValue(IsPaneOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the length of the pane when open
        /// </summary>
        public double OpenPaneLength
        {
            get => GetValue(OpenPaneLengthProperty);
            set => SetValue(OpenPaneLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the background of the pane
        /// </summary>
        public IBrush? PaneBackground
        {
            get => GetValue(PaneBackgroundProperty);
            set => SetValue(PaneBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="SplitViewPanePlacement"/> for the SplitView
        /// </summary>
        public SplitViewPanePlacement PanePlacement
        {
            get => GetValue(PanePlacementProperty);
            set => SetValue(PanePlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the Pane for the SplitView
        /// </summary>
        [DependsOn(nameof(PaneTemplate))]
        public object? Pane
        {
            get => GetValue(PaneProperty);
            set => SetValue(PaneProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate PaneTemplate
        {
            get => GetValue(PaneTemplateProperty);
            set => SetValue(PaneTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets whether WinUI equivalent LightDismissOverlayMode is enabled
        /// <para>When enabled, and the pane is open in Overlay or CompactOverlay mode,
        /// the contents of the <see cref="SplitView"/> are darkened to visually separate the open pane
        /// and the rest of the <see cref="SplitView"/>.</para>
        /// </summary>
        public bool UseLightDismissOverlayMode
        {
            get => GetValue(UseLightDismissOverlayModeProperty);
            set => SetValue(UseLightDismissOverlayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether swipe-from-edge gesture is enabled for opening/closing the pane.
        /// <para>When enabled, the user can swipe from the pane edge to open the pane,
        /// and swipe the open pane back to close it. Supports both touch and mouse input.</para>
        /// </summary>
        public bool IsSwipeToOpenEnabled
        {
            get => GetValue(IsSwipeToOpenEnabledProperty);
            set => SetValue(IsSwipeToOpenEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the TemplateSettings for the <see cref="SplitView"/>.
        /// </summary>
        public SplitViewTemplateSettings TemplateSettings
        {
            get => _templateSettings;
            private set => SetAndRaise(TemplateSettingsProperty, ref _templateSettings, value);
        }

        /// <summary>
        /// Gets whether a swipe gesture is currently in progress (dragging or snap-animating).
        /// </summary>
        public bool IsSwipeGestureActive => _isSwipeDirectionLocked || _isSwipeAnimating;

        /// <summary>
        /// Fired when the pane is closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? PaneClosed
        {
            add => AddHandler(PaneClosedEvent, value);
            remove => RemoveHandler(PaneClosedEvent, value);
        }

        /// <summary>
        /// Fired when the pane is closing.
        /// </summary>
        /// <remarks>
        /// The event args <see cref="CancelRoutedEventArgs.Cancel"/> property may be set to true to cancel the event
        /// and keep the pane open.
        /// </remarks>
        public event EventHandler<CancelRoutedEventArgs>? PaneClosing
        {
            add => AddHandler(PaneClosingEvent, value);
            remove => RemoveHandler(PaneClosingEvent, value);
        }

        /// <summary>
        /// Fired when the pane is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? PaneOpened
        {
            add => AddHandler(PaneOpenedEvent, value);
            remove => RemoveHandler(PaneOpenedEvent, value);
        }

        /// <summary>
        /// Fired when the pane is opening.
        /// </summary>
        /// <remarks>
        /// The event args <see cref="CancelRoutedEventArgs.Cancel"/> property may be set to true to cancel the event
        /// and keep the pane closed.
        /// </remarks>
        public event EventHandler<CancelRoutedEventArgs>? PaneOpening
        {
            add => AddHandler(PaneOpeningEvent, value);
            remove => RemoveHandler(PaneOpeningEvent, value);
        }

        protected override bool RegisterContentPresenter(ContentPresenter presenter)
        {
            var result = base.RegisterContentPresenter(presenter);

            if (presenter.Name == "PART_PanePresenter")
            {
                return true;
            }

            return result;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _pane = e.NameScope.Find<Panel>("PART_PaneRoot");

            UpdateVisualStateForDisplayMode(DisplayMode);
            UpdatePaneStatePseudoClass(IsPaneOpen);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // :left and :right style triggers contain the template so we need to do this as
            // soon as we're attached so the template applies. The other visual states can
            // be updated after the template applies
            UpdateVisualStateForPanePlacementProperty(PanePlacement);

            AttachSwipeHandlers();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _pointerDisposable?.Dispose();
            _pointerDisposable = null;

            DetachSwipeHandlers();
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CompactPaneLengthProperty)
            {
                UpdateVisualStateForCompactPaneLength(change.GetNewValue<double>());
            }
            else if (change.Property == DisplayModeProperty)
            {
                UpdateVisualStateForDisplayMode(change.GetNewValue<SplitViewDisplayMode>());
            }
            else if (change.Property == IsPaneOpenProperty)
            {
                bool isPaneOpen = change.GetNewValue<bool>();
                UpdatePaneStatePseudoClass(isPaneOpen);
                if (isPaneOpen)
                {
                    OnPaneOpened(new RoutedEventArgs(PaneOpenedEvent, this));
                }
                else
                {
                    OnPaneClosed(new RoutedEventArgs(PaneClosedEvent, this));
                }
            }
            else if (change.Property == PaneProperty)
            {
                if (change.OldValue is ILogical oldChild)
                {
                    LogicalChildren.Remove(oldChild);
                }

                if (change.NewValue is ILogical newChild)
                {
                    LogicalChildren.Add(newChild);
                }
            }
            else if (change.Property == PanePlacementProperty)
            {
                UpdateVisualStateForPanePlacementProperty(change.GetNewValue<SplitViewPanePlacement>());
            }
            else if (change.Property == UseLightDismissOverlayModeProperty)
            {
                var mode = change.GetNewValue<bool>();
                PseudoClasses.Set(pcLightDismiss, mode);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && e.Key == Key.Escape)
            {
                if (IsPaneOpen && IsInOverlayMode())
                {
                    SetCurrentValue(IsPaneOpenProperty, false);
                    e.Handled = true;
                }
            }

            base.OnKeyDown(e);
        }

        private void PointerReleasedOutside(object? sender, PointerReleasedEventArgs e)
        {
            if (IsSwipeGestureActive)
                return;

            if (!IsPaneOpen || _pane == null)
            {
                return;
            }

            var closePane = true;
            var src = e.Source as Visual;
            while (src != null)
            {
                // Make assumption that if Popup is in visual tree,
                // owning control is within pane
                // This works because if pane is triggered to close
                // when clicked anywhere else in Window, the pane
                // would close before the popup is opened
                if (src == _pane || src is PopupRoot)
                {
                    closePane = false;
                    break;
                }

                src = src.VisualParent;
            }

            if (closePane)
            {
                SetCurrentValue(IsPaneOpenProperty, false);
                e.Handled = true;
            }
        }

        private bool IsInOverlayMode()
        {
            return (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay);
        }

        protected virtual void OnPaneOpening(CancelRoutedEventArgs args)
        {
            RaiseEvent(args);
        }

        protected virtual void OnPaneOpened(RoutedEventArgs args)
        {
            InvalidateLightDismissSubscription();
            RaiseEvent(args);
        }

        protected virtual void OnPaneClosing(CancelRoutedEventArgs args)
        {
            RaiseEvent(args);
        }

        protected virtual void OnPaneClosed(RoutedEventArgs args)
        {
            _pointerDisposable?.Dispose();
            _pointerDisposable = null;
            RaiseEvent(args);
        }

        /// <summary>
        /// Gets the appropriate PseudoClass for the given <see cref="SplitViewDisplayMode"/>.
        /// </summary>
        private static string GetPseudoClass(SplitViewDisplayMode mode)
        {
            return mode switch
            {
                SplitViewDisplayMode.Inline => pcInline,
                SplitViewDisplayMode.CompactInline => pcCompactInline,
                SplitViewDisplayMode.Overlay => pcOverlay,
                SplitViewDisplayMode.CompactOverlay => pcCompactOverlay,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        /// <summary>
        /// Gets the appropriate PseudoClass for the given <see cref="SplitViewPanePlacement"/>.
        /// </summary>
        private static string GetPseudoClass(SplitViewPanePlacement placement)
        {
            return placement switch
            {
                SplitViewPanePlacement.Left => pcLeft,
                SplitViewPanePlacement.Right => pcRight,
                SplitViewPanePlacement.Top => pcTop,
                SplitViewPanePlacement.Bottom => pcBottom,
                _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null)
            };
        }

        /// <summary>
        /// Called when the <see cref="IsPaneOpen"/> property has to be coerced.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        protected virtual bool OnCoerceIsPaneOpen(bool value)
        {
            CancelRoutedEventArgs eventArgs;

            if (value)
            {
                eventArgs = new CancelRoutedEventArgs(PaneOpeningEvent, this);
                OnPaneOpening(eventArgs);
            }
            else
            {
                eventArgs = new CancelRoutedEventArgs(PaneClosingEvent, this);
                OnPaneClosing(eventArgs);
            }

            if (eventArgs.Cancel)
            {
                return !value;
            }

            return value;
        }

        private void UpdateVisualStateForCompactPaneLength(double newLen)
        {
            var displayMode = DisplayMode;
            if (displayMode == SplitViewDisplayMode.CompactInline)
            {
                TemplateSettings.ClosedPaneWidth = newLen;
                TemplateSettings.ClosedPaneHeight = newLen;
            }
            else if (displayMode == SplitViewDisplayMode.CompactOverlay)
            {
                TemplateSettings.ClosedPaneWidth = newLen;
                TemplateSettings.PaneColumnGridLength = new GridLength(newLen, GridUnitType.Pixel);
                TemplateSettings.ClosedPaneHeight = newLen;
                TemplateSettings.PaneRowGridLength = new GridLength(newLen, GridUnitType.Pixel);
            }
        }

        private void UpdateVisualStateForDisplayMode(SplitViewDisplayMode newValue)
        {
            if (!string.IsNullOrEmpty(_lastDisplayModePseudoclass))
            {
                PseudoClasses.Remove(_lastDisplayModePseudoclass);
            }

            _lastDisplayModePseudoclass = GetPseudoClass(newValue);
            PseudoClasses.Add(_lastDisplayModePseudoclass);

            var (closedPaneWidth, paneColumnGridLength) = newValue switch
            {
                SplitViewDisplayMode.Overlay => (0d, new GridLength(0, GridUnitType.Pixel)),
                SplitViewDisplayMode.CompactOverlay => (CompactPaneLength, new GridLength(CompactPaneLength, GridUnitType.Pixel)),
                SplitViewDisplayMode.Inline => (0d, new GridLength(0, GridUnitType.Auto)),
                SplitViewDisplayMode.CompactInline => (CompactPaneLength, new GridLength(0, GridUnitType.Auto)),
                _ => throw new NotImplementedException(),
            };
            TemplateSettings.ClosedPaneWidth = closedPaneWidth;
            TemplateSettings.PaneColumnGridLength = paneColumnGridLength;

            var (closedPaneHeight, paneRowGridLength) = newValue switch
            {
                SplitViewDisplayMode.Overlay => (0d, new GridLength(0, GridUnitType.Pixel)),
                SplitViewDisplayMode.CompactOverlay => (CompactPaneLength, new GridLength(CompactPaneLength, GridUnitType.Pixel)),
                SplitViewDisplayMode.Inline => (0d, new GridLength(0, GridUnitType.Auto)),
                SplitViewDisplayMode.CompactInline => (CompactPaneLength, new GridLength(0, GridUnitType.Auto)),
                _ => throw new NotImplementedException(),
            };
            TemplateSettings.ClosedPaneHeight = closedPaneHeight;
            TemplateSettings.PaneRowGridLength = paneRowGridLength;

            InvalidateLightDismissSubscription();
        }

        private void UpdateVisualStateForPanePlacementProperty(SplitViewPanePlacement newValue)
        {
            if (!string.IsNullOrEmpty(_lastPlacementPseudoclass))
            {
                PseudoClasses.Remove(_lastPlacementPseudoclass);
            }

            _lastPlacementPseudoclass = GetPseudoClass(newValue);
            PseudoClasses.Add(_lastPlacementPseudoclass);
        }

        private void InvalidateLightDismissSubscription()
        {
            if (_pane == null)
                return;

            // If this returns false, we're not in Overlay or CompactOverlay DisplayMode
            // and don't need the light dismiss behavior
            if (!IsInOverlayMode())
            {
                _pointerDisposable?.Dispose();
                _pointerDisposable = null;
                return;
            }

            if (_pointerDisposable == null)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    _pointerDisposable = Disposable.Create(() =>
                    {
                        topLevel.PointerReleased -= PointerReleasedOutside;
                        topLevel.BackRequested -= TopLevelBackRequested;
                    });

                    topLevel.PointerReleased += PointerReleasedOutside;
                    topLevel.BackRequested += TopLevelBackRequested;
                }
            }
        }

        private void TopLevelBackRequested(object? sender, RoutedEventArgs e)
        {
            if (!IsInOverlayMode())
                return;

            SetCurrentValue(IsPaneOpenProperty, false);
            e.Handled = true;
        }

        private void UpdatePaneStatePseudoClass(bool isPaneOpen)
        {
            if (isPaneOpen)
            {
                PseudoClasses.Add(pcOpen);
                PseudoClasses.Remove(pcClosed);
            }
            else
            {
                PseudoClasses.Add(pcClosed);
                PseudoClasses.Remove(pcOpen);
            }
        }

        /// <summary>
        /// Coerces/validates the <see cref="IsPaneOpen"/> property value.
        /// </summary>
        /// <param name="instance">The <see cref="SplitView"/> instance.</param>
        /// <param name="value">The value to coerce.</param>
        /// <returns>The coerced/validated value.</returns>
        private static bool CoerceIsPaneOpen(AvaloniaObject instance, bool value)
        {
            if (instance is SplitView splitView)
            {
                return splitView.OnCoerceIsPaneOpen(value);
            }

            return value;
        }

        #region Swipe gesture handling

        private bool IsHorizontalPlacement =>
            PanePlacement == SplitViewPanePlacement.Left ||
            PanePlacement == SplitViewPanePlacement.Right;

        /// <summary>
        /// Returns true when the "open" direction is positive in the drag axis.
        /// For Left placement (LTR): rightward = positive X = true.
        /// For Right placement (LTR): leftward = negative X = false.
        /// RTL inverts horizontal placements.
        /// For Top: downward = positive Y = true.
        /// For Bottom: upward = negative Y = false.
        /// </summary>
        private bool IsOpenDirectionPositive
        {
            get
            {
                var placement = PanePlacement;
                bool isRtl = FlowDirection == FlowDirection.RightToLeft;
                return placement switch
                {
                    SplitViewPanePlacement.Left => !isRtl,
                    SplitViewPanePlacement.Right => isRtl,
                    SplitViewPanePlacement.Top => true,
                    SplitViewPanePlacement.Bottom => false,
                    _ => true
                };
            }
        }

        private void AttachSwipeHandlers()
        {
            if (_swipeHandlersAttached)
                return;
            _swipeHandlersAttached = true;

            AddHandler(InputElement.PointerPressedEvent, OnSwipePointerPressed, RoutingStrategies.Tunnel);
            AddHandler(InputElement.PointerMovedEvent, OnSwipePointerMoved, RoutingStrategies.Tunnel);
            AddHandler(InputElement.PointerReleasedEvent, OnSwipePointerReleased, RoutingStrategies.Tunnel);
            AddHandler(InputElement.PointerCaptureLostEvent, OnSwipePointerCaptureLost, RoutingStrategies.Tunnel);
            SizeChanged += OnSwipeSizeChanged;
        }

        private void DetachSwipeHandlers()
        {
            if (!_swipeHandlersAttached)
                return;
            _swipeHandlersAttached = false;

            RemoveHandler(InputElement.PointerPressedEvent, OnSwipePointerPressed);
            RemoveHandler(InputElement.PointerMovedEvent, OnSwipePointerMoved);
            RemoveHandler(InputElement.PointerReleasedEvent, OnSwipePointerReleased);
            RemoveHandler(InputElement.PointerCaptureLostEvent, OnSwipePointerCaptureLost);
            SizeChanged -= OnSwipeSizeChanged;
        }

        private bool IsInSwipeEdgeZone(Point point)
        {
            var placement = PanePlacement;
            bool isRtl = FlowDirection == FlowDirection.RightToLeft;

            if (IsHorizontalPlacement)
            {
                var width = Bounds.Width;
                var edgeZone = width * SwipeEdgeZoneFraction;

                bool paneOnLeft = (placement == SplitViewPanePlacement.Left && !isRtl) ||
                                  (placement == SplitViewPanePlacement.Right && isRtl);
                return paneOnLeft ? point.X <= edgeZone : point.X >= width - edgeZone;
            }
            else
            {
                var height = Bounds.Height;
                var edgeZone = height * SwipeEdgeZoneFraction;

                return placement == SplitViewPanePlacement.Top
                    ? point.Y <= edgeZone
                    : point.Y >= height - edgeZone;
            }
        }

        /// <summary>
        /// Gets the drag delta along the relevant axis, signed so that
        /// positive = toward-open direction.
        /// </summary>
        private double GetSwipeDelta(Point current)
        {
            double raw;
            if (IsHorizontalPlacement)
                raw = current.X - _swipeStartPoint.X;
            else
                raw = current.Y - _swipeStartPoint.Y;

            return IsOpenDirectionPositive ? raw : -raw;
        }

        /// <summary>
        /// Gets the position along the drag axis for velocity tracking.
        /// </summary>
        private double GetSwipeAxisPosition(Point point) =>
            IsHorizontalPlacement ? point.X : point.Y;

        private void SetSwipePaneSize(double size)
        {
            if (_pane == null) return;
            _swipeCurrentPaneSize = size;

            if (IsHorizontalPlacement)
                _pane.Width = size;
            else
                _pane.Height = size;
        }

        private void ClearSwipePaneSizeOverride()
        {
            if (_pane == null) return;

            if (IsHorizontalPlacement)
                _pane.ClearValue(Layoutable.WidthProperty);
            else
                _pane.ClearValue(Layoutable.HeightProperty);
        }

        private void SuppressPaneTransitions()
        {
            if (_pane == null) return;
            _pane.SetValue(Animatable.TransitionsProperty, new Transitions());
        }

        private void RestorePaneTransitions()
        {
            _pane?.ClearValue(Animatable.TransitionsProperty);
        }

        private void OnSwipePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!IsSwipeToOpenEnabled || _isSwipeDragging || _isSwipeAnimating || _pane == null)
                return;

            var pointerType = e.Pointer.Type;
            if (pointerType != PointerType.Touch && pointerType != PointerType.Mouse)
                return;

            var point = e.GetPosition(this);

            if (IsPaneOpen)
            {
                _isSwipeToClose = true;
            }
            else
            {
                if (!IsInSwipeEdgeZone(point))
                    return;
                _isSwipeToClose = false;
            }

            _swipeStartPoint = point;
            _isSwipeDragging = true;
            _isSwipeDirectionLocked = false;
            _swipeVelocitySamples.Clear();
            SwipeRecordVelocitySample(GetSwipeAxisPosition(point));
        }

        private void OnSwipePointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isSwipeDragging || _pane == null)
                return;

            var point = e.GetPosition(this);
            var delta = GetSwipeDelta(point);

            if (!_isSwipeDirectionLocked)
            {
                double absPrimary, absSecondary;
                if (IsHorizontalPlacement)
                {
                    absPrimary = Math.Abs(point.X - _swipeStartPoint.X);
                    absSecondary = Math.Abs(point.Y - _swipeStartPoint.Y);
                }
                else
                {
                    absPrimary = Math.Abs(point.Y - _swipeStartPoint.Y);
                    absSecondary = Math.Abs(point.X - _swipeStartPoint.X);
                }

                if (absPrimary < SwipeDirectionLockThreshold &&
                    absSecondary < SwipeDirectionLockThreshold)
                    return;

                // Must be more along the primary axis than the secondary
                if (absSecondary > absPrimary)
                {
                    SwipeCancelDrag(e.Pointer);
                    return;
                }

                // For open: delta must be positive (toward open)
                if (!_isSwipeToClose && delta <= 0)
                {
                    SwipeCancelDrag(e.Pointer);
                    return;
                }

                // For close: delta must be negative (toward close)
                if (_isSwipeToClose && delta >= 0)
                {
                    SwipeCancelDrag(e.Pointer);
                    return;
                }

                _isSwipeDirectionLocked = true;
                e.Pointer.Capture(this);

                // Suppress XAML theme transitions during drag
                SuppressPaneTransitions();


                // For swipe-to-open: start with pane at 0 size
                if (!_isSwipeToClose)
                {
                    SetSwipePaneSize(0);
                }
            }

            SwipeRecordVelocitySample(GetSwipeAxisPosition(point));

            var target = OpenPaneLength;
            if (target <= 0) return;

            if (_isSwipeToClose)
            {
                var absDelta = Math.Abs(delta);
                _swipeCurrentPaneSize = Math.Max(0, Math.Min(target, target - absDelta));
            }
            else
            {
                _swipeCurrentPaneSize = Math.Max(0, Math.Min(target, delta));
            }

            SetSwipePaneSize(_swipeCurrentPaneSize);
            e.Handled = true;
        }

        private void OnSwipePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isSwipeDragging)
                return;

            var point = e.GetPosition(this);
            SwipeRecordVelocitySample(GetSwipeAxisPosition(point));

            if (!_isSwipeDirectionLocked)
            {
                // No gesture occurred — don't release capture (would steal button's capture)
                _isSwipeDragging = false;
                return;
            }

            // Clear _isSwipeDragging BEFORE releasing capture so that the synchronous
            // OnSwipePointerCaptureLost handler is a no-op (it also checks _isSwipeDragging).
            _isSwipeDragging = false;
            e.Pointer.Capture(null);

            var target = OpenPaneLength;
            var velocity = SwipeCalculateVelocity();

            bool shouldOpen;
            if (_isSwipeToClose)
            {
                // Velocity is in raw axis units; convert to open-direction sign
                double openDirVelocity = IsOpenDirectionPositive ? -velocity : velocity;
                shouldOpen = !(openDirVelocity > SwipeVelocityThreshold ||
                               _swipeCurrentPaneSize < target * SwipeClosePositionThreshold);
            }
            else
            {
                double openDirVelocity = IsOpenDirectionPositive ? velocity : -velocity;
                shouldOpen = openDirVelocity > SwipeVelocityThreshold ||
                             _swipeCurrentPaneSize > target * SwipeOpenPositionThreshold;
            }

            SwipeAnimateToState(shouldOpen, target);
        }

        private void OnSwipePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            if (!_isSwipeDragging)
                return;

            if (_isSwipeDirectionLocked)
            {
                var wasOpen = _isSwipeToClose;
                _isSwipeDragging = false;
                SwipeAnimateToState(wasOpen, OpenPaneLength);
            }
            else
            {
                _isSwipeDragging = false;
            }
        }

        private void OnSwipeSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_isSwipeDragging && !_isSwipeDirectionLocked)
                return;

            // Cancel gesture on resize
            var wasOpen = _isSwipeToClose;
            _isSwipeDragging = false;
            _isSwipeDirectionLocked = false;
            _isSwipeAnimating = false;

            ClearSwipePaneSizeOverride();
            RestorePaneTransitions();
            SetCurrentValue(IsPaneOpenProperty, wasOpen);
        }

        private void SwipeCancelDrag(IPointer pointer)
        {
            _isSwipeDragging = false;
            _isSwipeDirectionLocked = false;
            pointer.Capture(null);
        }

        private void SwipeRecordVelocitySample(double position)
        {
            _swipeVelocitySamples.Add((DateTime.UtcNow, position));
            while (_swipeVelocitySamples.Count > SwipeVelocitySampleCount)
                _swipeVelocitySamples.RemoveAt(0);
        }

        private double SwipeCalculateVelocity()
        {
            if (_swipeVelocitySamples.Count < 2) return 0;

            var first = _swipeVelocitySamples[0];
            var last = _swipeVelocitySamples[_swipeVelocitySamples.Count - 1];
            var dt = (last.time - first.time).TotalSeconds;
            if (dt <= 0) return 0;

            return (last.position - first.position) / dt;
        }

        private void SwipeAnimateToState(bool open, double targetWidth)
        {
            var from = _swipeCurrentPaneSize;
            var to = open ? targetWidth : 0;

            if (Math.Abs(from - to) < 1)
            {
                SwipeFinalizeState(open, targetWidth);
                return;
            }

            _isSwipeAnimating = true;
            var easing = new CubicEaseOut();
            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(SwipeSnapDurationMs);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) =>
            {
                var elapsed = DateTime.UtcNow - startTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                var easedProgress = easing.Ease(progress);
                var current = from + (to - from) * easedProgress;

                SetSwipePaneSize(Math.Max(0, current));

                if (progress >= 1.0)
                {
                    timer.Stop();
                    SwipeFinalizeState(open, targetWidth);
                    _isSwipeAnimating = false;
                    _isSwipeDirectionLocked = false;
                }
            };
            timer.Start();
        }

        private void SwipeFinalizeState(bool open, double targetWidth)
        {
            // Set the pane to the exact final size before committing state
            SetSwipePaneSize(open ? targetWidth : 0);

            // Commit the IsPaneOpen state — events fire, pseudo-classes update
            SetCurrentValue(IsPaneOpenProperty, open);

            // Clear the local Width/Height override so style takes over
            ClearSwipePaneSizeOverride();

            // Restore theme transitions and remove swiping pseudo-class
            // Done on next dispatcher tick so IsSwipeGestureActive stays true
            // through any PaneClosing events fired on this event cycle
            if (!_isSwipeAnimating)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RestorePaneTransitions();
                    _isSwipeDirectionLocked = false;
                }, DispatcherPriority.Input);
            }
            else
            {
                RestorePaneTransitions();
            }
        }

        #endregion
    }
}
