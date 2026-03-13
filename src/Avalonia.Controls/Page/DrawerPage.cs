using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A page that provides a drawer pattern.
    /// </summary>
    [TemplatePart("PART_DrawerPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_DrawerHeader", typeof(ContentPresenter))]
    [TemplatePart("PART_DrawerFooter", typeof(ContentPresenter))]
    [TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_SplitView", typeof(SplitView))]
    [TemplatePart("PART_TopBar", typeof(Border))]
    [TemplatePart("PART_PaneButton", typeof(ToggleButton))]
    [TemplatePart("PART_CompactPaneToggle", typeof(ToggleButton))]
    [TemplatePart("PART_Backdrop", typeof(Border))]
    [TemplatePart("PART_CompactPaneIconPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_PaneIconPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_BottomPaneIconPresenter", typeof(ContentPresenter))]
    [PseudoClasses(":placement-right", ":placement-top", ":placement-bottom", ":detail-is-navpage")]
    public class DrawerPage : Page
    {
        /// <summary>
        /// Defines the <see cref="Opened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
            RoutedEvent.Register<DrawerPage, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Closing"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<DrawerClosingEventArgs> ClosingEvent =
            RoutedEvent.Register<DrawerPage, DrawerClosingEventArgs>(nameof(Closing), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Closed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
            RoutedEvent.Register<DrawerPage, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Drawer"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DrawerProperty =
            AvaloniaProperty.Register<DrawerPage, object?>(nameof(Drawer));

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            AvaloniaProperty.Register<DrawerPage, object?>(nameof(Content));

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsOpenProperty =
            AvaloniaProperty.Register<DrawerPage, bool>(nameof(IsOpen), coerce: CoerceIsOpen);

        /// <summary>
        /// Defines the <see cref="DrawerLength"/> property.
        /// </summary>
        public static readonly StyledProperty<double> DrawerLengthProperty =
            AvaloniaProperty.Register<DrawerPage, double>(nameof(DrawerLength), 320,
                validate: ValidateLength);

        /// <summary>
        /// Defines the <see cref="CompactDrawerLength"/> property.
        /// </summary>
        public static readonly StyledProperty<double> CompactDrawerLengthProperty =
            AvaloniaProperty.Register<DrawerPage, double>(nameof(CompactDrawerLength), 48,
                validate: ValidateLength);

        /// <summary>
        /// Defines the <see cref="DrawerBreakpointLength"/> property.
        /// </summary>
        public static readonly StyledProperty<double> DrawerBreakpointLengthProperty =
            AvaloniaProperty.Register<DrawerPage, double>(nameof(DrawerBreakpointLength), 0d);

        /// <summary>
        /// Defines the <see cref="IsGestureEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsGestureEnabledProperty =
            AvaloniaProperty.Register<DrawerPage, bool>(nameof(IsGestureEnabled), true);

        /// <summary>
        /// Defines the <see cref="DrawerBehavior"/> property.
        /// </summary>
        public static readonly StyledProperty<DrawerBehavior> DrawerBehaviorProperty =
            AvaloniaProperty.Register<DrawerPage, DrawerBehavior>(nameof(DrawerBehavior), DrawerBehavior.Auto);

        /// <summary>
        /// Defines the <see cref="DrawerLayoutBehavior"/> property.
        /// </summary>
        public static readonly StyledProperty<DrawerLayoutBehavior> DrawerLayoutBehaviorProperty =
            AvaloniaProperty.Register<DrawerPage, DrawerLayoutBehavior>(nameof(DrawerLayoutBehavior), DrawerLayoutBehavior.Overlay);

        /// <summary>
        /// Defines the <see cref="DrawerPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<DrawerPlacement> DrawerPlacementProperty =
            AvaloniaProperty.Register<DrawerPage, DrawerPlacement>(nameof(DrawerPlacement), DrawerPlacement.Left);

        /// <summary>
        /// Defines the <see cref="DrawerHeader"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DrawerHeaderProperty =
            AvaloniaProperty.Register<DrawerPage, object?>(nameof(DrawerHeader));

        /// <summary>
        /// Defines the <see cref="DrawerFooter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DrawerFooterProperty =
            AvaloniaProperty.Register<DrawerPage, object?>(nameof(DrawerFooter));

        /// <summary>
        /// Defines the <see cref="DrawerIcon"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DrawerIconProperty =
            AvaloniaProperty.Register<DrawerPage, object?>(nameof(DrawerIcon));

        private static readonly DefaultPageDataTemplate s_defaultPageDataTemplate = new DefaultPageDataTemplate();

        /// <summary>
        /// Defines the <see cref="DrawerTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> DrawerTemplateProperty =
            AvaloniaProperty.Register<DrawerPage, IDataTemplate?>(nameof(DrawerTemplate));

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            AvaloniaProperty.Register<DrawerPage, IDataTemplate?>(nameof(ContentTemplate), s_defaultPageDataTemplate);

        /// <summary>
        /// Defines the <see cref="DrawerBackground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> DrawerBackgroundProperty =
            AvaloniaProperty.Register<DrawerPage, IBrush?>(nameof(DrawerBackground));

        /// <summary>
        /// Defines the <see cref="DrawerHeaderBackground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> DrawerHeaderBackgroundProperty =
            AvaloniaProperty.Register<DrawerPage, IBrush?>(nameof(DrawerHeaderBackground));

        /// <summary>
        /// Defines the <see cref="DrawerHeaderForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> DrawerHeaderForegroundProperty =
            AvaloniaProperty.Register<DrawerPage, IBrush?>(nameof(DrawerHeaderForeground));

        /// <summary>
        /// Defines the <see cref="DrawerFooterBackground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> DrawerFooterBackgroundProperty =
            AvaloniaProperty.Register<DrawerPage, IBrush?>(nameof(DrawerFooterBackground));

        /// <summary>
        /// Defines the <see cref="DrawerFooterForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> DrawerFooterForegroundProperty =
            AvaloniaProperty.Register<DrawerPage, IBrush?>(nameof(DrawerFooterForeground));

        /// <summary>
        /// Defines the <see cref="BackdropBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackdropBrushProperty =
            AvaloniaProperty.Register<DrawerPage, IBrush?>(nameof(BackdropBrush));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<DrawerPage>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<DrawerPage>();

        /// <summary>
        /// Defines the <see cref="DisplayMode"/> property.
        /// </summary>
        public static readonly StyledProperty<SplitViewDisplayMode> DisplayModeProperty =
            SplitView.DisplayModeProperty.AddOwner<DrawerPage>();


        private ContentPresenter? _contentPresenter;
        private ContentPresenter? _drawerPresenter;
        private ContentPresenter? _drawerHeaderPresenter;
        private ContentPresenter? _drawerFooterPresenter;
        private ContentPresenter? _compactPaneIconPresenter;
        private ContentPresenter? _paneIconPresenter;
        private ContentPresenter? _bottomPaneIconPresenter;
        private SplitView? _splitView;
        private Border? _topBar;
        private ToggleButton? _paneButton;
        private Border? _backdrop;
        private IDisposable? _navBarVisibleSub;

        private const double EdgeGestureWidth = 20;
        private bool _suppressDrawerEvents;
        private bool _hasHadFirstPage;
        private Point _swipeStartPoint;
        private readonly SwipeGestureRecognizer _swipeRecognizer = new SwipeGestureRecognizer();

        private bool IsRtl => FlowDirection == FlowDirection.RightToLeft;

        private bool IsVerticalPlacement => DrawerPlacement == DrawerPlacement.Top || DrawerPlacement == DrawerPlacement.Bottom;

        private bool IsPaneOnRight => (DrawerPlacement == DrawerPlacement.Right) != IsRtl;

        /// <summary>
        /// Occurs when <see cref="IsOpen"/> changes to <see langword="true"/>.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        /// <summary>
        /// Occurs when the drawer is about to close.
        /// </summary>
        public event EventHandler<DrawerClosingEventArgs>? Closing
        {
            add => AddHandler(ClosingEvent, value);
            remove => RemoveHandler(ClosingEvent, value);
        }

        /// <summary>
        /// Occurs when <see cref="IsOpen"/> changes to <see langword="false"/>
        /// and closing is not cancelled.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        private static bool ValidateLength(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;

        private static bool CoerceIsOpen(AvaloniaObject instance, bool value)
        {
            var drawer = (DrawerPage)instance;
            if (drawer._suppressDrawerEvents)
                return value;

            if (value && drawer.DrawerBehavior == DrawerBehavior.Disabled)
                return false;

            if (!value && drawer.GetValue(IsOpenProperty) && drawer.DrawerBehavior != DrawerBehavior.Disabled)
            {
                var args = new DrawerClosingEventArgs(ClosingEvent);
                drawer.RaiseEvent(args);
                if (args.Cancel)
                    return true;
            }

            return value;
        }

        static DrawerPage()
        {
            PageNavigationSystemBackButtonPressedEvent.AddClassHandler<DrawerPage>((sender, eventArgs) =>
            {
                if (sender.IsOpen
                    && sender.DrawerBehavior != DrawerBehavior.Locked
                    && sender.DrawerBehavior != DrawerBehavior.Disabled)
                {
                    sender.IsOpen = false;
                    eventArgs.Handled = true;
                }
            });
        }

        public DrawerPage()
        {
            _swipeRecognizer.IsMouseEnabled = true;
            GestureRecognizers.Add(_swipeRecognizer);
            AddHandler(PointerPressedEvent, OnSwipePointerPressed, handledEventsToo: true);
            UpdateSwipeRecognizerAxes();
        }

        /// <summary>
        /// Gets or sets the drawer pane content.
        /// </summary>
        [DependsOn(nameof(DrawerTemplate))]
        public object? Drawer
        {
            get => GetValue(DrawerProperty);
            set => SetValue(DrawerProperty, value);
        }

        /// <summary>
        /// Gets or sets the main content.
        /// </summary>
        [Content]
        [DependsOn(nameof(ContentTemplate))]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the drawer pane is currently open.
        /// </summary>
        public bool IsOpen
        {
            get => GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the drawer pane.
        /// </summary>
        public double DrawerLength
        {
            get => GetValue(DrawerLengthProperty);
            set => SetValue(DrawerLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the compact drawer width.
        /// </summary>
        public double CompactDrawerLength
        {
            get => GetValue(CompactDrawerLengthProperty);
            set => SetValue(CompactDrawerLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the size threshold for switching to overlay mode. Set to 0 to disable.
        /// </summary>
        public double DrawerBreakpointLength
        {
            get => GetValue(DrawerBreakpointLengthProperty);
            set => SetValue(DrawerBreakpointLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets whether swipe gestures can open or close the drawer.
        /// </summary>
        public bool IsGestureEnabled
        {
            get => GetValue(IsGestureEnabledProperty);
            set => SetValue(IsGestureEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the open/close behavior of the drawer pane.
        /// </summary>
        public DrawerBehavior DrawerBehavior
        {
            get => GetValue(DrawerBehaviorProperty);
            set => SetValue(DrawerBehaviorProperty, value);
        }

        /// <summary>
        /// Gets or sets the layout behavior of the drawer.
        /// </summary>
        public DrawerLayoutBehavior DrawerLayoutBehavior
        {
            get => GetValue(DrawerLayoutBehaviorProperty);
            set => SetValue(DrawerLayoutBehaviorProperty, value);
        }

        /// <summary>
        /// Gets or sets which edge of the control the drawer appears from.
        /// </summary>
        public DrawerPlacement DrawerPlacement
        {
            get => GetValue(DrawerPlacementProperty);
            set => SetValue(DrawerPlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the header content displayed at the top of the drawer pane.
        /// </summary>
        public object? DrawerHeader
        {
            get => GetValue(DrawerHeaderProperty);
            set => SetValue(DrawerHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the footer content displayed at the bottom of the drawer pane.
        /// </summary>
        public object? DrawerFooter
        {
            get => GetValue(DrawerFooterProperty);
            set => SetValue(DrawerFooterProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon displayed in the drawer toggle button.
        /// </summary>
        public object? DrawerIcon
        {
            get => GetValue(DrawerIconProperty);
            set => SetValue(DrawerIconProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display <see cref="Drawer"/> content.
        /// </summary>
        public IDataTemplate? DrawerTemplate
        {
            get => GetValue(DrawerTemplateProperty);
            set => SetValue(DrawerTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display <see cref="Content"/>.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the drawer pane.
        /// </summary>
        public IBrush? DrawerBackground
        {
            get => GetValue(DrawerBackgroundProperty);
            set => SetValue(DrawerBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the drawer header area.
        /// </summary>
        public IBrush? DrawerHeaderBackground
        {
            get => GetValue(DrawerHeaderBackgroundProperty);
            set => SetValue(DrawerHeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the drawer header area.
        /// </summary>
        public IBrush? DrawerHeaderForeground
        {
            get => GetValue(DrawerHeaderForegroundProperty);
            set => SetValue(DrawerHeaderForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush of the drawer footer area.
        /// </summary>
        public IBrush? DrawerFooterBackground
        {
            get => GetValue(DrawerFooterBackgroundProperty);
            set => SetValue(DrawerFooterBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush of the drawer footer area.
        /// </summary>
        public IBrush? DrawerFooterForeground
        {
            get => GetValue(DrawerFooterForegroundProperty);
            set => SetValue(DrawerFooterForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the backdrop brush for overlay mode.
        /// </summary>
        public IBrush? BackdropBrush
        {
            get => GetValue(BackdropBrushProperty);
            set => SetValue(BackdropBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the detail content.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the detail content.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="SplitViewDisplayMode"/>.
        /// </summary>
        public SplitViewDisplayMode DisplayMode
        {
            get => GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            DetachBackdropPointerPressed();

            _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
            _drawerPresenter = e.NameScope.Find<ContentPresenter>("PART_DrawerPresenter");
            _drawerHeaderPresenter = e.NameScope.Find<ContentPresenter>("PART_DrawerHeader");
            _drawerFooterPresenter = e.NameScope.Find<ContentPresenter>("PART_DrawerFooter");
            _compactPaneIconPresenter = e.NameScope.Find<ContentPresenter>("PART_CompactPaneIconPresenter");
            _paneIconPresenter = e.NameScope.Find<ContentPresenter>("PART_PaneIconPresenter");
            _bottomPaneIconPresenter = e.NameScope.Find<ContentPresenter>("PART_BottomPaneIconPresenter");
            _splitView = e.NameScope.Find<SplitView>("PART_SplitView");
            _topBar = e.NameScope.Find<Border>("PART_TopBar");
            _paneButton = e.NameScope.Find<ToggleButton>("PART_PaneButton");
            _backdrop = e.NameScope.Find<Border>("PART_Backdrop");

            UpdateIconPresenters();

            if (_backdrop != null)
            {
                if (IsAttachedToVisualTree)
                    AttachBackdropPointerPressed();

                AutomationProperties.SetAccessibilityView(_backdrop, AccessibilityView.Raw);
            }

            ApplyForeground(_drawerHeaderPresenter, DrawerHeaderForeground);
            ApplyForeground(_drawerFooterPresenter, DrawerFooterForeground);

            ApplyDrawerBackground();
            UpdateSplitViewDisplayMode();
            UpdatePanePlacement();
            UpdateBackdropState();
            UpdateContentSafeAreaPadding();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DrawerIconProperty)
            {
                UpdateIconPresenters();
            }
            else if (change.Property == DrawerProperty || change.Property == ContentProperty)
            {
                if (change.OldValue is ILogical oldLogical)
                    LogicalChildren.Remove(oldLogical);
                if (change.NewValue is ILogical newLogical)
                    LogicalChildren.Add(newLogical);
                UpdateActivePage();

                if (change.Property == ContentProperty)
                {
                    _navBarVisibleSub?.Dispose();
                    _navBarVisibleSub = null;

                    if (change.OldValue is NavigationPage oldNav)
                        oldNav.SetDrawerPage(null);
                    if (change.NewValue is NavigationPage newNav)
                    {
                        newNav.SetDrawerPage(this);
                        _navBarVisibleSub = newNav.GetObservable(NavigationPage.IsNavBarEffectivelyVisibleProperty)
                            .Subscribe(new AnonymousObserver<bool>(_ => UpdateDetailNavBarVisiblePseudoClass()));
                    }
                    UpdateDetailNavBarVisiblePseudoClass();
                }
            }
            else if (change.Property == IsOpenProperty || change.Property == DisplayModeProperty)
            {
                SyncCurrentPage();
                UpdateBackdropState();

                if (change.Property == IsOpenProperty)
                {
                    UpdateDrawerFocus();

                    if (!_suppressDrawerEvents)
                    {
                        if (change.GetNewValue<bool>())
                            RaiseEvent(new RoutedEventArgs(OpenedEvent));
                        else
                            RaiseEvent(new RoutedEventArgs(ClosedEvent));
                    }
                }
            }
            else if (change.Property == DrawerBackgroundProperty)
            {
                ApplyDrawerBackground();
            }
            else if (change.Property == DrawerBehaviorProperty ||
                     change.Property == DrawerLayoutBehaviorProperty ||
                     change.Property == DrawerBreakpointLengthProperty)
            {
                UpdateSplitViewDisplayMode();
            }
            else if (change.Property == BoundsProperty && DrawerBreakpointLength > 0)
            {
                UpdateSplitViewDisplayMode();
            }
            else if (change.Property == IsGestureEnabledProperty)
            {
                _swipeRecognizer.IsEnabled = change.GetNewValue<bool>();
            }
            else if (change.Property == DrawerPlacementProperty)
            {
                UpdateSwipeRecognizerAxes();
                UpdatePanePlacement();
                UpdateContentSafeAreaPadding();
            }
            else if (change.Property == BackdropBrushProperty)
            {
                UpdateBackdropState();
            }
            else if (change.Property == FlowDirectionProperty)
            {
                UpdateContentSafeAreaPadding();
            }
            else if (change.Property == DrawerHeaderForegroundProperty)
            {
                ApplyForeground(_drawerHeaderPresenter, change.GetNewValue<IBrush?>());
            }
            else if (change.Property == DrawerFooterForegroundProperty)
            {
                ApplyForeground(_drawerFooterPresenter, change.GetNewValue<IBrush?>());
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            AddHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);

            AttachBackdropPointerPressed();
            RestoreNavigationState();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            RemoveHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);

            DetachBackdropPointerPressed();

            ClearNavigationState();
        }

        private void RestoreNavigationState()
        {
            if (Content is not NavigationPage nav)
                return;

            _navBarVisibleSub?.Dispose();
            nav.SetDrawerPage(this);
            _navBarVisibleSub = nav.GetObservable(NavigationPage.IsNavBarEffectivelyVisibleProperty)
                .Subscribe(new AnonymousObserver<bool>(_ => UpdateDetailNavBarVisiblePseudoClass()));
            UpdateDetailNavBarVisiblePseudoClass();
        }

        private void ClearNavigationState()
        {
            _navBarVisibleSub?.Dispose();
            _navBarVisibleSub = null;

            if (Content is NavigationPage nav)
                nav.SetDrawerPage(null);
        }

        private void AttachBackdropPointerPressed()
        {
            if (_backdrop != null)
                _backdrop.PointerPressed += OnBackdropPressed;
        }

        private void DetachBackdropPointerPressed()
        {
            if (_backdrop != null)
                _backdrop.PointerPressed -= OnBackdropPressed;
        }

        private void UpdateSwipeRecognizerAxes()
        {
            _swipeRecognizer.CanVerticallySwipe = IsVerticalPlacement;
            _swipeRecognizer.CanHorizontallySwipe = !IsVerticalPlacement;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (!_hasHadFirstPage && CurrentPage != null)
            {
                _hasHadFirstPage = true;
                CurrentPage.SendNavigatedTo(new NavigatedToEventArgs(null, NavigationType.Push));
            }
        }

        private void OnSwipePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _swipeStartPoint = e.GetPosition(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape && IsOpen &&
                DrawerBehavior != DrawerBehavior.Locked &&
                (DisplayMode == SplitViewDisplayMode.Overlay || DisplayMode == SplitViewDisplayMode.CompactOverlay))
            {
                SetCurrentValue(IsOpenProperty, false);
                e.Handled = true;
            }
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            if (!IsGestureEnabled ||
                DrawerBehavior == DrawerBehavior.Disabled ||
                DrawerBehavior == DrawerBehavior.Locked ||
                DisplayMode == SplitViewDisplayMode.Inline ||
                Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            if (IsVerticalPlacement)
            {
                if (e.SwipeDirection != SwipeDirection.Up && e.SwipeDirection != SwipeDirection.Down)
                    return;

                bool towardPane = DrawerPlacement == DrawerPlacement.Bottom
                    ? e.SwipeDirection == SwipeDirection.Up
                    : e.SwipeDirection == SwipeDirection.Down;

                if (!IsOpen)
                {
                    double openGestureEdge = DisplayMode == SplitViewDisplayMode.CompactOverlay ||
                                             DisplayMode == SplitViewDisplayMode.CompactInline
                        ? CompactDrawerLength
                        : EdgeGestureWidth;

                    bool inEdge = DrawerPlacement == DrawerPlacement.Bottom
                        ? _swipeStartPoint.Y >= Bounds.Height - openGestureEdge
                        : _swipeStartPoint.Y <= openGestureEdge;

                    if (towardPane && inEdge)
                    {
                        SetCurrentValue(IsOpenProperty, true);
                        e.Handled = true;
                    }
                }
                else if (!towardPane)
                {
                    SetCurrentValue(IsOpenProperty, false);
                    e.Handled = true;
                }
            }
            else
            {
                if (e.SwipeDirection != SwipeDirection.Left && e.SwipeDirection != SwipeDirection.Right)
                    return;

                bool towardPane = IsPaneOnRight
                    ? e.SwipeDirection == SwipeDirection.Left
                    : e.SwipeDirection == SwipeDirection.Right;

                if (!IsOpen)
                {
                    double openGestureEdge = DisplayMode == SplitViewDisplayMode.CompactOverlay ||
                                             DisplayMode == SplitViewDisplayMode.CompactInline
                        ? CompactDrawerLength
                        : EdgeGestureWidth;

                    bool inEdge = IsPaneOnRight
                        ? _swipeStartPoint.X >= Bounds.Width - openGestureEdge
                        : _swipeStartPoint.X <= openGestureEdge;

                    if (towardPane && inEdge)
                    {
                        SetCurrentValue(IsOpenProperty, true);
                        e.Handled = true;
                    }
                }
                else if (!towardPane)
                {
                    SetCurrentValue(IsOpenProperty, false);
                    e.Handled = true;
                }
            }
        }

        protected override void UpdateContentSafeAreaPadding()
        {
            if (_contentPresenter != null && _drawerPresenter != null)
            {
                if (IsVerticalPlacement)
                {
                    _drawerPresenter.Padding = DrawerPlacement == DrawerPlacement.Bottom
                        ? new Thickness(SafeAreaPadding.Left, SafeAreaPadding.Top, SafeAreaPadding.Right, 0)
                        : new Thickness(SafeAreaPadding.Left, 0, SafeAreaPadding.Right, SafeAreaPadding.Bottom);
                }
                else
                {
                    _drawerPresenter.Padding = IsPaneOnRight
                        ? new Thickness(0, SafeAreaPadding.Top, SafeAreaPadding.Right, SafeAreaPadding.Bottom)
                        : new Thickness(SafeAreaPadding.Left, SafeAreaPadding.Top, 0, SafeAreaPadding.Bottom);
                }

                if (_topBar != null)
                {
                    _topBar.Margin = new Thickness(SafeAreaPadding.Left, SafeAreaPadding.Top, SafeAreaPadding.Right, 0);
                }

                _contentPresenter.Padding = Padding;

                if (Content is Page detail)
                {
                    var remainingSafeArea = Padding.GetRemainingSafeAreaPadding(SafeAreaPadding);
                    detail.SafeAreaPadding = new Thickness(remainingSafeArea.Left, 0, remainingSafeArea.Right, remainingSafeArea.Bottom);
                }
            }
        }

        protected override void UpdateActivePage()
        {
            var previousPage = CurrentPage;

            SetCurrentValue(CurrentPageProperty, ResolveCurrentPage());

            if (!ReferenceEquals(previousPage, CurrentPage) && VisualRoot != null)
            {
                _hasHadFirstPage = true;
                previousPage?.SendNavigatedFrom(new NavigatedFromEventArgs(CurrentPage, NavigationType.Replace));
                CurrentPage?.SendNavigatedTo(new NavigatedToEventArgs(previousPage, NavigationType.Replace));
            }

            UpdateContentSafeAreaPadding();
        }

        private void SyncCurrentPage() => SetCurrentValue(CurrentPageProperty, ResolveCurrentPage());

        private Page? ResolveCurrentPage()
        {
            bool drawerIsOverlay = IsOpen &&
                (DisplayMode == SplitViewDisplayMode.Overlay || DisplayMode == SplitViewDisplayMode.CompactOverlay);
            return (drawerIsOverlay ? Drawer as Page : null) ?? Content as Page;
        }

        private void UpdatePanePlacement()
        {
            if (_splitView == null)
                return;

            _splitView.PanePlacement = DrawerPlacement switch
            {
                DrawerPlacement.Right  => SplitViewPanePlacement.Right,
                DrawerPlacement.Top    => SplitViewPanePlacement.Top,
                DrawerPlacement.Bottom => SplitViewPanePlacement.Bottom,
                _                      => SplitViewPanePlacement.Left
            };

            PseudoClasses.Set(":placement-right",  DrawerPlacement == DrawerPlacement.Right);
            PseudoClasses.Set(":placement-top",    DrawerPlacement == DrawerPlacement.Top);
            PseudoClasses.Set(":placement-bottom", DrawerPlacement == DrawerPlacement.Bottom);
        }

        private void UpdateSplitViewDisplayMode()
        {
            var previousMode = DisplayMode;

            var mode = ResolveSplitViewDisplayMode();
            SetCurrentValue(DisplayModeProperty, mode);

            if (DrawerBehavior == DrawerBehavior.Disabled)
            {
                SetCurrentValue(IsOpenProperty, false);
                return;
            }

            if (DrawerBehavior == DrawerBehavior.Locked)
            {
                SetCurrentValue(IsOpenProperty, true);
                return;
            }

            if (_splitView == null)
                return;

            if (DrawerBreakpointLength > 0 && previousMode != mode)
            {
                if (mode == SplitViewDisplayMode.Inline)
                {
                    _suppressDrawerEvents = true;
                    try { SetCurrentValue(IsOpenProperty, true); }
                    finally { _suppressDrawerEvents = false; }
                }
                else if (mode == SplitViewDisplayMode.Overlay)
                {
                    _suppressDrawerEvents = true;
                    try { SetCurrentValue(IsOpenProperty, false); }
                    finally { _suppressDrawerEvents = false; }
                }
            }
        }

        private SplitViewDisplayMode ResolveSplitViewDisplayMode()
        {
            switch (DrawerBehavior)
            {
                case DrawerBehavior.Locked:
                    return SplitViewDisplayMode.Inline;
                case DrawerBehavior.Disabled:
                case DrawerBehavior.Flyout:
                    return SplitViewDisplayMode.Overlay;
            }

            var breakpoint = DrawerBreakpointLength;
            if (breakpoint > 0)
            {
                var length = IsVerticalPlacement ? Bounds.Height : Bounds.Width;
                if (length > 0 && length < breakpoint)
                    return SplitViewDisplayMode.Overlay;
            }

            switch (DrawerLayoutBehavior)
            {
                case DrawerLayoutBehavior.Split:
                    return SplitViewDisplayMode.Inline;
                case DrawerLayoutBehavior.CompactOverlay:
                    return SplitViewDisplayMode.CompactOverlay;
                case DrawerLayoutBehavior.CompactInline:
                    return SplitViewDisplayMode.CompactInline;
                default:
                    return SplitViewDisplayMode.Overlay;
            }
        }

        private void UpdateDrawerFocus()
        {
            if (!IsOpen)
            {
                _paneButton?.Focus();
            }
            else if (DisplayMode == SplitViewDisplayMode.Overlay || DisplayMode == SplitViewDisplayMode.CompactOverlay)
            {
                if (_drawerPresenter != null)
                {
                    var firstFocusable = KeyboardNavigationHandler.GetNext(_drawerPresenter, NavigationDirection.Next);
                    firstFocusable?.Focus();
                }
            }
        }

        private void UpdateBackdropState()
        {
            if (_backdrop == null)
                return;
            var show = IsOpen
                && BackdropBrush != null
                && (DisplayMode == SplitViewDisplayMode.Overlay || DisplayMode == SplitViewDisplayMode.CompactOverlay);
            _backdrop.IsVisible = show;
            _backdrop.IsHitTestVisible = show;
        }

        private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
        {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
        }

        private void UpdateIconPresenters()
        {
            if (_compactPaneIconPresenter != null)
                _compactPaneIconPresenter.Content = CreateIconContent(DrawerIcon);
            if (_paneIconPresenter != null)
                _paneIconPresenter.Content = CreateIconContent(DrawerIcon);
            if (_bottomPaneIconPresenter != null)
                _bottomPaneIconPresenter.Content = CreateIconContent(DrawerIcon);
        }

        private static object? CreateIconContent(object? icon)
        {
            if (icon is Geometry geometry)
                return new PathIcon { Data = geometry };

            if (icon is not Control)
                return icon;

            // For Control-typed icons, create an independent copy per presenter to avoid
            // the "already has a visual parent" exception when the same instance is used
            // in multiple ContentPresenters simultaneously.
            if (icon is PathIcon pathIcon)
            {
                var clone = new PathIcon { Data = pathIcon.Data };

                CopyIfSet(pathIcon, clone, PathIcon.WidthProperty);
                CopyIfSet(pathIcon, clone, PathIcon.HeightProperty);
                CopyIfSet(pathIcon, clone, PathIcon.MarginProperty);
                CopyIfSet(pathIcon, clone, PathIcon.HorizontalAlignmentProperty);
                CopyIfSet(pathIcon, clone, PathIcon.VerticalAlignmentProperty);
                CopyIfSet(pathIcon, clone, PathIcon.ForegroundProperty);
                CopyIfSet(pathIcon, clone, PathIcon.OpacityProperty);
                CopyIfSet(pathIcon, clone, PathIcon.RenderTransformProperty);
                CopyIfSet(pathIcon, clone, PathIcon.RenderTransformOriginProperty);

                clone.Classes.Replace(pathIcon.Classes);

                return clone;

                static void CopyIfSet<T>(AvaloniaObject src, AvaloniaObject dst, AvaloniaProperty<T> property)
                {
                    if (src.IsSet(property))
                        dst.SetValue(property, src.GetValue(property));
                }
            }

            // For other Control subtypes, return null to avoid a crash.
            // Users should pass non-Control icon data instead.
            return null;
        }

        private void ApplyDrawerBackground()
        {
            if (_splitView == null)
                return;

            if (DrawerBackground != null)
                _splitView.PaneBackground = DrawerBackground;
            else
                _splitView.ClearValue(SplitView.PaneBackgroundProperty);
        }

        private static void ApplyForeground(ContentPresenter? presenter, IBrush? brush)
        {
            if (presenter == null)
                return;

            if (brush != null)
                TextElement.SetForeground(presenter, brush);
            else
                presenter.ClearValue(TextElement.ForegroundProperty);
        }

        private void UpdateDetailNavBarVisiblePseudoClass()
        {
            bool hideTopBar = Content is NavigationPage navPage && navPage.IsNavBarEffectivelyVisible;
            PseudoClasses.Set(":detail-is-navpage", hideTopBar);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new DrawerPageAutomationPeer(this);
    }
}
