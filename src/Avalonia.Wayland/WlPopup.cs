using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland
{
    internal class WlPopup : WlWindow, IPopupImpl, IPopupPositioner, XdgPopup.IEvents, XdgPositioner.IEvents
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly XdgPositioner _xdgPositioner;

        private XdgPopup? _xdgPopup;
        private uint _repositionToken;
        private bool _isLightDismissEnabled;

        internal WlPopup(AvaloniaWaylandPlatform platform, WlWindow parent) : base(platform)
        {
            _platform = platform;
            _xdgPositioner = platform.XdgWmBase.CreatePositioner();
            _xdgPositioner.SetReactive();
            Parent = parent;
        }

        public IPopupPositioner PopupPositioner => this;

        public void SetWindowManagerAddShadowHint(bool enabled) { }

        public void SetIsLightDismissEnabledHint(bool enabled) => _isLightDismissEnabled = enabled;

        public override void Show(bool activate, bool isDialog)
        {
            if (_xdgPopup is null)
            {
                _xdgPopup = XdgSurface.GetPopup(Parent!.XdgSurface, _xdgPositioner);
                _xdgPopup.Events = this;
                if (_isLightDismissEnabled)
                    _xdgPopup.Grab(_platform.WlSeat, _platform.WlInputDevice.UserActionDownSerial);
            }

            base.Show(activate, isDialog);
        }

        public override void Hide()
        {
            // TODO: Hide() is called before Dispose(), which means this wl_popup would be destroyed before possible child popups, which is a protocol violation.
            // We just wait for Dispose() to call Close that first closes all child popups.
        }

        public void Update(PopupPositionerParameters parameters)
        {
            Resize(parameters.Size);
            _xdgPositioner.SetAnchor(ParsePopupAnchor(parameters.Anchor));
            _xdgPositioner.SetGravity(ParsePopupGravity(parameters.Gravity));
            _xdgPositioner.SetOffset((int)parameters.Offset.X, (int)parameters.Offset.Y);
            _xdgPositioner.SetSize((int)parameters.Size.Width, (int)parameters.Size.Height);
            _xdgPositioner.SetAnchorRect((int)parameters.AnchorRectangle.X, (int)parameters.AnchorRectangle.Y, (int)Math.Max(1, parameters.AnchorRectangle.Width), (int)Math.Max(1, parameters.AnchorRectangle.Height));
            _xdgPositioner.SetConstraintAdjustment((uint)parameters.ConstraintAdjustment);
            _xdgPopup?.Reposition(_xdgPositioner, _repositionToken++);
        }

        public void OnConfigure(XdgPopup eventSender, int x, int y, int width, int height)
        {
            PendingState.Position = new PixelPoint(x, y);
            var size = new PixelSize(width, height);
            if (size != default)
                PendingState.Size = size;
        }

        public void OnPopupDone(XdgPopup eventSender)
        {
            if (_platform.WlInputDevice.PointerHandler is null || InputRoot is null)
                return;
            var args = new RawPointerEventArgs(_platform.WlInputDevice.PointerHandler.MouseDevice, 0, InputRoot, RawPointerEventType.NonClientLeftButtonDown, new Point(), _platform.WlInputDevice.RawInputModifiers);
            Input?.Invoke(args);
        }

        public void OnRepositioned(XdgPopup eventSender, uint token) => PositionChanged?.Invoke(AppliedState.Position);

        public override void Dispose()
        {
            _xdgPositioner.Dispose();
            _xdgPopup?.Dispose();
            base.Dispose();
        }

        private static XdgPositioner.AnchorEnum ParsePopupAnchor(PopupAnchor popupAnchor) => popupAnchor switch
        {
            PopupAnchor.TopLeft => XdgPositioner.AnchorEnum.TopLeft,
            PopupAnchor.TopRight => XdgPositioner.AnchorEnum.TopRight,
            PopupAnchor.BottomLeft => XdgPositioner.AnchorEnum.BottomLeft,
            PopupAnchor.BottomRight => XdgPositioner.AnchorEnum.BottomRight,
            PopupAnchor.Top => XdgPositioner.AnchorEnum.Top,
            PopupAnchor.Left => XdgPositioner.AnchorEnum.Left,
            PopupAnchor.Bottom => XdgPositioner.AnchorEnum.Bottom,
            PopupAnchor.Right => XdgPositioner.AnchorEnum.Right,
            _ => XdgPositioner.AnchorEnum.None
        };

        private static XdgPositioner.GravityEnum ParsePopupGravity(PopupGravity popupGravity) => popupGravity switch
        {
            PopupGravity.TopLeft => XdgPositioner.GravityEnum.TopLeft,
            PopupGravity.TopRight => XdgPositioner.GravityEnum.TopRight,
            PopupGravity.BottomLeft => XdgPositioner.GravityEnum.BottomLeft,
            PopupGravity.BottomRight => XdgPositioner.GravityEnum.BottomRight,
            PopupGravity.Top => XdgPositioner.GravityEnum.Top,
            PopupGravity.Left => XdgPositioner.GravityEnum.Left,
            PopupGravity.Bottom => XdgPositioner.GravityEnum.Bottom,
            PopupGravity.Right => XdgPositioner.GravityEnum.Right,
            _ => XdgPositioner.GravityEnum.None
        };
    }
}
