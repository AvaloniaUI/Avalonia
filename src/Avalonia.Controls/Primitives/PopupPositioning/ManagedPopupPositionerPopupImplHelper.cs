using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Primitives.PopupPositioning
{
    /// <summary>
    /// This class is used to simplify integration of IPopupImpl implementations with popup positioner
    /// </summary>
    [PrivateApi]
    public class ManagedPopupPositionerPopupImplHelper : IManagedPopupPositionerPopup 
    {
        private readonly IWindowBaseImpl _parent;

        public delegate void MoveResizeDelegate(PixelPoint position, Size size, double scaling);
        private readonly MoveResizeDelegate _moveResize;

        public ManagedPopupPositionerPopupImplHelper(IWindowBaseImpl parent, MoveResizeDelegate moveResize)
        {
            _parent = parent;
            _moveResize = moveResize;
        }

        public IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens =>

            _parent.Screen.AllScreens
                .Select(s => new ManagedPopupPositionerScreenInfo(s.Bounds.ToRect(1), s.WorkingArea.ToRect(1)))
                .ToArray();

        public Rect ParentClientAreaScreenGeometry
        {
            get
            {
                // Popup positioner operates with abstract coordinates, but in our case they are pixel ones
                var point = _parent.PointToScreen(default);
                var size = _parent.ClientSize * Scaling;
                return new Rect(point.X, point.Y, size.Width, size.Height);

            }
        }

        public void MoveAndResize(Point devicePoint, Size virtualSize)
        {
            _moveResize(new PixelPoint((int)devicePoint.X, (int)devicePoint.Y), virtualSize, _parent.RenderScaling);
        }

        public virtual double Scaling => _parent.DesktopScaling;
    }
}
