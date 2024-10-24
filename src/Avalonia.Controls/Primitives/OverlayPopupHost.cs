using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Primitives
{
    public class OverlayPopupHost : ContentControl, IPopupHost, IManagedPopupPositionerPopup, IInputRoot
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<Transform?> TransformProperty =
            PopupRoot.TransformProperty.AddOwner<OverlayPopupHost>();

        private readonly OverlayLayer _overlayLayer;
        private readonly ManagedPopupPositioner _positioner;
        private readonly IKeyboardNavigationHandler? _keyboardNavigationHandler;
        private Point _lastRequestedPosition;
        private PopupPositionRequest? _popupPositionRequest;
        private Size _popupSize;
        private bool _needsUpdate;

        static OverlayPopupHost()
            => KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<OverlayPopupHost>(KeyboardNavigationMode.Cycle);

        public OverlayPopupHost(OverlayLayer overlayLayer)
        {
            _overlayLayer = overlayLayer;
            _positioner = new ManagedPopupPositioner(this);
            _keyboardNavigationHandler = AvaloniaLocator.Current.GetService<IKeyboardNavigationHandler>();
            _keyboardNavigationHandler?.SetOwner(this);
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012", Justification = "Explicit set")]
        public void SetChild(Control? control)
        {
            Content = control;
        }

        /// <inheritdoc />
        public Visual? HostedVisualTreeRoot => null;

        /// <inheritdoc />
        public Transform? Transform
        {
            get => GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        bool IPopupHost.Topmost
        {
            get => false;
            set { /* Not currently supported in overlay popups */ }
        }

        private IInputRoot? InputRoot
            => TopLevel.GetTopLevel(this);

        IKeyboardNavigationHandler? IInputRoot.KeyboardNavigationHandler
            => _keyboardNavigationHandler;

        IFocusManager? IInputRoot.FocusManager
            => InputRoot?.FocusManager;

        IPlatformSettings? IInputRoot.PlatformSettings
            => InputRoot?.PlatformSettings;

        IInputElement? IInputRoot.PointerOverElement
        {
            get => InputRoot?.PointerOverElement;
            set
            {
                if (InputRoot is { } inputRoot)
                    inputRoot.PointerOverElement = value;
            }
        }

        bool IInputRoot.ShowAccessKeys
        {
            get => InputRoot?.ShowAccessKeys ?? false;
            set
            {
                if (InputRoot is { } inputRoot)
                    inputRoot.ShowAccessKeys = value;
            }
        }

        /// <inheritdoc />
        internal override Interactive? InteractiveParent => Parent as Interactive;

        /// <inheritdoc />
        public void Dispose() => Hide();

        /// <inheritdoc />
        public void Show()
        {
            _overlayLayer.Children.Add(this);

            if (Content is Visual { IsAttachedToVisualTree: false })
            {
                // We need to force a measure pass so any descendants are built, for focus to work.
                UpdateLayout();
            }
        }

        /// <inheritdoc />
        public void Hide()
        {
            _overlayLayer.Children.Remove(this);
        }

        public void TakeFocus()
        {
            // Nothing to do here: overlay popups are implemented inside the window.
        }

        [Unstable(ObsoletionMessages.MayBeRemovedInAvalonia12)]
        public void ConfigurePosition(Visual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None, PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null)
        {
            ((IPopupHost)this).ConfigurePosition(new PopupPositionRequest(target, placement, offset, anchor, gravity,
                constraintAdjustment, rect, null));
        }

        /// <inheritdoc />
        void IPopupHost.ConfigurePosition(PopupPositionRequest positionRequest)
        {
            _popupPositionRequest = positionRequest;
            _needsUpdate = true;
            UpdatePosition();
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_popupSize != finalSize)
            {
                _popupSize = finalSize;
                _needsUpdate = true;
                UpdatePosition();
            }
            return base.ArrangeOverride(finalSize);
        }

        private void UpdatePosition()
        {
            if (_needsUpdate && _popupPositionRequest is not null)
            {
                _needsUpdate = false;
                _positioner.Update(TopLevel.GetTopLevel(_overlayLayer)!, _popupPositionRequest, _popupSize, FlowDirection);
            }
        }

        IReadOnlyList<ManagedPopupPositionerScreenInfo> IManagedPopupPositionerPopup.Screens
        {
            get
            {
                var rc = new Rect(default, _overlayLayer.AvailableSize);
                var topLevel = TopLevel.GetTopLevel(this);
                if(topLevel != null)
                {
                    var padding = topLevel.InsetsManager?.SafeAreaPadding ?? default;
                    rc = rc.Deflate(padding);
                }

                return new[] {new ManagedPopupPositionerScreenInfo(rc, rc)};
            }
        }

        Rect IManagedPopupPositionerPopup.ParentClientAreaScreenGeometry =>
            new Rect(default, _overlayLayer.Bounds.Size);

        void IManagedPopupPositionerPopup.MoveAndResize(Point devicePoint, Size virtualSize)
        {
            _lastRequestedPosition = devicePoint;
            MediaContext.Instance.BeginInvokeOnRender(() =>
            {
                Canvas.SetLeft(this, _lastRequestedPosition.X);
                Canvas.SetTop(this, _lastRequestedPosition.Y);
            });
        }

        double IManagedPopupPositionerPopup.Scaling => 1;

        // TODO12: mark PrivateAPI or internal.
        [Unstable("PopupHost is considered an internal API. Use Popup or any Popup-based controls (Flyout, Tooltip) instead.")]
        public static IPopupHost CreatePopupHost(Visual target, IAvaloniaDependencyResolver? dependencyResolver)
        {
            if (TopLevel.GetTopLevel(target) is { } topLevel && topLevel.PlatformImpl?.CreatePopup() is { } popupImpl)
            {
                return new PopupRoot(topLevel, popupImpl, dependencyResolver);
            }

            if (OverlayLayer.GetOverlayLayer(target) is { } overlayLayer)
            {
                return new OverlayPopupHost(overlayLayer);
            }

            throw new InvalidOperationException(
                "Unable to create IPopupImpl and no overlay layer is found for the target control");
        }
    }
}
