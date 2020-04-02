using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.Primitives.PopupPositioning
{
    public interface IManagedPopupPositionerPopup
    {
        IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens { get; }
        Rect ParentClientAreaScreenGeometry { get; }
        void MoveAndResize(Point devicePoint, Size virtualSize);
        Point TranslatePoint(Point pt);
        Size TranslateSize(Size size);
    }

    public class ManagedPopupPositionerScreenInfo
    {
        public Rect Bounds { get; }
        public Rect WorkingArea { get; }

        public ManagedPopupPositionerScreenInfo(Rect bounds, Rect workingArea)
        {
            Bounds = bounds;
            WorkingArea = workingArea;
        }
    }

    public class ManagedPopupPositioner : IPopupPositioner
    {
        private readonly IManagedPopupPositionerPopup _popup;

        public ManagedPopupPositioner(IManagedPopupPositionerPopup popup)
        {
            _popup = popup;
        }


        private static Point GetAnchorPoint(Rect anchorRect, PopupPositioningEdge edge)
        {
            double x, y;
            if ((edge & PopupPositioningEdge.Left) != 0)
                x = anchorRect.X;
            else if ((edge & PopupPositioningEdge.Right) != 0)
                x = anchorRect.Right;
            else
                x = anchorRect.X + anchorRect.Width / 2;
            
            if ((edge & PopupPositioningEdge.Top) != 0)
                y = anchorRect.Y;
            else if ((edge & PopupPositioningEdge.Bottom) != 0)
                y = anchorRect.Bottom;
            else
                y = anchorRect.Y + anchorRect.Height / 2;
            return new Point(x, y);
        }

        private static Point Gravitate(Point anchorPoint, Size size, PopupPositioningEdge gravity)
        {
            double x, y;
            if ((gravity & PopupPositioningEdge.Left) != 0)
                x = -size.Width;
            else if ((gravity & PopupPositioningEdge.Right) != 0)
                x = 0;
            else
                x = -size.Width / 2;
            
            if ((gravity & PopupPositioningEdge.Top) != 0)
                y = -size.Height;
            else if ((gravity & PopupPositioningEdge.Bottom) != 0)
                y = 0;
            else
                y = -size.Height / 2;
            return anchorPoint + new Point(x, y);
        }

        public void Update(PopupPositionerParameters parameters)
        {

            Update(_popup.TranslateSize(parameters.Size), parameters.Size,
                new Rect(_popup.TranslatePoint(parameters.AnchorRectangle.TopLeft),
                    _popup.TranslateSize(parameters.AnchorRectangle.Size)),
                parameters.Anchor, parameters.Gravity, parameters.ConstraintAdjustment,
                _popup.TranslatePoint(parameters.Offset));
        }

        
        private void Update(Size translatedSize, Size originalSize,
            Rect anchorRect, PopupPositioningEdge anchor, PopupPositioningEdge gravity,
            PopupPositionerConstraintAdjustment constraintAdjustment, Point offset)
        {
            var parentGeometry = _popup.ParentClientAreaScreenGeometry;
            anchorRect = anchorRect.Translate(parentGeometry.TopLeft);
            
            Rect GetBounds()
            {
                var screens = _popup.Screens;
                
                var targetScreen = screens.FirstOrDefault(s => s.Bounds.Contains(anchorRect.TopLeft))
                                   ?? screens.FirstOrDefault(s => s.Bounds.Intersects(anchorRect))
                                   ?? screens.FirstOrDefault(s => s.Bounds.Contains(parentGeometry.TopLeft))
                                   ?? screens.FirstOrDefault(s => s.Bounds.Intersects(parentGeometry))
                                   ?? screens.FirstOrDefault();

                if (targetScreen != null && targetScreen.WorkingArea.IsEmpty)
                {
                    return targetScreen.Bounds;
                }
                
                return targetScreen?.WorkingArea
                       ?? new Rect(0, 0, double.MaxValue, double.MaxValue);
            }

            var bounds = GetBounds();

            bool FitsInBounds(Rect rc, PopupPositioningEdge edge = PopupPositioningEdge.AllMask)
            {
                if ((edge & PopupPositioningEdge.Left) != 0
                    && rc.X < bounds.X)
                    return false;

                if ((edge & PopupPositioningEdge.Top) != 0
                    && rc.Y < bounds.Y)
                    return false;

                if ((edge & PopupPositioningEdge.Right) != 0
                    && rc.Right > bounds.Right)
                    return false;

                if ((edge & PopupPositioningEdge.Bottom) != 0
                    && rc.Bottom > bounds.Bottom)
                    return false;

                return true;
            }

            Rect GetUnconstrained(PopupPositioningEdge a, PopupPositioningEdge g) =>
                new Rect(Gravitate(GetAnchorPoint(anchorRect, a), translatedSize, g) + offset, translatedSize);


            var geo = GetUnconstrained(anchor, gravity);

            // If flipping geometry and anchor is allowed and helps, use the flipped one,
            // otherwise leave it as is
            if (!FitsInBounds(geo, PopupPositioningEdge.HorizontalMask)
                && (constraintAdjustment & PopupPositionerConstraintAdjustment.FlipX) != 0)
            {
                var flipped = GetUnconstrained(anchor.FlipX(), gravity.FlipX());
                if (FitsInBounds(flipped, PopupPositioningEdge.HorizontalMask))
                    geo = geo.WithX(flipped.X);
            }

            // If sliding is allowed, try moving the rect into the bounds
            if ((constraintAdjustment & PopupPositionerConstraintAdjustment.SlideX) != 0)
            {
                geo = geo.WithX(Math.Max(geo.X, bounds.X));
                if (geo.Right > bounds.Right)
                    geo = geo.WithX(bounds.Right - geo.Width);
            }
            
            // If flipping geometry and anchor is allowed and helps, use the flipped one,
            // otherwise leave it as is
            if (!FitsInBounds(geo, PopupPositioningEdge.VerticalMask)
                && (constraintAdjustment & PopupPositionerConstraintAdjustment.FlipY) != 0)
            {
                var flipped = GetUnconstrained(anchor.FlipY(), gravity.FlipY());
                if (FitsInBounds(flipped, PopupPositioningEdge.VerticalMask))
                    geo = geo.WithY(flipped.Y);
            }

            // If sliding is allowed, try moving the rect into the bounds
            if ((constraintAdjustment & PopupPositionerConstraintAdjustment.SlideY) != 0)
            {
                geo = geo.WithY(Math.Max(geo.Y, bounds.Y));
                if (geo.Bottom > bounds.Bottom)
                    geo = geo.WithY(bounds.Bottom - geo.Height);
            }

            _popup.MoveAndResize(geo.TopLeft, originalSize);
        }
    }
}
