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

namespace Avalonia.Media
{
    /// <summary>
    /// Contains internal helpers used to build and draw various geometries.
    /// </summary>
    internal class GeometryBuilder
    {
        private const double PiOver2 = 1.57079633; // 90 deg to rad

        /// <summary>
        /// Draws a new rounded rectangle within the given geometry context.
        /// </summary>
        /// <remarks>
        /// WinUI: https://github.com/microsoft/microsoft-ui-xaml/blob/93742a178db8f625ba9299f62c21f656e0b195ad/dxaml/xcp/core/core/elements/geometry.cpp#L1072-L1079
        /// Wpf:
        /// </remarks>
        /// <param name="context">The geometry context to draw into.</param>
        /// <param name="keypoints">The rounded rectangle keypoints defining the rectangle to draw.</param>
        public static void DrawRoundedCornersRectangle(
            StreamGeometryContext context,
            RoundedRectKeypoints keypoints)
        {
            context.BeginFigure(keypoints.TopLeft, true);

            // Top
            context.LineTo(keypoints.TopRight);

            // TopRight corner
            var radiusX = keypoints.RightTop.X - keypoints.TopRight.X;
            var radiusY = keypoints.TopRight.Y - keypoints.RightTop.Y;
            context.ArcTo(
                keypoints.RightTop,
                new Size(radiusX, radiusY),
                rotationAngle: PiOver2,
                isLargeArc: false,
                SweepDirection.Clockwise);

            // Right
            context.LineTo(keypoints.RightBottom);

            // BottomRight corner
            radiusX = keypoints.RightBottom.X - keypoints.BottomRight.X;
            radiusY = keypoints.BottomRight.Y - keypoints.RightBottom.Y;
            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(
                    keypoints.BottomRight,
                    new Size(radiusX, radiusY),
                    rotationAngle: PiOver2,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
            }

            // Bottom
            context.LineTo(keypoints.BottomLeft);

            // BottomLeft corner
            radiusX = keypoints.BottomLeft.X - keypoints.LeftBottom.X;
            radiusY = keypoints.BottomLeft.Y - keypoints.LeftBottom.Y;
            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(
                    keypoints.LeftBottom,
                    new Size(radiusX, radiusY),
                    rotationAngle: PiOver2,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
            }

            // Left
            context.LineTo(keypoints.LeftTop);

            // TopLeft corner
            radiusX = keypoints.TopLeft.X - keypoints.LeftTop.X;
            radiusY = keypoints.TopLeft.Y - keypoints.LeftTop.Y;
            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(
                    keypoints.TopLeft,
                    new Size(radiusX, radiusY),
                    rotationAngle: PiOver2,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
            }

            context.EndFigure(true);
        }

