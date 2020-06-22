using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Displays a popup window.
    /// </summary>
    public class Popup : Control, IVisualTreeHost
    {
        public static readonly StyledProperty<bool> WindowManagerAddShadowHintProperty =
            AvaloniaProperty.Register<PopupRoot, bool>(nameof(WindowManagerAddShadowHint), true);

        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> ChildProperty =
            AvaloniaProperty.Register<Popup, Control?>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<Popup, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<Popup, bool>(
                nameof(IsOpen),
                o => o.IsOpen,
                (o, v) => o.IsOpen = v);

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
                PopupPositionerConstraintAdjustment.ResizeX | PopupPositionerConstraintAdjustment.ResizeY);

        /// <summary>
        /// Defines the <see cref="PlacementGravity"/> property.
        /// </summary>
        public static readonly StyledProperty<PopupGravity> PlacementGravityProperty =
            AvaloniaProperty.Register<Popup, PopupGravity>(nameof(PlacementGravity));

        /// <summary>
        /// Defines the <see cref="PlacementMode"/> property.
        /// </summary>
        public static readonly StyledProperty<PlacementMode> PlacementModeProperty =
            AvaloniaProperty.Register<Popup, PlacementMode>(nameof(PlacementMode), defaultValue: PlacementMode.Bottom);

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

#pragma warning disable 618
        /// <summary>
        /// Defines the <see cref="ObeyScreenEdges"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ObeyScreenEdgesProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(ObeyScreenEdges), true);
