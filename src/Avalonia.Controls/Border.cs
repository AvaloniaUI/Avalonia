// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control which decorates a child with a border and background.
    /// </summary>
    public class Border : Decorator
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<Border, IBrush>(nameof(Background));

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BorderBrushProperty =
            AvaloniaProperty.Register<Border, IBrush>(nameof(BorderBrush));

        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            AvaloniaProperty.Register<Border, Thickness>(nameof(BorderThickness));

        /// <summary>
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<Border, CornerRadius>(nameof(CornerRadius));

        private readonly BorderRenderer _borderRenderer = new BorderRenderer();

        /// <summary>
        /// Initializes static members of the <see cref="Border"/> class.
        /// </summary>
        static Border()
        {
            AffectsRender(BackgroundProperty, BorderBrushProperty);
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public IBrush BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            _borderRenderer.Render(context, Bounds.Size, BorderThickness, CornerRadius, Background, BorderBrush);
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return MeasureOverrideImpl(availableSize, Child, Padding, BorderThickness);
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child != null)
            {
                var padding = Padding + BorderThickness;
                Child.Arrange(new Rect(finalSize).Deflate(padding));
            }

            _borderRenderer.Update(finalSize, BorderThickness, CornerRadius);

            return finalSize;
        }

        internal static Size MeasureOverrideImpl(
            Size availableSize,
            IControl child,
            Thickness padding,
            Thickness borderThickness)
        {
            padding += borderThickness;

            if (child != null)
            {
                child.Measure(availableSize.Deflate(padding));
                return child.DesiredSize.Inflate(padding);
            }

            return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
        }


        internal class BorderRenderer
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
                    var innerRadii = new Radii(cornerRadius, borderThickness, false);

                    StreamGeometry backgroundGeometry = null;

                    //  calculate border / background rendering geometry
                    if (!innerRect.Width.Equals(0) && !innerRect.Height.Equals(0))
                    {
                        backgroundGeometry = new StreamGeometry();

                        using (var ctx = backgroundGeometry.Open())
                        {
                            GenerateGeometry(ctx, innerRect, innerRadii);
                        }

                        _backgroundGeometryCache = backgroundGeometry;
                    }
                    else
                    {
                        _backgroundGeometryCache = null;
                    }

                    if (!boundRect.Width.Equals(0) && !boundRect.Height.Equals(0))
                    {
                        var outerRadii = new Radii(cornerRadius, borderThickness, true);
                        var borderGeometry = new StreamGeometry();

                        using (var ctx = borderGeometry.Open())
                        {
                            GenerateGeometry(ctx, boundRect, outerRadii);

                            if (backgroundGeometry != null)
                            {
                                GenerateGeometry(ctx, innerRect, innerRadii);
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
                    IBrush brush;
                    var borderGeometry = _borderGeometryCache;
                    if (borderGeometry != null && (brush = borderBrush) != null)
                    {
                        context.DrawGeometry(brush, null, borderGeometry);
                    }

                    var backgroundGeometry = _backgroundGeometryCache;
                    if (backgroundGeometry != null && (brush = background) != null)
                    {
                        context.DrawGeometry(brush, null, backgroundGeometry);
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

            private static void GenerateGeometry(StreamGeometryContext ctx, Rect rect, Radii radii)
            {
                //
                //  Compute the coordinates of the key points
                //

                var topLeft = new Point(radii.LeftTop, 0);
                var topRight = new Point(rect.Width - radii.RightTop, 0);
                var rightTop = new Point(rect.Width, radii.TopRight);
                var rightBottom = new Point(rect.Width, rect.Height - radii.BottomRight);
                var bottomRight = new Point(rect.Width - radii.RightBottom, rect.Height);
                var bottomLeft = new Point(radii.LeftBottom, rect.Height);
                var leftBottom = new Point(0, rect.Height - radii.BottomLeft);
                var leftTop = new Point(0, radii.TopLeft);

                //
                //  Check keypoints for overlap and resolve by partitioning radii according to
                //  the percentage of each one.  
                //

                //  Top edge is handled here
                if (topLeft.X > topRight.X)
                {
                    var x = radii.LeftTop / (radii.LeftTop + radii.RightTop) * rect.Width;
                    topLeft += new Point(x, 0);
                    topRight += new Point(x, 0);
                }

                //  Right edge
                if (rightTop.Y > rightBottom.Y)
                {
                    var y = radii.TopRight / (radii.TopRight + radii.BottomRight) * rect.Height;
                    rightTop += new Point(0, y);
                    rightBottom += new Point(0, y);
                }

                //  Bottom edge
                if (bottomRight.X < bottomLeft.X)
                {
                    var x = radii.LeftBottom / (radii.LeftBottom + radii.RightBottom) * rect.Width;
                    bottomRight += new Point(x, 0);
                    bottomLeft += new Point(x, 0);
                }

                // Left edge
                if (leftBottom.Y < leftTop.Y)
                {
                    var y = radii.TopLeft / (radii.TopLeft + radii.BottomLeft) * rect.Height;
                    leftBottom += new Point(0, y);
                    leftTop += new Point(0, y);
                }

                //
                //  Add on offsets
                //

                var offset = new Vector(rect.TopLeft.X, rect.TopLeft.Y);
                topLeft += offset;
                topRight += offset;
                rightTop += offset;
                rightBottom += offset;
                bottomRight += offset;
                bottomLeft += offset;
                leftBottom += offset;
                leftTop += offset;

                //
                //  Create the border geometry
                //
                ctx.BeginFigure(topLeft, true);

                // Top
                ctx.LineTo(topRight);

                // TopRight
                var radiusX = rect.TopRight.X - topRight.X;
                var radiusY = rightTop.Y - rect.TopRight.Y;
                if (!radiusX.Equals(0) || !radiusY.Equals(0))
                {
                    ctx.ArcTo(rightTop, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
                }

                // Right
                ctx.LineTo(rightBottom);

                // BottomRight
                radiusX = rect.BottomRight.X - bottomRight.X;
                radiusY = rect.BottomRight.Y - rightBottom.Y;
                if (!radiusX.Equals(0) || !radiusY.Equals(0))
                {
                    ctx.ArcTo(bottomRight, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
                }

                // Bottom
                ctx.LineTo(bottomLeft);

                // BottomLeft
                radiusX = bottomLeft.X - rect.BottomLeft.X;
                radiusY = rect.BottomLeft.Y - leftBottom.Y;
                if (!radiusX.Equals(0) || !radiusY.Equals(0))
                {
                    ctx.ArcTo(leftBottom, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
                }

                // Left
                ctx.LineTo(leftTop);

                // TopLeft
                radiusX = topLeft.X - rect.TopLeft.X;
                radiusY = leftTop.Y - rect.TopLeft.Y;
                if (!radiusX.Equals(0) || !radiusY.Equals(0))
                {
                    ctx.ArcTo(topLeft, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise);
                }

                ctx.EndFigure(true);
            }

            private struct Radii
            {
                internal Radii(CornerRadius radii, Thickness borders, bool outer)
                {
                    var left = 0.5 * borders.Left;
                    var top = 0.5 * borders.Top;
                    var right = 0.5 * borders.Right;
                    var bottom = 0.5 * borders.Bottom;

                    if (outer)
                    {
                        if (radii.TopLeft.Equals(0))
                        {
                            LeftTop = TopLeft = 0.0;
                        }
                        else
                        {
                            LeftTop = radii.TopLeft + left;
                            TopLeft = radii.TopLeft + top;
                        }
                        if (radii.TopRight.Equals(0))
                        {
                            TopRight = RightTop = 0.0;
                        }
                        else
                        {
                            TopRight = radii.TopRight + top;
                            RightTop = radii.TopRight + right;
                        }
                        if (radii.BottomRight.Equals(0))
                        {
                            RightBottom = BottomRight = 0.0;
                        }
                        else
                        {
                            RightBottom = radii.BottomRight + right;
                            BottomRight = radii.BottomRight + bottom;
                        }
                        if (radii.BottomLeft.Equals(0))
                        {
                            BottomLeft = LeftBottom = 0.0;
                        }
                        else
                        {
                            BottomLeft = radii.BottomLeft + bottom;
                            LeftBottom = radii.BottomLeft + left;
                        }
                    }
                    else
                    {
                        LeftTop = Math.Max(0.0, radii.TopLeft - left);
                        TopLeft = Math.Max(0.0, radii.TopLeft - top);
                        TopRight = Math.Max(0.0, radii.TopRight - top);
                        RightTop = Math.Max(0.0, radii.TopRight - right);
                        RightBottom = Math.Max(0.0, radii.BottomRight - right);
                        BottomRight = Math.Max(0.0, radii.BottomRight - bottom);
                        BottomLeft = Math.Max(0.0, radii.BottomLeft - bottom);
                        LeftBottom = Math.Max(0.0, radii.BottomLeft - left);
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
}