        public static RoundedRectKeypoints CalculateRoundedCornersRectangleV1(
            Rect boundRect,
            Thickness borderThickness,
            CornerRadius cornerRadius,
            bool inner)
        {
            // This was initially derived from WPF:
            //  - Border.ArrangeOverride()
            //    https://github.com/dotnet/wpf/blob/137b671131455a5c252a297747725ddce5a21c63/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Border.cs#L261C10-L261C10
            //  - Border.Radii struct
            //    https://github.com/dotnet/wpf/blob/137b671131455a5c252a297747725ddce5a21c63/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Border.cs#L933
            //  - Border.GenerateGeometry()
            //    https://github.com/dotnet/wpf/blob/137b671131455a5c252a297747725ddce5a21c63/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Border.cs#L650

            var left = 0.5 * borderThickness.Left;
            var top = 0.5 * borderThickness.Top;
            var right = 0.5 * borderThickness.Right;
            var bottom = 0.5 * borderThickness.Bottom;

            double leftTopY;
            double topLeftX;
            double topRightX;
            double rightTopY;
            double rightBottomY;
            double bottomRightX;
            double bottomLeftX;
            double leftBottomY;

            if (inner)
            {
                leftTopY = Math.Max(0, cornerRadius.TopLeft - top) + boundRect.TopLeft.Y;
                topLeftX = Math.Max(0, cornerRadius.TopLeft - left) + boundRect.TopLeft.X;
                topRightX = boundRect.Width - Math.Max(0, cornerRadius.TopRight - top) + boundRect.TopLeft.X;
                rightTopY = Math.Max(0, cornerRadius.TopRight - right) + boundRect.TopLeft.Y;
                rightBottomY = boundRect.Height - Math.Max(0, cornerRadius.BottomRight - bottom) +
                               boundRect.TopLeft.Y;
                bottomRightX = boundRect.Width - Math.Max(0, cornerRadius.BottomRight - right) +
                               boundRect.TopLeft.X;
                bottomLeftX = Math.Max(0, cornerRadius.BottomLeft - left) + boundRect.TopLeft.X;
                leftBottomY = boundRect.Height - Math.Max(0, cornerRadius.BottomLeft - bottom) +
                              boundRect.TopLeft.Y;
            }
            else
            {
                leftTopY = cornerRadius.TopLeft + top + boundRect.TopLeft.Y;
                topLeftX = cornerRadius.TopLeft + left + boundRect.TopLeft.X;
                topRightX = boundRect.Width - (cornerRadius.TopRight + right) + boundRect.TopLeft.X;
                rightTopY = cornerRadius.TopRight + top + boundRect.TopLeft.Y;
                rightBottomY = boundRect.Height - (cornerRadius.BottomRight + bottom) + boundRect.TopLeft.Y;
                bottomRightX = boundRect.Width - (cornerRadius.BottomRight + right) + boundRect.TopLeft.X;
                bottomLeftX = cornerRadius.BottomLeft + left + boundRect.TopLeft.X;
                leftBottomY = boundRect.Height - (cornerRadius.BottomLeft + bottom) + boundRect.TopLeft.Y;
            }

            var leftTopX = boundRect.TopLeft.X;
            var topLeftY = boundRect.TopLeft.Y;
            var topRightY = boundRect.TopLeft.Y;
            var rightTopX = boundRect.Width + boundRect.TopLeft.X;
            var rightBottomX = boundRect.Width + boundRect.TopLeft.X;
            var bottomRightY = boundRect.Height + boundRect.TopLeft.Y;
            var bottomLeftY = boundRect.Height + boundRect.TopLeft.Y;
            var leftBottomX = boundRect.TopLeft.X;

            var keypoints = new RoundedRectKeypoints();
            keypoints.LeftTop = new Point(leftTopX, leftTopY);
            keypoints.TopLeft = new Point(topLeftX, topLeftY);
            keypoints.TopRight = new Point(topRightX, topRightY);
            keypoints.RightTop = new Point(rightTopX, rightTopY);
            keypoints.RightBottom = new Point(rightBottomX, rightBottomY);
            keypoints.BottomRight = new Point(bottomRightX, bottomRightY);
            keypoints.BottomLeft = new Point(bottomLeftX, bottomLeftY);
            keypoints.LeftBottom = new Point(leftBottomX, leftBottomY);

            // Fix overlap
            if (keypoints.TopLeft.X > keypoints.TopRight.X)
            {
                var scaledX = topLeftX / (topLeftX + topRightX) * boundRect.Width;
                keypoints.TopLeft = new Point(scaledX, keypoints.TopLeft.Y);
                keypoints.TopRight = new Point(scaledX, keypoints.TopRight.Y);
            }

            if (keypoints.RightTop.Y > keypoints.RightBottom.Y)
            {
                var scaledY = rightBottomY / (rightTopY + rightBottomY) * boundRect.Height;
                keypoints.RightTop = new Point(keypoints.RightTop.X, scaledY);
                keypoints.RightBottom = new Point(keypoints.RightBottom.X, scaledY);
            }

            if (keypoints.BottomRight.X < keypoints.BottomLeft.X)
            {
                var scaledX = bottomLeftX / (bottomLeftX + bottomRightX) * boundRect.Width;
                keypoints.BottomRight = new Point(scaledX, keypoints.BottomRight.Y);
                keypoints.BottomLeft = new Point(scaledX, keypoints.BottomLeft.Y);
            }

            if (keypoints.LeftBottom.Y < keypoints.LeftTop.Y)
            {
                var scaledY = leftTopY / (leftTopY + leftBottomY) * boundRect.Height;
                keypoints.LeftBottom = new Point(keypoints.LeftBottom.X, scaledY);
                keypoints.LeftTop = new Point(keypoints.LeftTop.X, scaledY);
            }

            return keypoints;
        }

