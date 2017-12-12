// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public static readonly StyledProperty<float> CornerRadiusProperty =
            AvaloniaProperty.Register<Border, float>(nameof(CornerRadius));

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
        public float CornerRadius
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
            var background = Background;
            var borderBrush = BorderBrush;
            var borderThickness = BorderThickness;
            var cornerRadius = CornerRadius;
            var rect = new Rect(Bounds.Size).Deflate(BorderThickness);

            if (background != null)
            {
                context.FillRectangle(Brushes.Transparent, rect);
            }

            if (borderBrush != null)
            {
                var pen = new Pen(borderBrush, 4);
                var resolution = 5.0;
                

                Point pt(double x, double y)
                {
                    return new Point(x, y);
                }

                void DrawSector(Point pos, double radius, double degStart, double degEnd, double fromSize, double toSize)
                {
                    var points = new List<Point>();
                    var degSteps = ((degEnd - degStart) / resolution);
                    var sizeStep = (toSize - fromSize) / degSteps/2.0;
                    var curStep = fromSize / 2.0;

                    for (double i = degStart; i <= degEnd; i += resolution)//outer radius
                    {
                        points.Add(new Point(pos.X + Math.Cos(i / 180.0 * Math.PI) * (radius+curStep), pos.Y + (-Math.Sin(i / 180.0 * Math.PI)) * (radius+curStep)));
                        curStep += sizeStep;
                    }

                    var geom = new PolylineGeometry(points, true);//fill
                    geom.Points.Add(pos);
                    context.DrawGeometry(Background, new Pen(Brushes.Transparent), geom);

                    for (double i = degEnd; i >= degStart; i -= resolution)//inner radius
                    {
                        curStep -= sizeStep;
                        points.Add(new Point(pos.X + Math.Cos(i / 180.0 * Math.PI) * (radius - curStep), pos.Y + (-Math.Sin(i / 180.0 * Math.PI)) * (radius - curStep)));
                    }
                    geom = new PolylineGeometry(points, true);//draw border
                    context.DrawGeometry(borderBrush, new Pen(Brushes.Transparent), geom);

                }

                var centralRectangle = new Rect(pt(rect.X + cornerRadius, rect.Y), new Size(rect.Width - (2 * cornerRadius), rect.Height));
                var leftRect = new Rect(pt(rect.X, rect.Y+cornerRadius), new Size(cornerRadius, rect.Height-(2*cornerRadius)));
                var rightRect = new Rect(pt(rect.X+rect.Width-cornerRadius, rect.Y + cornerRadius), new Size(cornerRadius, rect.Height - (2 * cornerRadius)));

                context.FillRectangle(background, leftRect);
                context.FillRectangle(background, rightRect);
                context.FillRectangle(background, centralRectangle);

                DrawSector(pt(rect.X+cornerRadius, rect.Y+cornerRadius), cornerRadius, 90,180, BorderThickness.Top, borderThickness.Left);
                context.DrawLine(new Pen(borderBrush, BorderThickness.Left), pt(rect.X, rect.Y + cornerRadius), pt(rect.X, rect.Y + rect.Height - cornerRadius));
                DrawSector(pt(rect.X + cornerRadius, rect.Y + rect.Height - cornerRadius), cornerRadius, 180, 270, borderThickness.Left, borderThickness.Bottom);
                context.DrawLine(new Pen(borderBrush, BorderThickness.Bottom), pt(rect.X+cornerRadius, rect.Y + rect.Height), pt(rect.X+rect.Width-cornerRadius, rect.Y + rect.Height));
                DrawSector(pt(rect.X +rect.Width - cornerRadius, rect.Y + rect.Height - cornerRadius), cornerRadius, 270, 360, borderThickness.Bottom, borderThickness.Right);
                context.DrawLine(new Pen(borderBrush, BorderThickness.Right), pt(rect.X +rect.Width, rect.Y + rect.Height-cornerRadius), pt(rect.X + rect.Width, rect.Y+cornerRadius));
                DrawSector(pt(rect.X + rect.Width - cornerRadius, rect.Y +  cornerRadius), cornerRadius, 0, 90, borderThickness.Right, borderThickness.Top);
                context.DrawLine(new Pen(borderBrush, BorderThickness.Top), pt(rect.X + cornerRadius, rect.Y), pt(rect.X + rect.Width - cornerRadius, rect.Y));
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;
            var padding = Padding + BorderThickness;

            if (child != null)
            {
                child.Measure(availableSize.Deflate(padding));
                return child.DesiredSize.Inflate(padding);
            }
            else
            {
                return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
            }
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;

            if (child != null)
            {
                var padding = Padding + BorderThickness;
                child.Arrange(new Rect(finalSize).Deflate(padding));
            }

            return finalSize;
        }
    }
}