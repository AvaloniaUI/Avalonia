// The documentation and flag names in this file are initially taken from
// xdg_shell wayland protocol this API is designed after
// therefore, I'm including the license from wayland-protocols repo

/* 
Copyright © 2008-2013 Kristian Høgsberg
Copyright © 2010-2013 Intel Corporation
Copyright © 2013      Rafael Antognolli
Copyright © 2013      Jasper St. Pierre
Copyright © 2014      Jonas Ådahl
Copyright © 2014      Jason Ekstrand
Copyright © 2014-2015 Collabora, Ltd.
Copyright © 2015      Red Hat Inc.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

---

The above is the version of the MIT "Expat" License used by X.org:

    http://cgit.freedesktop.org/xorg/xserver/tree/COPYING
    
    
Adjustments for Avalonia needs:
Copyright © 2019 Nikita Tsukanov
    
    
*/

using System;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives.PopupPositioning
{
    /// <summary>
    /// 
    /// The IPopupPositioner provides a collection of rules for the placement of a
    /// a popup relative to its parent. Rules can be defined to ensure
    /// the popup remains within the visible area's borders, and to
    /// specify how the popup changes its position, such as sliding along
    /// an axis, or flipping around a rectangle. These positioner-created rules are
    /// constrained by the requirement that a popup must intersect with or
    /// be at least partially adjacent to its parent surface.
    /// </summary>
    public struct PopupPositionerParameters
    {
        private PopupPositioningEdge _gravity;
        private PopupPositioningEdge _anchor;

        /// <summary>
        /// Set the size of the popup that is to be positioned with the positioner
        /// object. The size is in scaled coordinates.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Specify the anchor rectangle within the parent that the popup
        /// will be placed relative to. The rectangle is relative to the
        /// parent geometry
        /// 
        /// The anchor rectangle may not extend outside the window geometry of the
        /// popup's parent. The anchor rectangle is in scaled coordinates
        /// </summary>
        public Rect AnchorRectangle { get; set; }


        /// <summary>
        /// Defines the anchor point for the anchor rectangle. The specified anchor
        /// is used derive an anchor point that the popup will be
        /// positioned relative to. If a corner anchor is set (e.g. 'TopLeft' or
        /// 'BottomRight'), the anchor point will be at the specified corner;
        /// otherwise, the derived anchor point will be centered on the specified
        /// edge, or in the center of the anchor rectangle if no edge is specified.
        /// </summary>
        public PopupPositioningEdge Anchor
        {
            get => _anchor;
            set
            {
                PopupPositioningEdgeHelper.ValidateEdge(value);
                _anchor = value;
            }
        }

        /// <summary>
        /// Defines in what direction a popup should be positioned, relative to
        /// the anchor point of the parent. If a corner gravity is
        /// specified (e.g. 'BottomRight' or 'TopLeft'), then the popup
        /// will be placed towards the specified gravity; otherwise, the popup
        /// will be centered over the anchor point on any axis that had no
        /// gravity specified.
        /// </summary>
        public PopupPositioningEdge Gravity
        {
            get => _gravity;
            set
            {
                PopupPositioningEdgeHelper.ValidateEdge(value);
                _gravity = value;
            }
        }

        /// <summary>
        /// Specify how the popup should be positioned if the originally intended
        /// position caused the popup to be constrained, meaning at least
        /// partially outside positioning boundaries set by the positioner. The
        /// adjustment is set by constructing a bitmask describing the adjustment to
        /// be made when the popup is constrained on that axis.
        /// 
        /// If no bit for one axis is set, the positioner will assume that the child
        /// surface should not change its position on that axis when constrained.
        /// 
        /// If more than one bit for one axis is set, the order of how adjustments
        /// are applied is specified in the corresponding adjustment descriptions.
        /// 
        /// The default adjustment is none.
        /// </summary>
        public PopupPositionerConstraintAdjustment ConstraintAdjustment { get; set; }
        
        /// <summary>
        /// Specify the popup position offset relative to the position of the
        /// anchor on the anchor rectangle and the anchor on the popup. For
        /// example if the anchor of the anchor rectangle is at (x, y), the popup
        /// has the gravity bottom|right, and the offset is (ox, oy), the calculated
        /// surface position will be (x + ox, y + oy). The offset position of the
        /// surface is the one used for constraint testing. See
        /// set_constraint_adjustment.
        /// 
        /// An example use case is placing a popup menu on top of a user interface
        /// element, while aligning the user interface element of the parent surface
        /// with some user interface element placed somewhere in the popup.
        /// </summary>
        public Point Offset { get; set; }
    }
    
    /// <summary>
    /// The constraint adjustment value define ways how popup position will
    /// be adjusted if the unadjusted position would result in the popup
    /// being partly constrained.
    /// 
    /// Whether a popup is considered 'constrained' is left to the positioner
    /// to determine. For example, the popup may be partly outside the
    /// target platform defined 'work area', thus necessitating the popup's
    /// position be adjusted until it is entirely inside the work area.
    /// </summary>
    [Flags]
    public enum PopupPositionerConstraintAdjustment
    {
        /// <summary>
        /// Don't alter the surface position even if it is constrained on some
        /// axis, for example partially outside the edge of an output.
        /// </summary>
        None = 0,

        /// <summary>
        /// Slide the surface along the x axis until it is no longer constrained.
        ///        First try to slide towards the direction of the gravity on the x axis
        ///        until either the edge in the opposite direction of the gravity is
        ///        unconstrained or the edge in the direction of the gravity is
        ///        constrained.
        ///
        ///        Then try to slide towards the opposite direction of the gravity on the
        ///        x axis until either the edge in the direction of the gravity is
        ///        unconstrained or the edge in the opposite direction of the gravity is
        ///        constrained.
        /// </summary>
        SlideX = 1,


        /// <summary>
        ///            Slide the surface along the y axis until it is no longer constrained.
        /// 
        /// First try to slide towards the direction of the gravity on the y axis
        /// until either the edge in the opposite direction of the gravity is
        /// unconstrained or the edge in the direction of the gravity is
        /// constrained.
        /// 
        /// Then try to slide towards the opposite direction of the gravity on the
        /// y axis until either the edge in the direction of the gravity is
        /// unconstrained or the edge in the opposite direction of the gravity is
        /// constrained.
        /// */
        /// </summary>
        SlideY = 2,

        /// <summary>
        /// Invert the anchor and gravity on the x axis if the surface is
        /// constrained on the x axis. For example, if the left edge of the
        /// surface is constrained, the gravity is 'left' and the anchor is
        /// 'left', change the gravity to 'right' and the anchor to 'right'.
        /// 
        /// If the adjusted position also ends up being constrained, the resulting
        /// position of the flip_x adjustment will be the one before the
        /// adjustment.
        /// </summary>
        FlipX = 4,

        /// <summary>
        /// Invert the anchor and gravity on the y axis if the surface is
        /// constrained on the y axis. For example, if the bottom edge of the
        /// surface is constrained, the gravity is 'bottom' and the anchor is
        /// 'bottom', change the gravity to 'top' and the anchor to 'top'.
        /// 
        /// The adjusted position is calculated given the original anchor
        /// rectangle and offset, but with the new flipped anchor and gravity
        /// values.
        /// 
        /// If the adjusted position also ends up being constrained, the resulting
        /// position of the flip_y adjustment will be the one before the
        /// adjustment.
        /// </summary>
        FlipY = 8,
        All = SlideX|SlideY|FlipX|FlipY
    }

    static class PopupPositioningEdgeHelper
    {
        public static void ValidateEdge(this PopupPositioningEdge edge)
        {
            if (((edge & PopupPositioningEdge.Left) != 0 && (edge & PopupPositioningEdge.Right) != 0)
                ||
                ((edge & PopupPositioningEdge.Top) != 0 && (edge & PopupPositioningEdge.Bottom) != 0))
                throw new ArgumentException("Opposite edges specified");
        }

        public static PopupPositioningEdge Flip(this PopupPositioningEdge edge)
        {
            var hmask = PopupPositioningEdge.Left | PopupPositioningEdge.Right;
            var vmask = PopupPositioningEdge.Top | PopupPositioningEdge.Bottom;
            if ((edge & hmask) != 0)
                edge ^= hmask;
            if ((edge & vmask) != 0)
                edge ^= vmask;
            return edge;
        }

        public static PopupPositioningEdge FlipX(this PopupPositioningEdge edge)
        {
            if ((edge & PopupPositioningEdge.HorizontalMask) != 0)
                edge ^= PopupPositioningEdge.HorizontalMask;
            return edge;
        }
        
        public static PopupPositioningEdge FlipY(this PopupPositioningEdge edge)
        {
            if ((edge & PopupPositioningEdge.VerticalMask) != 0)
                edge ^= PopupPositioningEdge.VerticalMask;
            return edge;
        }
        
    }

    [Flags]
    public enum PopupPositioningEdge
    {
        None,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right,

        
        VerticalMask = Top | Bottom,
        HorizontalMask = Left | Right,
        AllMask = VerticalMask|HorizontalMask
    }

    public interface IPopupPositioner
    {
        void Update(PopupPositionerParameters parameters);
    }

    static class PopupPositionerExtensions
    {
        public static void ConfigurePosition(ref this PopupPositionerParameters positionerParameters,
            TopLevel topLevel,
            IVisual target, PlacementMode placement, Point offset,
            PopupPositioningEdge anchor, PopupPositioningEdge gravity)
        {
            // We need a better way for tracking the last pointer position
            var pointer = topLevel.PointToClient(topLevel.PlatformImpl.MouseDevice.Position);
            
            positionerParameters.Offset = offset;
            positionerParameters.ConstraintAdjustment = PopupPositionerConstraintAdjustment.All;
            if (placement == PlacementMode.Pointer)
            {
                positionerParameters.AnchorRectangle = new Rect(pointer, new Size(1, 1));
                positionerParameters.Anchor = PopupPositioningEdge.BottomRight;
                positionerParameters.Gravity = PopupPositioningEdge.BottomRight;
            }
            else
            {
                if (target == null)
                    throw new InvalidOperationException("Placement mode is not Pointer and PlacementTarget is null");
                var matrix = target.TransformToVisual(topLevel);
                if (matrix == null)
                {
                    if (target.GetVisualRoot() == null)
                        throw new InvalidCastException("Target control is not attached to the visual tree");
                    throw new InvalidCastException("Target control is not in the same tree as the popup parent");
                }

                positionerParameters.AnchorRectangle = new Rect(default, target.Bounds.Size)
                    .TransformToAABB(matrix.Value);

                if (placement == PlacementMode.Right)
                {
                    positionerParameters.Anchor = PopupPositioningEdge.TopRight;
                    positionerParameters.Gravity = PopupPositioningEdge.BottomRight;
                }
                else if (placement == PlacementMode.Bottom)
                {
                    positionerParameters.Anchor = PopupPositioningEdge.BottomLeft;
                    positionerParameters.Gravity = PopupPositioningEdge.BottomRight;
                }
                else if (placement == PlacementMode.Left)
                {
                    positionerParameters.Anchor = PopupPositioningEdge.TopLeft;
                    positionerParameters.Gravity = PopupPositioningEdge.BottomLeft;
                }
                else if (placement == PlacementMode.Top)
                {
                    positionerParameters.Anchor = PopupPositioningEdge.TopLeft;
                    positionerParameters.Gravity = PopupPositioningEdge.TopRight;
                }
                else if (placement == PlacementMode.AnchorAndGravity)
                {
                    positionerParameters.Anchor = anchor;
                    positionerParameters.Gravity = gravity;
                }
                else
                    throw new InvalidOperationException("Invalid value for Popup.PlacementMode");
            }
        }
    }

}
