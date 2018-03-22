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
                    var innerRect = new Rect(borderThickness.Left, borderThickness.Top,
                        boundRect.Width - borderThickness.Right, boundRect.Height - borderThickness.Bottom);

                    StreamGeometry backgroundGeometry = null;

                    if (!boundRect.Width.Equals(0) && !boundRect.Height.Equals(0))
                    {
                        backgroundGeometry = new StreamGeometry();

                        if (cornerRadius.IsEmpty)
                        {
                            using (var ctx = backgroundGeometry.Open())
                            {
                                CreateGeometry(ctx, innerRect, cornerRadius);
                            }
                        }
                        else
                        {
                            using (var ctx = backgroundGeometry.Open())
                            {
                                CreateGeometry(ctx, innerRect, cornerRadius);
                            }
                        }

                        _backgroundGeometryCache = backgroundGeometry;
                    }
                    else
                    {
                        _backgroundGeometryCache = null;
                    }

                    if (!boundRect.Width.Equals(0) && !boundRect.Height.Equals(0))
                    {
                        var borderGeometry = new StreamGeometry();

                        if (cornerRadius.IsEmpty)
                        {
                            using (var ctx = borderGeometry.Open())
                            {
                                CreateGeometry(ctx, boundRect, cornerRadius);

                                if (backgroundGeometry != null)
                                {
                                    CreateGeometry(ctx, innerRect, cornerRadius);
                                }
                            }
                        }
                        else
                        {
                            using (var ctx = borderGeometry.Open())
                            {
                                CreateGeometry(ctx, boundRect, cornerRadius);

                                if (backgroundGeometry != null)
                                {
                                    CreateGeometry(ctx, innerRect, cornerRadius);
                                }

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

            private static void CreateGeometry(StreamGeometryContext context, Rect boundRect, CornerRadius cornerRadius)
            {
                var topLeft = new Point(boundRect.X + cornerRadius.TopLeft, boundRect.Y);
                var topRight = new Point(boundRect.Width - cornerRadius.TopRight, boundRect.Y);
                var rightTop = new Point(boundRect.Width, boundRect.Y + cornerRadius.TopRight);
                var rightBottom = new Point(boundRect.Width, boundRect.Height - cornerRadius.BottomRight);
                var bottomRight = new Point(boundRect.Width - cornerRadius.BottomRight, boundRect.Height);
                var bottomLeft = new Point(boundRect.X + cornerRadius.BottomLeft, boundRect.Height);
                var leftBottom = new Point(boundRect.X, boundRect.Height - cornerRadius.BottomLeft);
                var leftTop = new Point(boundRect.X, boundRect.Y + cornerRadius.TopLeft);

                context.BeginFigure(topLeft, true);

                //Top
                context.LineTo(topRight);

                //TopRight corner
                if (topRight != rightTop)
                {
                    context.ArcTo(rightTop, new Size(cornerRadius.TopRight, cornerRadius.TopRight), 0, false, SweepDirection.Clockwise);
                }

                //Right
                context.LineTo(rightBottom);

                //BottomRight corner
                if (rightBottom != bottomRight)
                {
                    context.ArcTo(bottomRight, new Size(cornerRadius.BottomRight, cornerRadius.BottomRight), 0, false, SweepDirection.Clockwise);
                }

                //Bottom
                context.LineTo(bottomLeft);

                //BottomLeft corner
                if (bottomLeft != leftBottom)
                {
                    context.ArcTo(leftBottom, new Size(cornerRadius.BottomLeft, cornerRadius.BottomLeft), 0, false, SweepDirection.Clockwise);
                }

                //Left
                context.LineTo(leftTop);

                //TopLeft corner
                if (leftTop != topLeft)
                {
                    context.ArcTo(topLeft, new Size(cornerRadius.TopLeft, cornerRadius.TopLeft), 0, false, SweepDirection.Clockwise);
                }

                context.EndFigure(true);
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
        }
    }
}