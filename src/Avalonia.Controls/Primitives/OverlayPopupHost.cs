using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public class OverlayPopupHost : ContentControl, IPopupHost, IManagedPopupPositionerPopup
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<Transform?> TransformProperty =
            PopupRoot.TransformProperty.AddOwner<OverlayPopupHost>();

        private readonly OverlayLayer _overlayLayer;
        private readonly ManagedPopupPositioner _positioner;
        private PopupPositionerParameters _positionerParameters;
        private Point _lastRequestedPosition;
        private bool _shown;

        public OverlayPopupHost(OverlayLayer overlayLayer)
        {
            _overlayLayer = overlayLayer;
            _positioner = new ManagedPopupPositioner(this);
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

        /// <inheritdoc />
        internal override Interactive? InteractiveParent => Parent as Interactive;

        /// <inheritdoc />
        public void Dispose() => Hide();

        /// <inheritdoc />
        public void Show()
        {
            _overlayLayer.Children.Add(this);
            _shown = true;
        }

        /// <inheritdoc />
        public void Hide()
        {
            _overlayLayer.Children.Remove(this);
            _shown = false;
        }

        /// <inheritdoc />
        public void ConfigurePosition(Visual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None, PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null)
        {
            _positionerParameters.ConfigurePosition((TopLevel)_overlayLayer.GetVisualRoot()!, target, placement, offset, anchor,
                gravity, constraintAdjustment, rect, FlowDirection);
            UpdatePosition();
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_positionerParameters.Size != finalSize)
            {
                _positionerParameters.Size = finalSize;
                UpdatePosition();
            }
            return base.ArrangeOverride(finalSize);
        }


        private void UpdatePosition()
        {
            // Don't bother the positioner with layout system artifacts
            if (_positionerParameters.Size.Width == 0 || _positionerParameters.Size.Height == 0)
                return;
            if (_shown)
            {
                _positioner.Update(_positionerParameters);
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
