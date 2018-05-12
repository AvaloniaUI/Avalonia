// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Visuals.Effects;

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
                var innerCoordinates = new BorderCoordinates(cornerRadius, borderThickness, false);

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
                    var outerCoordinates = new BorderCoordinates(cornerRadius, borderThickness, true);
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

        public void Render(DrawingContext context, Size size, Thickness borders, CornerRadius radii, IBrush background, IBrush borderBrush, IEffect effect = null)
        {
            if (_useComplexRendering)
            {
                var backgroundGeometry = _backgroundGeometryCache;
                if (backgroundGeometry != null)
                {
                    context.DrawGeometry(background, null, backgroundGeometry, effect);
                }

                var borderGeometry = _borderGeometryCache;
                if (borderGeometry != null)
                {
                    context.DrawGeometry(borderBrush, null, borderGeometry, effect);
                }
            }
            else
            {
                var borderThickness = borders.Left;
                var cornerRadius = (float)radii.TopLeft;
                var rect = new Rect(size);

                if (background != null)
                {
                    context.FillRectangle(background, rect.Deflate(borders), cornerRadius, effect);
                }

                if (borderBrush != null && borderThickness > 0)
                {
                    context.DrawRectangle(new Pen(borderBrush, borderThickness), rect.Deflate(borderThickness), cornerRadius, effect);
                }
            }
        }

        private static void CreateGeometry(StreamGeometryContext context, Rect boundRect, BorderCoordinates borderCoordinates)
        {
            var topLeft = new Point(borderCoordinates.LeftTop, 0);
            var topRight = new Point(boundRect.Width - borderCoordinates.RightTop, 0);
            var rightTop = new Point(boundRect.Width, borderCoordinates.TopRight);
            var rightBottom = new Point(boundRect.Width, boundRect.Height - borderCoordinates.BottomRight);
            var bottomRight = new Point(boundRect.Width - borderCoordinates.RightBottom, boundRect.Height);
            var bottomLeft = new Point(borderCoordinates.LeftBottom, boundRect.Height);
            var leftBottom = new Point(0, boundRect.Height - borderCoordinates.BottomLeft);
            var leftTop = new Point(0, borderCoordinates.TopLeft);


            if (topLeft.X > topRight.X)
            {
                var scaledX = borderCoordinates.LeftTop / (borderCoordinates.LeftTop + borderCoordinates.RightTop) * boundRect.Width;
                topLeft = new Point(scaledX, topLeft.Y);
                topRight = new Point(scaledX, topRight.Y);
            }

            if (rightTop.Y > rightBottom.Y)
            {
                var scaledY = borderCoordinates.TopRight / (borderCoordinates.TopRight + borderCoordinates.BottomRight) * boundRect.Height;
                rightTop = new Point(rightTop.X, scaledY);
                rightBottom = new Point(rightBottom.X, scaledY);
            }

            if (bottomRight.X < bottomLeft.X)
            {
                var scaledX = borderCoordinates.LeftBottom / (borderCoordinates.LeftBottom + borderCoordinates.RightBottom) * boundRect.Width;
                bottomRight = new Point(scaledX, bottomRight.Y);
                bottomLeft = new Point(scaledX, bottomLeft.Y);
            }

            if (leftBottom.Y < leftTop.Y)
            {
                var scaledY = borderCoordinates.TopLeft / (borderCoordinates.TopLeft + borderCoordinates.BottomLeft) * boundRect.Height;
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

        private struct BorderCoordinates
        {
            internal BorderCoordinates(CornerRadius cornerRadius, Thickness borderThickness, bool isOuter)
            {
                var left = 0.5 * borderThickness.Left;
                var top = 0.5 * borderThickness.Top;
                var right = 0.5 * borderThickness.Right;
                var bottom = 0.5 * borderThickness.Bottom;

                if (isOuter)
                {
                    if (cornerRadius.TopLeft == 0)
                    {
                        LeftTop = TopLeft = 0.0;
                    }
                    else
                    {
                        LeftTop = cornerRadius.TopLeft + left;
                        TopLeft = cornerRadius.TopLeft + top;
                    }
                    if (cornerRadius.TopRight == 0)
                    {
                        TopRight = RightTop = 0;
                    }
                    else
                    {
                        TopRight = cornerRadius.TopRight + top;
                        RightTop = cornerRadius.TopRight + right;
                    }
                    if (cornerRadius.BottomRight == 0)
                    {
                        RightBottom = BottomRight = 0;
                    }
                    else
                    {
                        RightBottom = cornerRadius.BottomRight + right;
                        BottomRight = cornerRadius.BottomRight + bottom;
                    }
                    if (cornerRadius.BottomLeft == 0)
                    {
                        BottomLeft = LeftBottom = 0;
                    }
                    else
                    {
                        BottomLeft = cornerRadius.BottomLeft + bottom;
                        LeftBottom = cornerRadius.BottomLeft + left;
                    }
                }
                else
                {
                    LeftTop = Math.Max(0, cornerRadius.TopLeft - left);
                    TopLeft = Math.Max(0, cornerRadius.TopLeft - top);
                    TopRight = Math.Max(0, cornerRadius.TopRight - top);
                    RightTop = Math.Max(0, cornerRadius.TopRight - right);
                    RightBottom = Math.Max(0, cornerRadius.BottomRight - right);
                    BottomRight = Math.Max(0, cornerRadius.BottomRight - bottom);
                    BottomLeft = Math.Max(0, cornerRadius.BottomLeft - bottom);
                    LeftBottom = Math.Max(0, cornerRadius.BottomLeft - left);
                }
            }

            internal readonly double LeftTop;
            internal readonly double TopLeft;
            internal readonly double TopRight;
            internal readonly double RightTop;
            internal readonly double RightBottom;
            internal readonly double BottomRight;
            internal readonly double BottomLeft;
            internal readonly double LeftBottom;
        }

    }
}