        public static RoundedRectKeypoints CalculateRoundedCornersRectangleV2(
            Rect boundRect,
            Thickness borderThickness,
            CornerRadius cornerRadius,
            BackgroundSizing sizing)
        {
            // This is a new implementation for Avalonia not based on any other code
            // It is the initial calculation algorithm supporting BackgroundSizing

            // Both the border thickness (offsets) and corner radius need to be adjust here
            // The borderThickenss is treating as offsets in the following code and those offsets
            // change whether the rounded rectangle is outside or inside the bounding box border.
            if (sizing == BackgroundSizing.InnerBorderEdge)
            {
                // borderThickness has no changes
            }
            else if (sizing == BackgroundSizing.OuterBorderEdge)
            {
                borderThickness = new Thickness(0);
            }
            else // CenterBorder
            {
                borderThickness = new Thickness(
                    0.5 * borderThickness.Left,
                    0.5 * borderThickness.Top,
                    0.5 * borderThickness.Right,
                    0.5 * borderThickness.Bottom);
            }

            /*
            topLeftRadiusX = (borderThickness.Left > cornerRadius.TopLeft ? 0 : cornerRadius.TopLeft);
            topLeftRadiusY = (borderThickness.Top > cornerRadius.TopLeft ? 0 : cornerRadius.TopLeft);
            topRightRadiusX = (borderThickness.Right > cornerRadius.TopRight ? 0 : cornerRadius.TopRight);
            topRightRadiusY = (borderThickness.Top > cornerRadius.TopRight ? 0 : cornerRadius.TopRight);
            bottomRightRadiusX = (borderThickness.Right > cornerRadius.BottomRight ? 0 : cornerRadius.BottomRight);
            bottomRightRadiusY = (borderThickness.Bottom > cornerRadius.BottomRight ? 0 : cornerRadius.BottomRight);
            bottomLeftRadiusX = (borderThickness.Left > cornerRadius.BottomLeft ? 0 : cornerRadius.BottomLeft);
            bottomLeftRadiusY = (borderThickness.Bottom > cornerRadius.BottomLeft ? 0 : cornerRadius.BottomLeft);
            */

            double topLeftRadiusX = cornerRadius.TopLeft;
            double topLeftRadiusY = cornerRadius.TopLeft;
            double topRightRadiusX = cornerRadius.TopRight;
            double topRightRadiusY = cornerRadius.TopRight;
            double bottomRightRadiusX = cornerRadius.BottomRight;
            double bottomRightRadiusY = cornerRadius.BottomRight;
            double bottomLeftRadiusX = cornerRadius.BottomLeft;
            double bottomLeftRadiusY = cornerRadius.BottomLeft;

            // Reduce the corner radius based on the thickness
            // Don't worry about keeping the radius circular here, it is adjusted later
            topLeftRadiusX = Math.Max(0, topLeftRadiusX - borderThickness.Left);
            topLeftRadiusY = Math.Max(0, topLeftRadiusY - borderThickness.Top);
            topRightRadiusX = Math.Max(0, topRightRadiusX - borderThickness.Right);
            topRightRadiusY = Math.Max(0, topRightRadiusY - borderThickness.Top);
            bottomRightRadiusX = Math.Max(0, bottomRightRadiusX - borderThickness.Right);
            bottomRightRadiusY = Math.Max(0, bottomRightRadiusY - borderThickness.Bottom);
            bottomLeftRadiusX = Math.Max(0, bottomLeftRadiusX - borderThickness.Left);
            bottomLeftRadiusY = Math.Max(0, bottomLeftRadiusY - borderThickness.Bottom);

            topLeftRadiusX = (borderThickness.Left > topLeftRadiusX ? 0 : topLeftRadiusX);
            topLeftRadiusY = (borderThickness.Top > topLeftRadiusY ? 0 : topLeftRadiusY);
            topRightRadiusX = (borderThickness.Right > topRightRadiusX ? 0 : topRightRadiusX);
            topRightRadiusY = (borderThickness.Top > topRightRadiusY ? 0 : topRightRadiusY);
            bottomRightRadiusX = (borderThickness.Right > bottomRightRadiusX ? 0 : bottomRightRadiusX);
            bottomRightRadiusY = (borderThickness.Bottom > bottomRightRadiusY ? 0 : bottomRightRadiusY);
            bottomLeftRadiusX = (borderThickness.Left > bottomLeftRadiusX ? 0 : bottomLeftRadiusX);
            bottomLeftRadiusY = (borderThickness.Bottom > bottomLeftRadiusY ? 0 : bottomLeftRadiusY);

            // Normalize corner radius
            // They should always be uniform in the X/Y directions as the original corner radius struct
            // does not support elliptical corners (only circular corners are expected)
            topLeftRadiusX = Math.Min(topLeftRadiusX, topLeftRadiusY);
            topLeftRadiusY = topLeftRadiusX;
            topRightRadiusX = Math.Min(topRightRadiusX, topRightRadiusY);
            topRightRadiusY = topRightRadiusX;
            bottomRightRadiusX = Math.Min(bottomRightRadiusX, bottomRightRadiusY);
            bottomRightRadiusY = bottomRightRadiusX;
            bottomLeftRadiusX = Math.Min(bottomLeftRadiusX, bottomLeftRadiusY);
            bottomLeftRadiusY = bottomLeftRadiusX;

            // All calculations are done starting from the TopLeft point in the bounding Rect
            // This is because using TopRight (for example) will create another point struct behind the scenes
            // That is undesirable for performance and unnecessary for these calculations
            var keypoints = new RoundedRectKeypoints();
            keypoints.LeftTop = new Point(
                boundRect.TopLeft.X + borderThickness.Left,
                boundRect.TopLeft.Y + (topLeftRadiusY + borderThickness.Top));
            keypoints.TopLeft = new Point(
                boundRect.TopLeft.X + (topLeftRadiusX + borderThickness.Left),
                boundRect.TopLeft.Y + borderThickness.Top);
            keypoints.TopRight = new Point(
                boundRect.TopLeft.X + boundRect.Width - (topRightRadiusX + borderThickness.Right),
                boundRect.TopLeft.Y + borderThickness.Top);
            keypoints.RightTop = new Point(
                boundRect.TopLeft.X + boundRect.Width - borderThickness.Right,
                boundRect.TopLeft.Y + (topRightRadiusY + borderThickness.Top));
            keypoints.RightBottom = new Point(
                boundRect.TopLeft.X + boundRect.Width - borderThickness.Right,
                boundRect.TopLeft.Y + boundRect.Height - (bottomRightRadiusY + borderThickness.Bottom));
            keypoints.BottomRight = new Point(
                boundRect.TopLeft.X + boundRect.Width - (bottomRightRadiusX + borderThickness.Right),
                boundRect.TopLeft.Y + boundRect.Height - borderThickness.Bottom);
            keypoints.BottomLeft = new Point(
                boundRect.TopLeft.X + (bottomLeftRadiusX + borderThickness.Left),
                boundRect.TopLeft.Y + boundRect.Height - borderThickness.Bottom);
            keypoints.LeftBottom = new Point(
                boundRect.TopLeft.X + borderThickness.Left,
                boundRect.TopLeft.Y + boundRect.Height - (bottomLeftRadiusY + borderThickness.Bottom));

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
