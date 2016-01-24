// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public abstract class Shape : Control
    {
        public static readonly PerspexProperty<Brush> FillProperty =
            PerspexProperty.Register<Shape, Brush>("Fill");

        public static readonly PerspexProperty<Stretch> StretchProperty =
            PerspexProperty.Register<Shape, Stretch>("Stretch");

        public static readonly PerspexProperty<Brush> StrokeProperty =
            PerspexProperty.Register<Shape, Brush>("Stroke");

        public static readonly PerspexProperty<PerspexList<double>> StrokeDashArrayProperty =
            PerspexProperty.Register<Shape, PerspexList<double>>("StrokeDashArray");

        public static readonly PerspexProperty<double> StrokeThicknessProperty =
            PerspexProperty.Register<Shape, double>("StrokeThickness");

        private Matrix _transform = Matrix.Identity;

        private Geometry _renderedGeometry;

        static Shape()
        {
            AffectsRender(FillProperty);
            AffectsMeasure(StretchProperty);
            AffectsRender(StrokeProperty);
            AffectsRender(StrokeDashArrayProperty);
            AffectsMeasure(StrokeThicknessProperty);
        }

        public abstract Geometry DefiningGeometry
        {
            get;
        }

        public Brush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public Geometry RenderedGeometry
        {
            get
            {
                if (_renderedGeometry == null)
                {
                    if (DefiningGeometry != null)
                    {
                        _renderedGeometry = DefiningGeometry.Clone();
                        _renderedGeometry.Transform = new MatrixTransform(_transform);
                    }
                }

                return _renderedGeometry;
            }
        }

        public Stretch Stretch
        {
            get { return GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public Brush Stroke
        {
            get { return GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public PerspexList<double> StrokeDashArray
        {
            get { return GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        public double StrokeThickness
        {
            get { return GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public PenLineCap StrokeDashCap { get; set; } = PenLineCap.Flat;

        public PenLineCap StrokeStartLineCap { get; set; } = PenLineCap.Flat;

        public PenLineCap StrokeEndLineCap { get; set; } = PenLineCap.Flat;

        public override void Render(DrawingContext context)
        {
            var geometry = RenderedGeometry;

            if (geometry != null)
            {
                var pen = new Pen(Stroke, StrokeThickness, new DashStyle(StrokeDashArray), 
                    StrokeDashCap, StrokeStartLineCap, StrokeEndLineCap);
                context.DrawGeometry(Fill, pen, geometry);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // This should probably use GetRenderBounds(strokeThickness) but then the calculations
            // will multiply the stroke thickness as well, which isn't correct.
            Rect shapeBounds = DefiningGeometry.Bounds;
            Size shapeSize = new Size(shapeBounds.Right, shapeBounds.Bottom);
            Matrix translate = Matrix.Identity;
            double width = Width;
            double height = Height;
            double desiredX = availableSize.Width;
            double desiredY = availableSize.Height;
            double sx = 0.0;
            double sy = 0.0;

            if (Stretch != Stretch.None)
            {
                shapeSize = shapeBounds.Size;
                translate = Matrix.CreateTranslation(-(Vector)shapeBounds.Position);
            }

            if (double.IsInfinity(availableSize.Width))
            {
                desiredX = shapeSize.Width;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                desiredY = shapeSize.Height;
            }

            if (shapeBounds.Width > 0)
            {
                sx = desiredX / shapeSize.Width;
            }

            if (shapeBounds.Height > 0)
            {
                sy = desiredY / shapeSize.Height;
            }

            if (double.IsInfinity(availableSize.Width))
            {
                sx = sy;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                sy = sx;
            }

            switch (Stretch)
            {
                case Stretch.Uniform:
                    sx = sy = Math.Min(sx, sy);
                    break;
                case Stretch.UniformToFill:
                    sx = sy = Math.Max(sx, sy);
                    break;
                case Stretch.Fill:
                    if (double.IsInfinity(availableSize.Width))
                    {
                        sx = 1.0;
                    }

                    if (double.IsInfinity(availableSize.Height))
                    {
                        sy = 1.0;
                    }

                    break;
                default:
                    sx = sy = 1;
                    break;
            }

            var t = translate * Matrix.CreateScale(sx, sy);

            if (_transform != t)
            {
                _transform = t;
                _renderedGeometry = null;
            }

            return new Size(shapeSize.Width * sx, shapeSize.Height * sy);
        }
    }
}