#pragma warning restore 618

        /// <summary>
        /// Defines the <see cref="HorizontalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(HorizontalOffset));

        /// <summary>
        /// Defines the <see cref="VerticalOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.Register<Popup, double>(nameof(VerticalOffset));

        /// <summary>
        /// Defines the <see cref="StaysOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> StaysOpenProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(StaysOpen), true);

        /// <summary>
        /// Defines the <see cref="Topmost"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<Popup, bool>(nameof(Topmost));

        private bool _isOpen;
        private bool _ignoreIsOpenChanged;
        private PopupOpenState? _openState;

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
        public event EventHandler<PopupClosedEventArgs>? Closed;

        /// <summary>
        /// Raised when the popup opens.
        /// </summary>
        public event EventHandler? Opened;

        public IPopupHost? Host => _openState?.PopupHost;

        public bool WindowManagerAddShadowHint
        {
            get { return GetValue(WindowManagerAddShadowHintProperty); }
            set { SetValue(WindowManagerAddShadowHintProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control to display in the popup.
        /// </summary>
        [Content]
        public Control? Child
        {
            get { return GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
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
        /// Gets or sets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
            set { SetAndRaise(IsOpenProperty, ref _isOpen, value); }
        }

        /// <summary>
        /// Gets or sets the anchor point on the <see cref="PlacementRect"/> when <see cref="PlacementMode"/>
        /// is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupAnchor PlacementAnchor
        {
            get { return GetValue(PlacementAnchorProperty); }
            set { SetValue(PlacementAnchorProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value describing how the popup position will be adjusted if the
        /// unadjusted position would result in the popup being partly constrained.
        /// </summary>
        public PopupPositionerConstraintAdjustment PlacementConstraintAdjustment
        {
            get { return GetValue(PlacementConstraintAdjustmentProperty); }
            set { SetValue(PlacementConstraintAdjustmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value which defines in what direction the popup should open
        /// when <see cref="PlacementMode"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
        /// </summary>
        public PopupGravity PlacementGravity
        {
            get { return GetValue(PlacementGravityProperty); }
            set { SetValue(PlacementGravityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the placement mode of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public PlacementMode PlacementMode
        {
            get { return GetValue(PlacementModeProperty); }
            set { SetValue(PlacementModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the the anchor rectangle within the parent that the popup will be placed
        /// relative to when <see cref="PlacementMode"/> is <see cref="PlacementMode.AnchorAndGravity"/>.
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
            get { return GetValue(PlacementRectProperty); }
            set { SetValue(PlacementRectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control that is used to determine the popup's position.
        /// </summary>
        public Control? PlacementTarget
        {
            get { return GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        [Obsolete("This property has no effect")]
        public bool ObeyScreenEdges
        {
            get => GetValue(ObeyScreenEdgesProperty);
            set => SetValue(ObeyScreenEdgesProperty, value);
        }

        /// <summary>
        /// Gets or sets the Horizontal offset of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double HorizontalOffset
        {
            get { return GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Vertical offset of the popup in relation to the <see cref="PlacementTarget"/>.
        /// </summary>
        public double VerticalOffset
        {
            get { return GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the popup should stay open when the popup is
        /// pressed or loses focus.
        /// </summary>
        public bool StaysOpen
        {
            get { return GetValue(StaysOpenProperty); }
            set { SetValue(StaysOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether this popup appears on top of all other windows
        /// </summary>
        public bool Topmost
        {
            get { return GetValue(TopmostProperty); }
            set { SetValue(TopmostProperty, value); }
        }

        /// <summary>
        /// Gets the root of the popup window.
        /// </summary>
        IVisual? IVisualTreeHost.Root => _openState?.PopupHost.HostedVisualTreeRoot;

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

            var placementTarget = PlacementTarget ?? this.GetLogicalAncestors().OfType<IVisual>().FirstOrDefault();

            if (placementTarget == null)
            {
                throw new InvalidOperationException("Popup has no logical parent and PlacementTarget is null");
            }
            
            var topLevel = placementTarget.VisualRoot as TopLevel;

            if (topLevel == null)
            {
                throw new InvalidOperationException(
                    "Attempted to open a popup not attached to a TopLevel");
            }

            var popupHost = OverlayPopupHost.CreatePopupHost(placementTarget, DependencyResolver);

            var handlerCleanup = new CompositeDisposable(5);

            void DeferCleanup(IDisposable? disposable)
            {
                if (disposable is null)
                {
                    return;
                }

                handlerCleanup.Add(disposable);
            }

            DeferCleanup(popupHost.BindConstraints(this, WidthProperty, MinWidthProperty, MaxWidthProperty,
                HeightProperty, MinHeightProperty, MaxHeightProperty, TopmostProperty));

            popupHost.SetChild(Child);
            ((ISetLogicalParent)popupHost).SetParent(this);

            popupHost.ConfigurePosition(
                placementTarget,
                PlacementMode,
                new Point(HorizontalOffset, VerticalOffset),
                PlacementAnchor,
                PlacementGravity,
                PlacementConstraintAdjustment,
                PlacementRect);

            DeferCleanup(SubscribeToEventHandler<IPopupHost, EventHandler<TemplateAppliedEventArgs>>(popupHost, RootTemplateApplied,
                (x, handler) => x.TemplateApplied += handler,
                (x, handler) => x.TemplateApplied -= handler));

            if (topLevel is Window window)
            {
                DeferCleanup(SubscribeToEventHandler<Window, EventHandler>(window, WindowDeactivated,
                    (x, handler) => x.Deactivated += handler,
                    (x, handler) => x.Deactivated -= handler));
            }
            else
            {
                var parentPopupRoot = topLevel as PopupRoot;

                if (parentPopupRoot?.Parent is Popup popup)
                {
                    DeferCleanup(SubscribeToEventHandler<Popup, EventHandler<PopupClosedEventArgs>>(popup, ParentClosed,
                        (x, handler) => x.Closed += handler,
                        (x, handler) => x.Closed -= handler));
                }
            }

            DeferCleanup(topLevel.AddDisposableHandler(PointerPressedEvent, PointerPressedOutside, RoutingStrategies.Tunnel));

            DeferCleanup(InputManager.Instance?.Process.Subscribe(ListenForNonClientClick));

            var cleanupPopup = Disposable.Create((popupHost, handlerCleanup), state =>
            {
                state.handlerCleanup.Dispose();

                state.popupHost.SetChild(null);
                state.popupHost.Hide();

                ((ISetLogicalParent)state.popupHost).SetParent(null);
                state.popupHost.Dispose();
            });

            _openState = new PopupOpenState(topLevel, popupHost, cleanupPopup);

            WindowManagerAddShadowHintChanged(popupHost, WindowManagerAddShadowHint);

            popupHost.Show();

            using (BeginIgnoringIsOpen())
            {
                IsOpen = true;
            }

            Opened?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public void Close() => CloseCore(null);

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
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            Close();
        }

        private static IDisposable SubscribeToEventHandler<T, TEventHandler>(T target, TEventHandler handler, Action<T, TEventHandler> subscribe, Action<T, TEventHandler> unsubscribe)
        {
            subscribe(target, handler);

            return Disposable.Create((unsubscribe, target, handler), state => state.unsubscribe(state.target, state.handler));
        }

        private void WindowManagerAddShadowHintChanged(IPopupHost host, bool hint)
        {
            if(host is PopupRoot pr)
            {
                pr.PlatformImpl.SetWindowManagerAddShadowHint(hint);
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

        private void CloseCore(EventArgs? closeEvent)
        {
            if (_openState is null)
            {
                using (BeginIgnoringIsOpen())
                {
                    IsOpen = false;
                }

                return;
            }

            _openState.Dispose();
            _openState = null;

            using (BeginIgnoringIsOpen())
            {
                IsOpen = false;
            }

            Closed?.Invoke(this, new PopupClosedEventArgs(closeEvent));
        }

        private void ListenForNonClientClick(RawInputEventArgs e)
        {
            var mouse = e as RawPointerEventArgs;

            if (!StaysOpen && mouse?.Type == RawPointerEventType.NonClientLeftButtonDown)
            {
                CloseCore(e);
            }
        }

        private void PointerPressedOutside(object sender, PointerPressedEventArgs e)
        {
            if (!StaysOpen && e.Source is IVisual v && !IsChildOrThis(v))
            {
                CloseCore(e);
            }
        }

        private void RootTemplateApplied(object sender, TemplateAppliedEventArgs e)
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
            if (TemplatedParent != null && popupHost.Presenter != null)
            {
                popupHost.Presenter.ApplyTemplate();

                var presenterSubscription = popupHost.Presenter.GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(SetTemplatedParentAndApplyChildTemplates);

                _openState.SetPresenterSubscription(presenterSubscription);
            }
        }

        private void SetTemplatedParentAndApplyChildTemplates(IControl control)
        {
            if (control != null)
            {
                var templatedParent = TemplatedParent;

                if (control.TemplatedParent == null)
                {
                    control.SetValue(TemplatedParentProperty, templatedParent);
                }

                control.ApplyTemplate();

                if (!(control is IPresenter) && control.TemplatedParent == templatedParent)
                {
                    foreach (IControl child in control.VisualChildren)
                    {
                        SetTemplatedParentAndApplyChildTemplates(child);
                    }
                }
            }
        }

        private bool IsChildOrThis(IVisual child)
        {
            if (_openState is null)
            {
                return false;
            }

            var popupHost = _openState.PopupHost;

            IVisual? root = child.VisualRoot;
            
            while (root is IHostedVisualTreeRoot hostedRoot)
            {
                if (root == popupHost)
                {
                    return true;
                }

                root = hostedRoot.Host?.VisualRoot;
            }

            return false;
        }
        
        public bool IsInsidePopup(IVisual visual)
        {
            if (_openState is null)
            {
                return false;
            }

            var popupHost = _openState.PopupHost;

            return popupHost != null && ((IVisual)popupHost).IsVisualAncestorOf(visual);
        }

        public bool IsPointerOverPopup => ((IInputElement?)_openState?.PopupHost)?.IsPointerOver ?? false;

        private void WindowDeactivated(object sender, EventArgs e)
        {
            if (!StaysOpen)
            {
                Close();
            }
        }

        private void ParentClosed(object sender, EventArgs e)
        {
            if (!StaysOpen)
            {
                Close();
            }
        }

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

            public PopupOpenState(TopLevel topLevel, IPopupHost popupHost, IDisposable cleanup)
            {
                TopLevel = topLevel;
                PopupHost = popupHost;
                _cleanup = cleanup;
            }

            public TopLevel TopLevel { get; }

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
