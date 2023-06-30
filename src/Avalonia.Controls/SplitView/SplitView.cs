using System;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control with two views: A collapsible pane and an area for content
    /// </summary>
    [TemplatePart("PART_PaneRoot", typeof(Panel))]
    [PseudoClasses(pcOpen, pcClosed)]
    [PseudoClasses(pcCompactOverlay, pcCompactInline, pcOverlay, pcInline)]
    [PseudoClasses(pcLeft, pcRight)]
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
        private const string pcLightDismiss = ":lightDismiss";

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
        /// Gets or sets the TemplateSettings for the <see cref="SplitView"/>.
        /// </summary>
        public SplitViewTemplateSettings TemplateSettings
        {
            get => _templateSettings;
            private set => SetAndRaise(TemplateSettingsProperty, ref _templateSettings, value);
        }

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
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // :left and :right style triggers contain the template so we need to do this as
            // soon as we're attached so the template applies. The other visual states can
            // be updated after the template applies
            UpdateVisualStateForPanePlacementProperty(PanePlacement);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _pointerDisposable?.Dispose();
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

                if (isPaneOpen)
                {
                    PseudoClasses.Add(pcOpen);
                    PseudoClasses.Remove(pcClosed);

                    OnPaneOpened(new RoutedEventArgs(PaneOpenedEvent, this));
                }
                else
                {
                    PseudoClasses.Add(pcClosed);
                    PseudoClasses.Remove(pcOpen);

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
            EnableLightDismiss();
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
            }
            else if (displayMode == SplitViewDisplayMode.CompactOverlay)
            {
                TemplateSettings.ClosedPaneWidth = newLen;
                TemplateSettings.PaneColumnGridLength = new GridLength(newLen, GridUnitType.Pixel);
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
                SplitViewDisplayMode.Overlay => (0, new GridLength(0, GridUnitType.Pixel)),
                SplitViewDisplayMode.CompactOverlay => (CompactPaneLength, new GridLength(CompactPaneLength, GridUnitType.Pixel)),
                SplitViewDisplayMode.Inline => (0, new GridLength(0, GridUnitType.Auto)),
                SplitViewDisplayMode.CompactInline => (CompactPaneLength, new GridLength(0, GridUnitType.Auto)),
                _ => throw new NotImplementedException(),
            };
            TemplateSettings.ClosedPaneWidth = closedPaneWidth;
            TemplateSettings.PaneColumnGridLength = paneColumnGridLength;
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

        private void EnableLightDismiss()
        {
            if (_pane == null)
                return;

            // If this returns false, we're not in Overlay or CompactOverlay DisplayMode
            // and don't need the light dismiss behavior
            if (!IsInOverlayMode())
                return;

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

        private void TopLevelBackRequested(object? sender, RoutedEventArgs e)
        {
            if (!IsInOverlayMode())
                return;

            SetCurrentValue(IsPaneOpenProperty, false);
            e.Handled = true;
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
    }
}
