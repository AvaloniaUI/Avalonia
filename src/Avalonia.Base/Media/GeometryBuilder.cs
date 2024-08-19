// Portions of this source file are adapted from the Windows Presentation Foundation (WPF) project.
// (https://github.com/dotnet/wpf)
//
// Licensed to The Avalonia Project under the MIT License, courtesy of The .NET Foundation.
//
// Portions of this source file are adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml/tree/winui3/main)
//
// Licensed to The Avalonia Project under the MIT License.

// Ignore Spelling: keypoints

using System;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Contains internal helpers used to build and draw various geometries.
    /// </summary>
    internal class GeometryBuilder
    {
        private const double PiOver2 = 1.57079633; // 90 deg to rad
        private const double Epsilon = 0.00000153; // Same as LayoutHelper.LayoutEpsilon

        /// <summary>
        /// Draws a new rounded rectangle within the given geometry context.
        /// Warning: The caller must manage and dispose the <see cref="StreamGeometryContext"/> externally.
        /// </summary>
        /// <remarks>
        /// WinUI: https://github.com/microsoft/microsoft-ui-xaml/blob/93742a178db8f625ba9299f62c21f656e0b195ad/dxaml/xcp/core/core/elements/geometry.cpp#L1072-L1079
        /// </remarks>
        /// <param name="context">The geometry context to draw into.</param>
        /// <param name="keypoints">The rounded rectangle keypoints defining the rectangle to draw.</param>
        public static void DrawRoundedCornersRectangle(
            StreamGeometryContext context,
            ref RoundedRectKeypoints keypoints)
        {
            double radiusX;
            double radiusY;

            context.BeginFigure(keypoints.TopLeft, isFilled: true);

            // Top
            context.LineTo(keypoints.TopRight);

            // TopRight corner
            radiusX = keypoints.RightTop.X - keypoints.TopRight.X;
            radiusY = keypoints.TopRight.Y - keypoints.RightTop.Y;
            radiusX = radiusX > 0 ? radiusX : -radiusX;
            radiusY = radiusY > 0 ? radiusY : -radiusY;

            context.ArcTo(
                keypoints.RightTop,
                new Size(radiusX, radiusY),
                rotationAngle: 0.0,
                isLargeArc: false,
                SweepDirection.Clockwise);

            // Right
            context.LineTo(keypoints.RightBottom);

            // BottomRight corner
            radiusX = keypoints.RightBottom.X - keypoints.BottomRight.X;
            radiusY = keypoints.BottomRight.Y - keypoints.RightBottom.Y;
            radiusX = radiusX > 0 ? radiusX : -radiusX;
            radiusY = radiusY > 0 ? radiusY : -radiusY;

            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(
                    keypoints.BottomRight,
                    new Size(radiusX, radiusY),
                    rotationAngle: 0.0,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
            }

            // Bottom
            context.LineTo(keypoints.BottomLeft);

            // BottomLeft corner
            radiusX = keypoints.BottomLeft.X - keypoints.LeftBottom.X;
            radiusY = keypoints.BottomLeft.Y - keypoints.LeftBottom.Y;
            radiusX = radiusX > 0 ? radiusX : -radiusX;
            radiusY = radiusY > 0 ? radiusY : -radiusY;

            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(
                    keypoints.LeftBottom,
                    new Size(radiusX, radiusY),
                    rotationAngle: 0.0,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
            }

            // Left
            context.LineTo(keypoints.LeftTop);

            // TopLeft corner
            radiusX = keypoints.TopLeft.X - keypoints.LeftTop.X;
            radiusY = keypoints.TopLeft.Y - keypoints.LeftTop.Y;
            radiusX = radiusX > 0 ? radiusX : -radiusX;
            radiusY = radiusY > 0 ? radiusY : -radiusY;

            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(
                    keypoints.TopLeft,
                    new Size(radiusX, radiusY),
                    rotationAngle: 0.0,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
            }

            context.EndFigure(isClosed: true);
        }

        /// <summary>
        /// Draws a new rounded rectangle within the given geometry context.
        /// Warning: The caller must manage and dispose the <see cref="StreamGeometryContext"/> externally.
        /// </summary>
        /// <param name="context">The geometry context to draw into.</param>
        /// <param name="rect">The existing rectangle dimensions without corner radii.</param>
        /// <param name="radiusX">The radius on the X-axis used to round the corners of the rectangle.</param>
        /// <param name="radiusY">The radius on the Y-axis used to round the corners of the rectangle.</param>
        public static void DrawRoundedCornersRectangle(
            StreamGeometryContext context,
            Rect rect,
            double radiusX,
            double radiusY)
        {
            var arcSize = new Size(radiusX, radiusY);

            // The rectangle is constructed as follows:
            //
            //   (origin)
            //   Corner 4            Corner 1
            //   Top/Left  Line 1    Top/Right
            //      \_   __________   _/
            //          |          |
            //   Line 4 |          | Line 2
            //       _  |__________|  _
            //      /      Line 3      \
            //   Corner 3            Corner 2
            //   Bottom/Left         Bottom/Right
            //
            // - Lines 1,3 follow the deflated rectangle bounds minus RadiusX
            // - Lines 2,4 follow the deflated rectangle bounds minus RadiusY
            // - All corners are constructed using elliptical arcs 

            context.BeginFigure(new Point(rect.Left + radiusX, rect.Top), isFilled: true);

            // Line 1 + Corner 1
            context.LineTo(new Point(rect.Right - radiusX, rect.Top));
            context.ArcTo(
                new Point(rect.Right, rect.Top + radiusY),
                arcSize,
                rotationAngle: PiOver2,
                isLargeArc: false,
                SweepDirection.Clockwise);

            // Line 2 + Corner 2
            context.LineTo(new Point(rect.Right, rect.Bottom - radiusY));
            context.ArcTo(
                new Point(rect.Right - radiusX, rect.Bottom),
                arcSize,
                rotationAngle: PiOver2,
                isLargeArc: false,
                SweepDirection.Clockwise);

            // Line 3 + Corner 3
            context.LineTo(new Point(rect.Left + radiusX, rect.Bottom));
            context.ArcTo(
                new Point(rect.Left, rect.Bottom - radiusY),
                arcSize,
                rotationAngle: PiOver2,
                isLargeArc: false,
                SweepDirection.Clockwise);

            // Line 4 + Corner 4
            context.LineTo(new Point(rect.Left, rect.Top + radiusY));
            context.ArcTo(
                new Point(rect.Left + radiusX, rect.Top),
                arcSize,
                rotationAngle: PiOver2,
                isLargeArc: false,
                SweepDirection.Clockwise);

            context.EndFigure(isClosed: true);
        }

        /// <summary>
        /// Calculates the keypoints of a rounded rectangle based on the algorithm in WinUI.
        /// These keypoints may then be drawn or transformed into other types.
        /// </summary>
        /// <param name="outerBounds">The outer bounds of the rounded rectangle.
        /// This should be the overall bounds and size of the shape/control without any
        /// corner radii or border thickness adjustments.</param>
        /// <param name="borderThickness">The unadjusted border thickness of the rounded rectangle.</param>
        /// <param name="cornerRadius">The unadjusted corner radii of the rounded rectangle.
        /// The corner radius is defined to be the middle of the border stroke (center of the border).</param>
        /// <param name="sizing">The sizing mode used to calculate the final rounded rectangle size.</param>
        /// <returns>New rounded rectangle keypoints.</returns>
        public static RoundedRectKeypoints CalculateRoundedCornersRectangleWinUI(
            Rect outerBounds,
            Thickness borderThickness,
            CornerRadius cornerRadius,
            BackgroundSizing sizing)
        {
            // This was initially derived from WinUI:
            //  - CGeometryBuilder::CalculateRoundedCornersRectangle
            //    https://github.com/microsoft/microsoft-ui-xaml/blob/93742a178db8f625ba9299f62c21f656e0b195ad/dxaml/xcp/core/core/elements/geometry.cpp#L862-L869
            //
            // It has been modified to accept a BackgroundSizing parameter directly as well
            // as to support BackgroundSizing.CenterBorder.
            //
            // Keep in mind:
            //   > In Xaml, the corner radius is defined to be the middle of the stroke
            //   > (i.e. half the border thickness extends to either side).

            bool fOuter;
            Rect boundRect = outerBounds;

            if (sizing == BackgroundSizing.InnerBorderEdge)
            {
                boundRect = outerBounds.Deflate(borderThickness);
                fOuter = false;
            }
            else if (sizing == BackgroundSizing.OuterBorderEdge)
            {
                fOuter = true;
            }
            else // CenterBorder
            {
                // This is a trick to support a 3rd state (CenterBorder) using the same WinUI-based algorithm.
                // The WinUI algorithm only supports the fOuter = True|False parameter.
                boundRect = outerBounds.Deflate(borderThickness * 0.5);
                fOuter = false;
            }

            // Start of WinUI converted code
            // WinUI's Point struct fields can be modified directly, Avalonia's Point is read-only.
            // Therefore, we will use doubles for calculation so multiple Point structs aren't
            // required during calculations -- everything can be done with these double variables.
            double fLeftTop;
            double fLeftBottom;
            double fTopLeft;
            double fTopRight;
            double fRightTop;
            double fRightBottom;
            double fBottomLeft;
            double fBottomRight;

            double left;
            double right;
            double top;
            double bottom;

            // If the caller wants to take the border into account
            // initialize the borders variables
            if (borderThickness != default)
            {
                left = 0.5 * borderThickness.Left;
                right = 0.5 * borderThickness.Right;
                top = 0.5 * borderThickness.Top;
                bottom = 0.5 * borderThickness.Bottom;
            }
            else
            {
                left = 0.0;
                right = 0.0;
                top = 0.0;
                bottom = 0.0;
            }

            // The following if/else block initializes the variables
            // of which the points of the path will be created
            // In case of outer, add the border - if any.
            // Otherwise (inner rectangle) subtract the border - if any
            if (fOuter)
            {
                if (MathUtilities.AreClose(cornerRadius.TopLeft, 0.0, Epsilon))
                {
                    fLeftTop = 0.0;
                    fTopLeft = 0.0;
                }
                else
                {
                    fLeftTop = cornerRadius.TopLeft + left;
                    fTopLeft = cornerRadius.TopLeft + top;
                }

                if (MathUtilities.AreClose(cornerRadius.TopRight, 0.0, Epsilon))
                {
                    fTopRight = 0.0;
                    fRightTop = 0.0;
                }
                else
                {
                    fTopRight = cornerRadius.TopRight + top;
                    fRightTop = cornerRadius.TopRight + right;
                }

                if (MathUtilities.AreClose(cornerRadius.BottomRight, 0.0, Epsilon))
                {
                    fRightBottom = 0.0;
                    fBottomRight = 0.0;
                }
                else
                {
                    fRightBottom = cornerRadius.BottomRight + right;
                    fBottomRight = cornerRadius.BottomRight + bottom;
                }

                if (MathUtilities.AreClose(cornerRadius.BottomLeft, 0.0, Epsilon))
                {
                    fBottomLeft = 0.0;
                    fLeftBottom = 0.0;
                }
                else
                {
                    fBottomLeft = cornerRadius.BottomLeft + bottom;
                    fLeftBottom = cornerRadius.BottomLeft + left;
                }
            }
            else
            {
                fLeftTop = Math.Max(0.0, cornerRadius.TopLeft - left);
                fTopLeft = Math.Max(0.0, cornerRadius.TopLeft - top);
                fTopRight = Math.Max(0.0, cornerRadius.TopRight - top);
                fRightTop = Math.Max(0.0, cornerRadius.TopRight - right);
                fRightBottom = Math.Max(0.0, cornerRadius.BottomRight - right);
                fBottomRight = Math.Max(0.0, cornerRadius.BottomRight - bottom);
                fBottomLeft = Math.Max(0.0, cornerRadius.BottomLeft - bottom);
                fLeftBottom = Math.Max(0.0, cornerRadius.BottomLeft - left);
            }

            double topLeftX = fLeftTop;
            double topLeftY = 0;

            double topRightX = boundRect.Width - fRightTop;
            double topRightY = 0;

            double rightTopX = boundRect.Width;
            double rightTopY = fTopRight;

            double rightBottomX = boundRect.Width;
            double rightBottomY = boundRect.Height - fBottomRight;

            double bottomRightX = boundRect.Width - fRightBottom;
            double bottomRightY = boundRect.Height;

            double bottomLeftX = fLeftBottom;
            double bottomLeftY = boundRect.Height;

            double leftBottomX = 0;
            double leftBottomY = boundRect.Height - fBottomLeft;

            double leftTopX = 0;
            double leftTopY = fTopLeft;

            // check keypoints for overlap and resolve by partitioning radii according to
            // the percentage of each one.

            // top edge
            if (topLeftX > topRightX)
            {
                double v = (fLeftTop) / (fLeftTop + fRightTop) * boundRect.Width;
                topLeftX = v;
                topRightX = v;
            }
            // right edge
            if (rightTopY > rightBottomY)
            {
                double v = (fTopRight) / (fTopRight + fBottomRight) * boundRect.Height;
                rightTopY = v;
                rightBottomY = v;
            }
            // bottom edge
            if (bottomRightX < bottomLeftX)
            {
                double v = (fLeftBottom) / (fLeftBottom + fRightBottom) * boundRect.Width;
                bottomRightX = v;
                bottomLeftX = v;
            }
            // left edge
            if (leftBottomY < leftTopY)
            {
                double v = (fTopLeft) / (fTopLeft + fBottomLeft) * boundRect.Height;
                leftBottomY = v;
                leftTopY = v;
            }

            // The above code does all calculations without taking into consideration X/Y absolute position.
            // In WinUI, this is compensated for in DrawRoundedCornersRectangle(); however, we do this here directly
            // when the final keypoints are being created.
            var keypoints = new RoundedRectKeypoints();
            keypoints.TopLeft = new Point(
                boundRect.X + topLeftX,
                boundRect.Y + topLeftY);
            keypoints.TopRight = new Point(
                boundRect.X + topRightX,
                boundRect.Y + topRightY);

            keypoints.RightTop = new Point(
                boundRect.X + rightTopX,
                boundRect.Y + rightTopY);
            keypoints.RightBottom = new Point(
                boundRect.X + rightBottomX,
                boundRect.Y + rightBottomY);

            keypoints.BottomRight = new Point(
                boundRect.X + bottomRightX,
                boundRect.Y + bottomRightY);
            keypoints.BottomLeft = new Point(
                boundRect.X + bottomLeftX,
                boundRect.Y + bottomLeftY);

            keypoints.LeftBottom = new Point(
                boundRect.X + leftBottomX,
                boundRect.Y + leftBottomY);
            keypoints.LeftTop = new Point(
                boundRect.X + leftTopX,
                boundRect.Y + leftTopY);

            return keypoints;
        }

        /// <summary>
        /// Represents the keypoints of a rounded rectangle.
        /// These keypoints can be shared between methods and turned into geometry.
        /// </summary>
        /// <remarks>
        /// A rounded rectangle is the base geometric shape used when drawing borders.
        /// It is a superset of a simple rectangle (which has corner radii set to zero).
        /// These keypoints can be combined together to produce geometries for both background
        /// and border elements.
        /// </remarks>
        internal struct RoundedRectKeypoints
        {
            // The following keypoints are defined for a rounded rectangle:
            //
            //       TopLeft                                  TopRight
            //              *--------------------------------*
            // (start)     /                                  \
            //    LeftTop *                                    * RightTop
            //            |                                    |
            //            |                                    |
            // LeftBottom *                                    * RightBottom
            //             \                                  /
            //              *--------------------------------*
            //    BottomLeft                                  BottomRight
            //
            // Or, for a simple rectangle without corner radii:
            //
            //    TopLeft = LeftTop                   TopRight = RightTop
            //  (start)   *------------------------------------*
            //            |                                    |
            //            |                                    |
            //            *------------------------------------*
            // BottomLeft = LeftBottom             BottomRight = RightBottom

            /// <summary>
            /// Initializes a new instance of the <see cref="RoundedRectKeypoints"/> struct.
            /// </summary>
            public RoundedRectKeypoints()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RoundedRectKeypoints"/> struct.
            /// </summary>
            /// <param name="roundedRect">An existing <see cref="RoundedRect"/> to initialize keypoints with.</param>
            public RoundedRectKeypoints(RoundedRect roundedRect)
            {
                LeftTop = new Point(
                    roundedRect.Rect.TopLeft.X,
                    roundedRect.Rect.TopLeft.Y + roundedRect.RadiiTopLeft.Y);
                TopLeft = new Point(
                    roundedRect.Rect.TopLeft.X + roundedRect.RadiiTopLeft.X,
                    roundedRect.Rect.TopLeft.Y);
                TopRight = new Point(
                    roundedRect.Rect.TopRight.X - roundedRect.RadiiTopRight.X,
                    roundedRect.Rect.TopRight.Y);
                RightTop = new Point(
                    roundedRect.Rect.TopRight.X,
                    roundedRect.Rect.TopRight.Y + roundedRect.RadiiTopRight.Y);
                RightBottom = new Point(
                    roundedRect.Rect.BottomRight.X,
                    roundedRect.Rect.BottomRight.Y - roundedRect.RadiiBottomRight.Y);
                BottomRight = new Point(
                    roundedRect.Rect.BottomRight.X - roundedRect.RadiiBottomRight.X,
                    roundedRect.Rect.BottomRight.Y);
                BottomLeft = new Point(
                    roundedRect.Rect.BottomLeft.X + roundedRect.RadiiBottomLeft.X,
                    roundedRect.Rect.BottomLeft.Y);
                LeftBottom = new Point(
                    roundedRect.Rect.BottomLeft.X,
                    roundedRect.Rect.BottomRight.Y - roundedRect.RadiiBottomLeft.Y);
            }

            /// <summary>
            /// Gets the topmost point in the left line segment of the rectangle.
            /// </summary>
            public Point LeftTop { get; set; }

            /// <summary>
            /// Gets the leftmost point in the top line segment of the rectangle.
            /// </summary>
            public Point TopLeft { get; set; }

            /// <summary>
            /// Gets the rightmost point in the top line segment of the rectangle.
            /// </summary>
            public Point TopRight { get; set; }

            /// <summary>
            /// Gets the topmost point in the right line segment of the rectangle.
            /// </summary>
            public Point RightTop { get; set; }

            /// <summary>
            /// Gets the bottommost point in the right line segment of the rectangle.
            /// </summary>
            public Point RightBottom { get; set; }

            /// <summary>
            /// Gets the rightmost point in the bottom line segment of the rectangle.
            /// </summary>
            public Point BottomRight { get; set; }

            /// <summary>
            /// Gets the leftmost point in the bottom line segment of the rectangle.
            /// </summary>
            public Point BottomLeft { get; set; }

            /// <summary>
            /// Gets the bottommost point in the left line segment of the rectangle.
            /// </summary>
            public Point LeftBottom { get; set; }

            /// <summary>
            /// Gets a value indicating whether the rounded rectangle is actually rounded on
            /// any corner. If false the key points represent a simple rectangle.
            /// </summary>
            public bool IsRounded
            {
                get
                {
                    return (TopLeft != LeftTop ||
                            TopRight != RightTop ||
                            BottomLeft != LeftBottom ||
                            BottomRight != RightBottom);
                }
            }

            /// <summary>
            /// Converts the keypoints into a simple rectangle (with no corners).
            /// This is equivalent to the outer rectangle with zero corner radii.
            /// </summary>
            /// <remarks>
            /// Warning: This will force the keypoints into a simple rectangle without
            /// any rounded corners. Use <see cref="IsRounded"/> to determine if corner
            /// information is otherwise available.
            /// </remarks>
            /// <returns>A new rectangle representing the keypoints.</returns>
            public Rect ToRect()
            {
                return new Rect(
                    topLeft: new Point(
                        x: LeftTop.X,
                        y: TopLeft.Y),
                    bottomRight: new Point(
                        x: RightBottom.X,
                        y: BottomRight.Y));
            }

            /// <summary>
            /// Converts the keypoints into a rounded rectangle with elliptical corner radii.
            /// </summary>
            /// <remarks>
            /// Elliptical corner radius (represented by <see cref="Vector"/>) is more powerful
            /// than circular corner radius (represented by a <see cref="CornerRadius"/>).
            /// Elliptical is a superset of circular.
            /// </remarks>
            /// <returns>A new rounded rectangle representing the keypoints.</returns>
            public RoundedRect ToRoundedRect()
            {
                return new RoundedRect(
                    ToRect(),
                    radiiTopLeft: new Vector(
                        x: TopLeft.X - LeftTop.X,
                        y: LeftTop.Y - TopLeft.Y),
                    radiiTopRight: new Vector(
                        x: RightTop.X - TopRight.X,
                        y: RightTop.Y - TopRight.Y),
                    radiiBottomRight: new Vector(
                        x: RightBottom.X - BottomRight.X,
                        y: BottomRight.Y - RightBottom.Y),
                    radiiBottomLeft: new Vector(
                        x: BottomLeft.X - LeftBottom.X,
                        y: BottomLeft.Y - LeftBottom.Y));
            }
        }
    }
}
