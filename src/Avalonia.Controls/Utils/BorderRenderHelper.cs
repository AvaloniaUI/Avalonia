// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Controls.Utils
{
    internal class BorderRenderHelper
    {
        private bool _useComplexRendering;
        private StreamGeometry _backgroundGeometryCache;
        private StreamGeometry _borderGeometryCache;

        public void Update(Size finalSize, Thickness borderThickness, CornerRadius cornerRadius)
        {
            if (borderThickness.IsUniform && cornerRadius.IsUniform)
            {
                _backgroundGeometryCache = null;
                _borderGeometryCache = null;
                _useComplexRendering = false;
            }
            else
            {
                _useComplexRendering = true;

                var boundRect = new Rect(finalSize);
                var innerRect = boundRect.Deflate(borderThickness);
                var innerCoordinates = GeometryCoordinates.CreateBackgroundCoordinates(cornerRadius, borderThickness);

                StreamGeometry backgroundGeometry = null;

                if (innerRect.Width != 0 && innerRect.Height != 0)
                {
                    backgroundGeometry = new StreamGeometry();

                    using (var ctx = backgroundGeometry.Open())
                    {
                        CreateGeometry(ctx, innerRect, innerCoordinates);
                    }

                    _backgroundGeometryCache = backgroundGeometry;
                }
                else
                {
                    _backgroundGeometryCache = null;
                }

                if (boundRect.Width != 0 && innerRect.Height != 0)
                {
                    var outerCoordinates = GeometryCoordinates.CreateBorderCoordinates(cornerRadius, borderThickness);
                    var borderGeometry = new StreamGeometry();

                    using (var ctx = borderGeometry.Open())
                    {
                        CreateGeometry(ctx, boundRect, outerCoordinates);

                        if (backgroundGeometry != null)
                        {
                            CreateGeometry(ctx, innerRect, innerCoordinates);
                        }
                    }

                    _borderGeometryCache = borderGeometry;
                }
                else
                {
                    _borderGeometryCache = null;
                }
            }
        }

        public void Render(DrawingContext context, Size size, Thickness borders, CornerRadius radii, IBrush background, IBrush borderBrush)
        {
            if (_useComplexRendering)
            {
                var backgroundGeometry = _backgroundGeometryCache;
                if (backgroundGeometry != null)
                {
                    context.DrawGeometry(background, null, backgroundGeometry);
                }

                var borderGeometry = _borderGeometryCache;
                if (borderGeometry != null)
                {
                    context.DrawGeometry(borderBrush, null, borderGeometry);
                }
            }
            else
            {
                var borderThickness = borders.Left;
                var cornerRadius = (float)radii.TopLeft;
                var rect = new Rect(size);

                if (background != null)
                {
                    context.FillRectangle(background, rect.Deflate(borders), cornerRadius);
                }

                if (borderBrush != null && borderThickness > 0)
                {
                    context.DrawRectangle(new Pen(borderBrush, borderThickness), rect.Deflate(borderThickness), cornerRadius);
                }
            }
        }

        private static void CreateGeometry(StreamGeometryContext context, Rect boundRect, GeometryCoordinates geometryCoordinates)
        {
            var topLeft = new Point(geometryCoordinates.LeftTop, 0);
            var topRight = new Point(boundRect.Width - geometryCoordinates.RightTop, 0);
            var rightTop = new Point(boundRect.Width, geometryCoordinates.TopRight);
            var rightBottom = new Point(boundRect.Width, boundRect.Height - geometryCoordinates.BottomRight);
            var bottomRight = new Point(boundRect.Width - geometryCoordinates.RightBottom, boundRect.Height);
            var bottomLeft = new Point(geometryCoordinates.LeftBottom, boundRect.Height);
            var leftBottom = new Point(0, boundRect.Height - geometryCoordinates.BottomLeft);
            var leftTop = new Point(0, geometryCoordinates.TopLeft);

            if (topLeft.X > topRight.X)
            {
                var scaledX = geometryCoordinates.LeftTop / (geometryCoordinates.LeftTop + geometryCoordinates.RightTop) * boundRect.Width;
                topLeft = new Point(scaledX, topLeft.Y);
                topRight = new Point(scaledX, topRight.Y);
            }

            if (rightTop.Y > rightBottom.Y)
            {
                var scaledY = geometryCoordinates.TopRight / (geometryCoordinates.TopRight + geometryCoordinates.BottomRight) * boundRect.Height;
                rightTop = new Point(rightTop.X, scaledY);
                rightBottom = new Point(rightBottom.X, scaledY);
            }

            if (bottomRight.X < bottomLeft.X)
            {
                var scaledX = geometryCoordinates.LeftBottom / (geometryCoordinates.LeftBottom + geometryCoordinates.RightBottom) * boundRect.Width;
                bottomRight = new Point(scaledX, bottomRight.Y);
                bottomLeft = new Point(scaledX, bottomLeft.Y);
            }

            if (leftBottom.Y < leftTop.Y)
            {
                var scaledY = geometryCoordinates.TopLeft / (geometryCoordinates.TopLeft + geometryCoordinates.BottomLeft) * boundRect.Height;
                leftBottom = new Point(leftBottom.X, scaledY);
                leftTop = new Point(leftTop.X, scaledY);
            }

            var offset = new Vector(boundRect.TopLeft.X, boundRect.TopLeft.Y);
            topLeft += offset;
            topRight += offset;
            rightTop += offset;
            rightBottom += offset;
            bottomRight += offset;
            bottomLeft += offset;
            leftBottom += offset;
            leftTop += offset;

            context.BeginFigure(topLeft, true);

            //Top
            context.LineTo(topRight);

            //TopRight corner
            var radiusX = boundRect.TopRight.X - topRight.X;
            var radiusY = rightTop.Y - boundRect.TopRight.Y;
            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(rightTop, new Size(radiusY, radiusY), 0, false, SweepDirection.Clockwise);
            }

            //Right
            context.LineTo(rightBottom);

            //BottomRight corner
            radiusX = boundRect.BottomRight.X - bottomRight.X;
            radiusY = boundRect.BottomRight.Y - rightBottom.Y;
            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(bottomRight, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
            }

            //Bottom
            context.LineTo(bottomLeft);

            //BottomLeft corner
            radiusX = bottomLeft.X - boundRect.BottomLeft.X;
            radiusY = boundRect.BottomLeft.Y - leftBottom.Y;
            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(leftBottom, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
            }

            //Left
            context.LineTo(leftTop);

            //TopLeft corner
            radiusX = topLeft.X - boundRect.TopLeft.X;
            radiusY = leftTop.Y - boundRect.TopLeft.Y;

            if (radiusX != 0 || radiusY != 0)
            {
                context.ArcTo(topLeft, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
            }

            context.EndFigure(true);
        }

        private struct GeometryCoordinates
        {
            internal static GeometryCoordinates CreateBorderCoordinates(CornerRadius cornerRadius, Thickness borderThickness)
            {
                var left = 0.5 * borderThickness.Left;
                var top = 0.5 * borderThickness.Top;
                var right = 0.5 * borderThickness.Right;
                var bottom = 0.5 * borderThickness.Bottom;

                var leftTop = 0.0;
                var topLeft = 0.0;
                if (cornerRadius.TopLeft != 0)
                {
                    leftTop = cornerRadius.TopLeft + left;
                    topLeft = cornerRadius.TopLeft + top;
                }

                var topRight = 0.0;
                var rightTop = 0.0;
                if (cornerRadius.TopRight != 0)
                {
                    topRight = cornerRadius.TopRight + top;
                    rightTop = cornerRadius.TopRight + right;
                }

                var rightBottom = 0.0;
                var bottomRight = 0.0;
                if (cornerRadius.BottomRight != 0)
                {
                    rightBottom = cornerRadius.BottomRight + right;
                    bottomRight = cornerRadius.BottomRight + bottom;
                }

                var bottomLeft = 0.0;
                var leftBottom = 0.0;
                if (cornerRadius.BottomLeft != 0)
                {
                    bottomLeft = cornerRadius.BottomLeft + bottom;
                    leftBottom = cornerRadius.BottomLeft + left;
                }

                return new GeometryCoordinates
                {
                    LeftTop = leftTop,
                    TopLeft = topLeft,
                    TopRight = topRight,
                    RightTop = rightTop,
                    RightBottom = rightBottom,
                    BottomRight = bottomRight,
                    BottomLeft = bottomLeft,
                    LeftBottom = leftBottom,
                };
            }

            internal static GeometryCoordinates CreateBackgroundCoordinates(CornerRadius cornerRadius, Thickness borderThickness)
            {
                var left = 0.5 * borderThickness.Left;
                var top = 0.5 * borderThickness.Top;
                var right = 0.5 * borderThickness.Right;
                var bottom = 0.5 * borderThickness.Bottom;

                return new GeometryCoordinates
                {
                    LeftTop = Math.Max(0, cornerRadius.TopLeft - left),
                    TopLeft = Math.Max(0, cornerRadius.TopLeft - top),
                    TopRight = Math.Max(0, cornerRadius.TopRight - top),
                    RightTop = Math.Max(0, cornerRadius.TopRight - right),
                    RightBottom = Math.Max(0, cornerRadius.BottomRight - right),
                    BottomRight = Math.Max(0, cornerRadius.BottomRight - bottom),
                    BottomLeft = Math.Max(0, cornerRadius.BottomLeft - bottom),
                    LeftBottom = Math.Max(0, cornerRadius.BottomLeft - left),
                };
            }

            internal double LeftTop { get; private set; }
            internal double TopLeft { get; private set; }
            internal double TopRight { get; private set; }
            internal double RightTop { get; private set; }
            internal double RightBottom { get; private set; }
            internal double BottomRight { get; private set; }
            internal double BottomLeft { get; private set; }
            internal double LeftBottom { get; private set; }
        }

    }
}
