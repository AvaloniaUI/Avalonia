// -----------------------------------------------------------------------
// <copyright file="Shape.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Shapes
{
    using System;
    using Perspex.Controls;
    using Perspex.Media;

    public abstract class Shape : Control
    {
        public static readonly PerspexProperty<Brush> FillProperty =
            PerspexProperty.Register<Shape, Brush>("Fill");

        public static readonly PerspexProperty<Stretch> StretchProperty =
            PerspexProperty.Register<Shape, Stretch>("Stretch");

        public static readonly PerspexProperty<Brush> StrokeProperty =
            PerspexProperty.Register<Shape, Brush>("Stroke");

        public static readonly PerspexProperty<double> StrokeThicknessProperty =
            PerspexProperty.Register<Shape, double>("StrokeThickness");

        private Matrix transform = Matrix.Identity;

        public abstract Geometry DefiningGeometry
        {
            get;
        }

        public Brush Fill
        {
            get { return this.GetValue(FillProperty); }
            set { this.SetValue(FillProperty, value); }
        }

        public Geometry RenderedGeometry
        {
            get
            {
                var result = this.DefiningGeometry;

                if (result != null)
                {
                    result = result.Clone();
                    result.Transform = new MatrixTransform(this.transform);
                }

                return result;
            }
        }

        public Stretch Stretch
        {
            get { return this.GetValue(StretchProperty); }
            set { this.SetValue(StretchProperty, value); }
        }

        public Brush Stroke
        {
            get { return this.GetValue(StrokeProperty); }
            set { this.SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return this.GetValue(StrokeThicknessProperty); }
            set { this.SetValue(StrokeThicknessProperty, value); }
        }

        public override void Render(IDrawingContext context)
        {
            var geometry = this.RenderedGeometry;

            if (geometry != null)
            {
                context.DrawGeometry(this.Fill, new Pen(this.Stroke, this.StrokeThickness), geometry);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Rect shapeBounds = this.DefiningGeometry.GetRenderBounds(this.StrokeThickness);
            Size shapeSize = new Size(shapeBounds.Right, shapeBounds.Bottom);
            Matrix translate = Matrix.Identity;
            double width = this.Width;
            double height = this.Height;
            double desiredX = availableSize.Width;
            double desiredY = availableSize.Height;
            double sx = 0.0;
            double sy = 0.0;

            if (this.Stretch != Stretch.None)
            {
                shapeSize = shapeBounds.Size;
                translate = Matrix.Translation(-(Vector)shapeBounds.Position);
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
                sx = desiredX / shapeBounds.Width;
            }

            if (shapeBounds.Height > 0)
            {
                sy = desiredY / shapeBounds.Height;
            }

            if (double.IsInfinity(availableSize.Width))
            {
                sx = sy;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                sy = sx;
            }

            switch (this.Stretch)
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

            this.transform = translate * Matrix.Scaling(sx, sy);

            double finalX = (width > 0) ? width : shapeSize.Width * sx;
            double finalY = (height > 0) ? height : shapeSize.Width * sy;
            return new Size(finalX, finalY);
        }
    }
}
