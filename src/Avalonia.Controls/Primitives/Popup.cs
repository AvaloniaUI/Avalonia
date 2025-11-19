using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Reactive;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Displays a popup window.
    /// </summary>
    public class Popup : Control, IPopupHostProvider
    {
        /// <summary>
        /// Defines the <see cref="WindowManagerAddShadowHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> WindowManagerAddShadowHintProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(WindowManagerAddShadowHint), false);

        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> ChildProperty =
            AvaloniaProperty.Register<Popup, Control?>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="InheritsTransform"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> InheritsTransformProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(InheritsTransform));

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsOpenProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(IsOpen));

        /// <summary>
        /// Defines the <see cref="PlacementAnchor"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupAnchor> PlacementAnchorProperty =
            AvaloniaProperty.Register<Popup, PopupAnchor>(nameof(PlacementAnchor));

        /// <summary>
        /// Defines the <see cref="PlacementConstraintAdjustment"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupPositionerConstraintAdjustment> PlacementConstraintAdjustmentProperty =
            AvaloniaProperty.Register<Popup, PopupPositionerConstraintAdjustment>(
                nameof(PlacementConstraintAdjustment),
                PopupPositionerConstraintAdjustment.FlipX | PopupPositionerConstraintAdjustment.FlipY |
                PopupPositionerConstraintAdjustment.SlideX | PopupPositionerConstraintAdjustment.SlideY |
                PopupPositionerConstraintAdjustment.ResizeX | PopupPositionerConstraintAdjustment.ResizeY);

        /// <summary>
        /// Defines the <see cref="PlacementGravity"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupGravity> PlacementGravityProperty =
            AvaloniaProperty.Register<Popup, PopupGravity>(nameof(PlacementGravity));

        /// <summary>
        /// Defines the <see cref="Placement"/> property.
        /// </summary>
        public static readonly StyledProperty<PlacementMode> PlacementProperty =
            AvaloniaProperty.Register<Popup, PlacementMode>(nameof(Placement), defaultValue: PlacementMode.Bottom);

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        [Obsolete("Use the Placement property instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly StyledProperty<PlacementMode> PlacementModeProperty = PlacementProperty;

        /// <summary>
        /// Defines the <see cref="PlacementRect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect?> PlacementRectProperty =
            AvaloniaProperty.Register<Popup, Rect?>(nameof(PlacementRect));

        /// <summary>
        /// Defines the <see cref="PlacementTarget"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> PlacementTargetProperty =
            AvaloniaProperty.Register<Popup, Control?>(nameof(PlacementTarget));

        /// <summary>
        /// Defines the <see cref="CustomPopupPlacementCallback"/> property.
        /// </summary>
        public static readonly StyledProperty<CustomPopupPlacementCallback?> CustomPopupPlacementCallbackProperty =
            AvaloniaProperty.Register<Popup, CustomPopupPlacementCallback?>(nameof(CustomPopupPlacementCallback));
        
        /// <summary>
        /// Defines the <see cref="OverlayDismissEventPassThrough"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> OverlayDismissEventPassThroughProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(OverlayDismissEventPassThrough));

        /// <summary>
        /// Defines the <see cref="OverlayInputPassThroughElement"/> property.
        /// </summary>
        public static readonly StyledProperty<IInputElement?> OverlayInputPassThroughElementProperty =
            AvaloniaProperty.Register<Popup, IInputElement?>(nameof(OverlayInputPassThroughElement));

        /// <summary>
        /// Defines the <see cref="HorizontalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(HorizontalOffset));

        /// <summary>
        /// Defines the <see cref="IsLightDismissEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(IsLightDismissEnabled));

        /// <summary>
        /// Defines the <see cref="VerticalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(VerticalOffset));

        /// <summary>
        /// Defines the <see cref="Topmost"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(Topmost));

        /// <summary>
        /// Defines the <see cref="TakesFocusFromNativeControl"/> property.
        /// </summary>
        public static readonly AttachedProperty<bool> TakesFocusFromNativeControlProperty =
            AvaloniaProperty.RegisterAttached<Popup, Control, bool>(nameof(TakesFocusFromNativeControl), true);

        /// <summary>
        /// Defines the <see cref="ShouldUseOverlayLayer"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShouldUseOverlayLayerProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(ShouldUseOverlayLayer));

        /// <summary>
        /// Defines the <see cref="IsUsingOverlayLayer"/> property.
        /// </summary>
        public static readonly DirectProperty<Popup, bool> IsUsingOverlayLayerProperty = AvaloniaProperty.RegisterDirect<Popup, bool>(
            nameof(IsUsingOverlayLayer), o => o.IsUsingOverlayLayer);

        private bool _isOpenRequested;
        private bool _ignoreIsOpenChanged;
        private bool _isUsingOverlayLayer;
        private PopupOpenState? _openState;
        private Action<IPopupHost?>? _popupHostChangedHandler;

        /// <summary>
        /// Initializes static members of the <see cref="Popup"/> class.
        /// </summary>
        static Popup()
        {
            IsHitTestVisibleProperty.OverrideDefaultValue<Popup>(false);
            ChildProperty.Changed.AddClassHandler<Popup>((x, e) => x.ChildChanged(e));
            IsOpenProperty.Changed.AddClassHandler<Popup>((x, e) => x.IsOpenChanged((AvaloniaPropertyChangedEventArgs<bool>)e));
        }

        /// <summary>
        /// Raised when the popup closes.
        /// </summary>
        public event EventHandler<EventArgs>? Closed;

        /// <summary>
        /// Raised when the popup opens.
        /// </summary>
        public event EventHandler? Opened;

        internal event EventHandler<CancelEventArgs>? Closing;

        public IPopupHost? Host => _openState?.PopupHost;

        /// <summary>
        /// Gets or sets a hint to the window manager that a shadow should be added to the popup.
        /// </summary>
        public bool WindowManagerAddShadowHint
        {
            get => GetValue(WindowManagerAddShadowHintProperty);
            set => SetValue(WindowManagerAddShadowHintProperty, value);
        }

        /// <summary>
        /// Gets or sets the control to display in the popup.
        /// </summary>
        [Content]
        public Control? Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        /// <summary>
        /// Gets or sets a dependency resolver for the <see cref="PopupRoot"/>.
        /// </summary>
        /// <remarks>
        /// This property allows a client to customize the behaviour of the popup by injecting
        /// a specialized dependency resolver into the <see cref="PopupRoot"/>'s constructor.
        /// </remarks>
        public IAvaloniaDependencyResolver? DependencyResolver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that determines whether the popup inherits the render transform
        /// from its <see cref="PlacementTarget"/>. Defaults to false.
        /// </summary>
        public bool InheritsTransform
        {
            get => GetValue(InheritsTransformProperty);
            set => SetValue(InheritsTransformProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that determines how the <see cref="Popup"/> can be dismissed.
        /// </summary>
        /// <remarks>
        /// Light dismiss is when the user taps on any area other than the popup.
        /// </remarks>
        public bool IsLightDismissEnabled
        {
            get => GetValue(IsLightDismissEnabledProperty);
            set => SetValue(IsLightDismissEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen
        {
            get => GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the anchor point on the <see cref="PlacementRect"/> when <see cref="Placement"/>
        /// is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupAnchor PlacementAnchor
        {
            get => GetValue(PlacementAnchorProperty);
            set => SetValue(PlacementAnchorProperty, value);
        }

        /// <summary>
        /// Gets or sets a value describing how the popup position will be adjusted if the
        /// unadjusted position would result in the popup being partly constrained.
        /// </summary>
        public PopupPositionerConstraintAdjustment PlacementConstraintAdjustment
        {
            get => GetValue(PlacementConstraintAdjustmentProperty);
            set => SetValue(PlacementConstraintAdjustmentProperty, value);
        }

        /// <summary>
        /// Gets or sets a value which defines in what direction the popup should open
        /// when <see cref="Placement"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupGravity PlacementGravity
        {
            get => GetValue(PlacementGravityProperty);
            set => SetValue(PlacementGravityProperty, value);
        }

        /// <inheritdoc cref="Placement"/>
        [Obsolete("Use the Placement property instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public PlacementMode PlacementMode
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the desired placement of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode Placement
        {
            get => GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the anchor rectangle within the parent that the popup will be placed
        /// relative to when <see cref="Placement"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        /// <remarks>
        /// The placement rect defines a rectangle relative to <see cref="PlacementTarget"/> around
        /// which the popup will be opened, with <see cref="PlacementAnchor"/> determining which edge
        /// of the placement target is used.
        /// 
        /// If unset, the anchor rectangle will be the bounds of the <see cref="PlacementTarget"/>.
        /// </remarks>
        public Rect? PlacementRect
        {
            get => GetValue(PlacementRectProperty);
            set => SetValue(PlacementRectProperty, value);
        }

        /// <summary>
        /// Gets or sets the control that is used to determine the popup's position.
        /// </summary>
        [ResolveByName]
        public Control? PlacementTarget
        {
            get => GetValue(PlacementTargetProperty);
            set => SetValue(PlacementTargetProperty, value);
        }

        /// <summary>
        /// Gets or sets a delegate handler method that positions the Popup control, when <see cref="Placement"/> is set to <see cref="PlacementMode.Custom"/>.
        /// </summary>
        public CustomPopupPlacementCallback? CustomPopupPlacementCallback
        {
            get => GetValue(CustomPopupPlacementCallbackProperty);
            set => SetValue(CustomPopupPlacementCallbackProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event that closes the popup is passed
        /// through to the parent window.
        /// </summary>
        /// <remarks>
        /// When <see cref="IsLightDismissEnabled"/> is set to true, clicks outside the popup
        /// cause the popup to close. When <see cref="OverlayDismissEventPassThrough"/> is set to
        /// false, these clicks will be handled by the popup and not be registered by the parent
        /// window. When set to true, the events will be passed through to the parent window.
        /// </remarks>
        public bool OverlayDismissEventPassThrough
        {
            get => GetValue(OverlayDismissEventPassThroughProperty);
            set => SetValue(OverlayDismissEventPassThroughProperty, value);
        }

        /// <summary>
        /// Gets or sets an element that should receive pointer input events even when underneath
        /// the popup's overlay.
        /// </summary>
        public IInputElement? OverlayInputPassThroughElement
        {
            get => GetValue(OverlayInputPassThroughElementProperty);
            set => SetValue(OverlayInputPassThroughElementProperty, value);
        }

        /// <summary>
        /// Gets or sets the Horizontal offset of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double HorizontalOffset
        {
            get => GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>
        /// Gets or sets the Vertical offset of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double VerticalOffset
        {
            get => GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this popup appears on top of all other windows
        /// </summary>
        public bool Topmost
        {
            get => GetValue(TopmostProperty);
            set => SetValue(TopmostProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup, on show, transfers focus from any
        /// focused native control to Avalonia. The default is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// This property only applies to advanced native control embedding scenarios. By default,
        /// if a popup is shown when a native control is focused, focus is transferred back to
        /// Avalonia in order for the popup to receive input. If this property is set to
        /// <c>false</c>, then the shown popup will not receive input until it receives an
        /// interaction which explicitly focuses the popup, such as a mouse click.
        /// </remarks>
        public bool TakesFocusFromNativeControl
        {
            get => GetValue(TakesFocusFromNativeControlProperty);
            set => SetValue(TakesFocusFromNativeControlProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the popup should be shown in the overlay layer of the parent window.
        /// </summary>
        /// <remarks>
        /// When <see cref="ShouldUseOverlayLayer"/> is "false" implementation depends on the platform.
        /// Use <see cref="IsUsingOverlayLayer"/> to get actual popup behavior.
        /// This is an equvalent of `OverlayPopups` property of the platform options, but settable independently per each popup. 
        /// </remarks>
        public bool ShouldUseOverlayLayer
        {
            get => GetValue(ShouldUseOverlayLayerProperty);
            set => SetValue(ShouldUseOverlayLayerProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the popup is shown in the overlay layer of the parent window.
        /// </summary>
        public bool IsUsingOverlayLayer
        {
            get => _isUsingOverlayLayer;
            private set => SetAndRaise(IsUsingOverlayLayerProperty, ref _isUsingOverlayLayer, value);
        }

        IPopupHost? IPopupHostProvider.PopupHost => Host;

        event Action<IPopupHost?>? IPopupHostProvider.PopupHostChanged 
        { 
            add => _popupHostChangedHandler += value; 
            remove => _popupHostChangedHandler -= value;
        }

        /// <summary>
        /// Opens the popup.
        /// </summary>
        public void Open()
        {
            // Popup is currently open
            if (_openState != null)
            {
                return;
            }

            var placementTarget = PlacementTarget ?? this.FindLogicalAncestorOfType<Control>();

            if (placementTarget == null)
            {
                _isOpenRequested = true;
                return;
            }
            
            var topLevel = TopLevel.GetTopLevel(placementTarget);

            if (topLevel == null)
            {
                _isOpenRequested = true;
                return;
            }

            _isOpenRequested = false;

            var popupHost = OverlayPopupHost.CreatePopupHost(placementTarget, DependencyResolver, ShouldUseOverlayLayer);
            var handlerCleanup = new CompositeDisposable(7);

            UpdateHostSizing(popupHost, topLevel, placementTarget);
            popupHost.Topmost = Topmost;
            popupHost.SetChild(Child);
            ((ISetLogicalParent)popupHost).SetParent(this);

            if (InheritsTransform)
            {
                TransformTrackingHelper.Track(placementTarget, PlacementTargetTransformChanged)
                    .DisposeWith(handlerCleanup);
            }
            else
            {
                popupHost.Transform = null;
            }

            if (popupHost is PopupRoot topLevelPopup)
            {
                topLevelPopup
                    .Bind(
                        ThemeVariantScope.ActualThemeVariantProperty,
                        this.GetBindingObservable(ThemeVariantScope.ActualThemeVariantProperty))
                    .DisposeWith(handlerCleanup);
            }

            UpdateHostPosition(popupHost, placementTarget);

            SubscribeToEventHandler<IPopupHost, EventHandler<TemplateAppliedEventArgs>>(popupHost, RootTemplateApplied,
                (x, handler) => x.TemplateApplied += handler,
                (x, handler) => x.TemplateApplied -= handler).DisposeWith(handlerCleanup);

            SubscribeToEventHandler<Control, EventHandler<VisualTreeAttachmentEventArgs>>(placementTarget, TargetDetached,
                (x, handler) => x.DetachedFromVisualTree += handler,
                (x, handler) => x.DetachedFromVisualTree -= handler).DisposeWith(handlerCleanup);

            if (topLevel is Window window && window.PlatformImpl != null)
            {
                SubscribeToEventHandler<Window, EventHandler>(window, WindowDeactivated,
                    (x, handler) => x.Deactivated += handler,
                    (x, handler) => x.Deactivated -= handler).DisposeWith(handlerCleanup);

                SubscribeToEventHandler<IWindowImpl, Action>(window.PlatformImpl, WindowLostFocus,
                    (x, handler) => x.LostFocus += handler,
                    (x, handler) => x.LostFocus -= handler).DisposeWith(handlerCleanup);

                // Recalculate popup position on parent moved/resized, but not if placement was on pointer
                if (Placement != PlacementMode.Pointer)
                {
                    SubscribeToEventHandler<IWindowImpl, Action<PixelPoint>>(window.PlatformImpl, WindowPositionChanged,
                        (x, handler) => x.PositionChanged += handler,
                        (x, handler) => x.PositionChanged -= handler).DisposeWith(handlerCleanup);

                    if (placementTarget is Layoutable layoutTarget)
                    {
                        // If the placement target is moved, update the popup position
                        SubscribeToEventHandler<Layoutable, EventHandler>(layoutTarget, PlacementTargetLayoutUpdated,
                            (x, handler) => x.LayoutUpdated += handler,
                            (x, handler) => x.LayoutUpdated -= handler).DisposeWith(handlerCleanup);
                    }
                }
            }
            else if (topLevel is PopupRoot parentPopupRoot)
            {
                SubscribeToEventHandler<PopupRoot, EventHandler<PixelPointEventArgs>>(parentPopupRoot, ParentPopupPositionChanged,
                    (x, handler) => x.PositionChanged += handler,
                    (x, handler) => x.PositionChanged -= handler).DisposeWith(handlerCleanup);

                if (parentPopupRoot.Parent is Popup popup)
                {
                    SubscribeToEventHandler<Popup, EventHandler<EventArgs>>(popup, ParentClosed,
                        (x, handler) => x.Closed += handler,
                        (x, handler) => x.Closed -= handler).DisposeWith(handlerCleanup);
                }
            }
            else if (topLevel is { } wtl && wtl.PlatformImpl is IWindowBaseImpl wimpl)
            {
                SubscribeToEventHandler<ITopLevelImpl, Action>(wimpl, TopLevelLostPlatformFocus,
                    (x, handler) => x.LostFocus += handler,
                    (x, handler) => x.LostFocus -= handler).DisposeWith(handlerCleanup);

                SubscribeToEventHandler<IWindowBaseImpl, Action>(wimpl, WindowBaseDeactivated,
                    (x, handler) => x.Deactivated += handler,
                    (x, handler) => x.Deactivated -= handler).DisposeWith(handlerCleanup);
            }
            else if (topLevel is { } tl && tl.PlatformImpl is ITopLevelImpl pimpl)
            {
                SubscribeToEventHandler<ITopLevelImpl, Action>(pimpl, TopLevelLostPlatformFocus,
                    (x, handler) => x.LostFocus += handler,
                    (x, handler) => x.LostFocus -= handler).DisposeWith(handlerCleanup);
            }

            InputManager.Instance?.Process.Subscribe(ListenForNonClientClick).DisposeWith(handlerCleanup);

            var cleanupPopup = Disposable.Create((popupHost, handlerCleanup), state =>
            {
                state.handlerCleanup.Dispose();

                state.popupHost.SetChild(null);
                state.popupHost.Hide();

                ((ISetLogicalParent)state.popupHost).SetParent(null);
                state.popupHost.Dispose();
            });

            if (IsLightDismissEnabled)
            {
                var dismissLayer = LightDismissOverlayLayer.GetLightDismissOverlayLayer(placementTarget);

                if (dismissLayer != null)
                {
                    dismissLayer.IsVisible = true;
                    dismissLayer.InputPassThroughElement = OverlayInputPassThroughElement;
                    
                    Disposable.Create(() =>
                    {
                        dismissLayer.IsVisible = false;
                        dismissLayer.InputPassThroughElement = null;
                    }).DisposeWith(handlerCleanup);
                    
                    SubscribeToEventHandler<LightDismissOverlayLayer, EventHandler<PointerPressedEventArgs>>(
                        dismissLayer,
                        PointerPressedDismissOverlay,
                        (x, handler) => x.PointerPressed += handler,
                        (x, handler) => x.PointerPressed -= handler).DisposeWith(handlerCleanup);
                }
            }

            _openState = new PopupOpenState(placementTarget, topLevel, popupHost, cleanupPopup);

            WindowManagerAddShadowHintChanged(popupHost, WindowManagerAddShadowHint);

            popupHost.Show();
            IsUsingOverlayLayer = popupHost is OverlayPopupHost;

            if (TakesFocusFromNativeControl)
                popupHost.TakeFocus();

            using (BeginIgnoringIsOpen())
            {
                SetCurrentValue(IsOpenProperty, true);
            }

            Opened?.Invoke(this, EventArgs.Empty);

            _popupHostChangedHandler?.Invoke(Host);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public void Close() => CloseCore();

        /// <summary>
        /// Gets the value of the <see cref="TakesFocusFromNativeControl"/> attached property on the
        /// specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        public static bool GetTakesFocusFromNativeControl(Control control)
        {
            return control.GetValue(TakesFocusFromNativeControlProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="TakesFocusFromNativeControl"/> attached property on the
        /// specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the TakesFocusFromNativeControl property.</param>
        public static void SetTakesFocusFromNativeControl(Control control, bool value)
        {
            control.SetValue(TakesFocusFromNativeControlProperty, value);
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>A size of 0,0 as Popup itself takes up no space.</returns>
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size();
        }


        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_isOpenRequested)
            {
                Open();
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            Close();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (_openState is not null)
            {
                if (change.Property == WidthProperty ||
                    change.Property == MinWidthProperty ||
                    change.Property == MaxWidthProperty ||
                    change.Property == HeightProperty ||
                    change.Property == MinHeightProperty ||
                    change.Property == MaxHeightProperty)
                {
                    UpdateHostSizing(_openState.PopupHost, _openState.TopLevel, _openState.PlacementTarget);
                }
                else if (change.Property == PlacementTargetProperty ||
                         change.Property == PlacementProperty ||
                         change.Property == HorizontalOffsetProperty ||
                         change.Property == VerticalOffsetProperty ||
                         change.Property == PlacementAnchorProperty ||
                         change.Property == PlacementConstraintAdjustmentProperty ||
                         change.Property == PlacementRectProperty)
                {
                    if (change.Property == PlacementTargetProperty)
                    {
                        var newTarget = change.GetNewValue<Control?>() ?? this.FindLogicalAncestorOfType<Control>();

                        if (newTarget is null || newTarget.GetVisualRoot() != _openState.TopLevel)
                        {
                            Close();
                            return;
                        }

                        _openState.PlacementTarget = newTarget;
                    }

                    UpdateHostPosition(_openState.PopupHost, _openState.PlacementTarget);
                }
                else if (change.Property == TopmostProperty)
                {
                    _openState.PopupHost.Topmost = change.GetNewValue<bool>();
                }
            }
        }

        /// <summary>
        /// Helper method to set popup's styling and templated parent.
        /// </summary>
        internal void SetPopupParent(Control? newParent)
        {
            if (Parent != null && Parent != newParent)
            {
                ((ISetLogicalParent)this).SetParent(null);
            }

            if (Parent == null || PlacementTarget != newParent)
            {
                ((ISetLogicalParent)this).SetParent(newParent);
                TemplatedParent = newParent?.TemplatedParent;
            }
        }

        private void UpdateHostPosition(IPopupHost popupHost, Control placementTarget)
        {
            popupHost.ConfigurePosition(new PopupPositionRequest(
                placementTarget,
                Placement,
                new Point(HorizontalOffset, VerticalOffset),
                PlacementAnchor,
                PlacementGravity,
                PlacementConstraintAdjustment,
                PlacementRect ?? new Rect(default, placementTarget.Bounds.Size),
                CustomPopupPlacementCallback));
        }

        private void UpdateHostSizing(IPopupHost popupHost, TopLevel topLevel, Control placementTarget)
        {
            var scaleX = 1.0;
            var scaleY = 1.0;

            if (InheritsTransform && placementTarget.TransformToVisual(topLevel) is { } m)
            {
                scaleX = Math.Sqrt(m.M11 * m.M11 + m.M12 * m.M12);
                scaleY = Math.Sqrt(m.M11 * m.M11 + m.M12 * m.M12);

                // Ideally we'd only assign a ScaleTransform here when the scale != 1, but there's
                // an issue with LayoutTransformControl in that it sets its LayoutTransform property
                // with LocalValue priority in ArrangeOverride in certain cases when LayoutTransform
                // is null, which breaks TemplateBindings to this property. Offending commit/line:
                //
                // https://github.com/AvaloniaUI/Avalonia/commit/6fbe1c2180ef45a940e193f1b4637e64eaab80ed#diff-5344e793df13f462126a8153ef46c44194f244b6890f25501709bae51df97f82R54
                popupHost.Transform = new ScaleTransform(scaleX, scaleY);
            }
            else
            {
                popupHost.Transform = null;
            }

            popupHost.Width = Width * scaleX;
            popupHost.MinWidth = MinWidth * scaleX;
            popupHost.MaxWidth = MaxWidth * scaleX;
            popupHost.Height = Height * scaleY;
            popupHost.MinHeight = MinHeight * scaleY;
            popupHost.MaxHeight = MaxHeight * scaleY;
        }

        private void HandlePositionChange()
        {
            if (_openState != null)
            {
                UpdateHostPosition(_openState.PopupHost, _openState.PlacementTarget);
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PopupAutomationPeer(this);
        }

        private static IDisposable SubscribeToEventHandler<T, TEventHandler>(T target, TEventHandler handler, Action<T, TEventHandler> subscribe, Action<T, TEventHandler> unsubscribe)
        {
            subscribe(target, handler);

            return Disposable.Create((unsubscribe, target, handler), state => state.unsubscribe(state.target, state.handler));
        }

        private static void WindowManagerAddShadowHintChanged(IPopupHost host, bool hint)
        {
            if (host is PopupRoot pr)
            {
                pr.WindowManagerAddShadowHint = hint;
            }
        }

        /// <summary>
        /// Called when the <see cref="IsOpen"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void IsOpenChanged(AvaloniaPropertyChangedEventArgs<bool> e)
        {
            if (!_ignoreIsOpenChanged)
            {
                if (e.NewValue.Value)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            LogicalChildren.Clear();

            ((ISetLogicalParent?)e.OldValue)?.SetParent(null);

            if (e.NewValue != null)
            {
                ((ISetLogicalParent)e.NewValue).SetParent(this);
                LogicalChildren.Add((ILogical)e.NewValue);
            }
        }

        private void CloseCore()
        {
            var closingArgs = new CancelEventArgs();
            Closing?.Invoke(this, closingArgs);
            if (closingArgs.Cancel)
            {
                return;
            }

            _isOpenRequested = false;
            if (_openState is null)
            {
                using (BeginIgnoringIsOpen())
                {
                    SetCurrentValue(IsOpenProperty, false);
                }

                return;
            }

            _openState.Dispose();
            _openState = null;

            _popupHostChangedHandler?.Invoke(null);

            using (BeginIgnoringIsOpen())
            {
                SetCurrentValue(IsOpenProperty, false);
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void ListenForNonClientClick(RawInputEventArgs e)
        {
            var mouse = e as RawPointerEventArgs;

            if (IsLightDismissEnabled && mouse?.Type == RawPointerEventType.NonClientLeftButtonDown)
            {
                CloseCore();
            }
        }

        private void PointerPressedDismissOverlay(object? sender, PointerPressedEventArgs e)
        {
            if (IsLightDismissEnabled && e.Source is Visual v && !IsChildOrThis(v))
            {
                if (OverlayDismissEventPassThrough)
                {
                    PassThroughEvent(e);
                }

                // Ensure the popup is closed if it was not closed by a pass-through event handler
                if (IsOpen)
                {
                    CloseCore();
                }
            }
        }

        private void TargetDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Close();
        }

        private static void PassThroughEvent(PointerPressedEventArgs e)
        {
            if (e.Source is LightDismissOverlayLayer layer &&
                layer.GetVisualRoot() is InputElement root)
            {
                var p = e.GetCurrentPoint(root);
                var hit = root.InputHitTest(p.Position, x => x != layer);

                if (hit != null)
                {
                    e.Pointer.Capture(hit);
                    hit.RaiseEvent(e);
                    e.Handled = true;
                }
            }
        }

        private void RootTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            if (_openState is null)
            {
                return;
            }

            var popupHost = _openState.PopupHost;

            popupHost.TemplateApplied -= RootTemplateApplied;

            _openState.SetPresenterSubscription(null);

            // If the Popup appears in a control template, then the child controls
            // that appear in the popup host need to have their TemplatedParent
            // properties set.
            if (TemplatedParent != null && popupHost.Presenter is Control presenter)
            {
                presenter.ApplyTemplate();

                var presenterSubscription = presenter.GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(SetTemplatedParentAndApplyChildTemplates);

                _openState.SetPresenterSubscription(presenterSubscription);
            }
        }

        private void SetTemplatedParentAndApplyChildTemplates(Control? control)
        {
            if (control != null)
            {
                TemplatedControl.ApplyTemplatedParent(control, TemplatedParent);
            }
        }

        private bool IsChildOrThis(Visual child)
        {
            if (_openState is null)
            {
                return false;
            }

            var popupHost = _openState.PopupHost;

            Visual? root = child.VisualRoot as Visual;
            
            while (root is IHostedVisualTreeRoot hostedRoot)
            {
                if (root == popupHost)
                {
                    return true;
                }

                root = hostedRoot.Host?.VisualRoot as Visual;
            }

            return false;
        }
        
        public bool IsInsidePopup(Visual visual)
        {
            if (_openState is null)
            {
                return false;
            }

            var popupHost = _openState.PopupHost;

            return ((Visual)popupHost).IsVisualAncestorOf(visual);
        }

        public bool IsPointerOverPopup => ((IInputElement?)_openState?.PopupHost)?.IsPointerOver ?? false;

        private void WindowDeactivated(object? sender, EventArgs e)
        {
            if (IsLightDismissEnabled)
            {
                Close();
            }
        }

        private void WindowBaseDeactivated()
        {
            if (IsLightDismissEnabled)
            {
                Close();
            }
        }

        private void ParentClosed(object? sender, EventArgs e)
        {
            if (IsLightDismissEnabled)
            {
                Close();
            }
        }

        private void TopLevelLostPlatformFocus()
        {
            if (IsLightDismissEnabled)
            {
                Close();
            }
        }

        private void PlacementTargetTransformChanged(Visual v, Matrix? matrix)
        {
            if (_openState is not null)
                UpdateHostSizing(_openState.PopupHost, _openState.TopLevel, _openState.PlacementTarget);
        }

        private void WindowLostFocus()
        {
            if (IsLightDismissEnabled)
                Close();
        }

        private void WindowPositionChanged(PixelPoint pp) => HandlePositionChange();

        private void PlacementTargetLayoutUpdated(object? src, EventArgs e)
        {
            if (_openState is null)
                return;

            // A LayoutUpdated event is raised for the whole visual tree:
            // the bounds of the PlacementTarget might not have effectively changed.
            var newBounds = _openState.PlacementTarget.Bounds;
            if (newBounds == _openState.LastPlacementTargetBounds)
                return;

            _openState.LastPlacementTargetBounds = newBounds;
            UpdateHostPosition(_openState.PopupHost, _openState.PlacementTarget);
        }

        private void ParentPopupPositionChanged(object? src, PixelPointEventArgs e) => HandlePositionChange();

        private IgnoreIsOpenScope BeginIgnoringIsOpen()
        {
            return new IgnoreIsOpenScope(this);
        }

        private readonly struct IgnoreIsOpenScope : IDisposable
        {
            private readonly Popup _owner;

            public IgnoreIsOpenScope(Popup owner)
            {
                _owner = owner;
                _owner._ignoreIsOpenChanged = true;
            }

            public void Dispose()
            {
                _owner._ignoreIsOpenChanged = false;
            }
        }

        private class PopupOpenState : IDisposable
        {
            private readonly IDisposable _cleanup;
            private IDisposable? _presenterCleanup;
            private Control _placementTarget;

            public PopupOpenState(Control placementTarget, TopLevel topLevel, IPopupHost popupHost, IDisposable cleanup)
            {
                PlacementTarget = placementTarget;
                TopLevel = topLevel;
                PopupHost = popupHost;
                _cleanup = cleanup;
            }

            public TopLevel TopLevel { get; }

            public Control PlacementTarget
            {
                get => _placementTarget;
                [MemberNotNull(nameof(_placementTarget))]
                set
                {
                    _placementTarget = value;
                    LastPlacementTargetBounds = value.Bounds;
                }
            }

            public Rect LastPlacementTargetBounds { get; set; }

            public IPopupHost PopupHost { get; }

            public void SetPresenterSubscription(IDisposable? presenterCleanup)
            {
                _presenterCleanup?.Dispose();

                _presenterCleanup = presenterCleanup;
            }

            public void Dispose()
            {
                _presenterCleanup?.Dispose();

                _cleanup.Dispose();
            }
        }
    }
}
