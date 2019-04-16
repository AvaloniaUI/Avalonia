// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public abstract class Shape : Control
    {
        public static readonly StyledProperty<IBrush> FillProperty =
            AvaloniaProperty.Register<Shape, IBrush>(nameof(Fill));

        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Shape, Stretch>(nameof(Stretch));

        public static readonly StyledProperty<IBrush> StrokeProperty =
            AvaloniaProperty.Register<Shape, IBrush>(nameof(Stroke));

        public static readonly StyledProperty<AvaloniaList<double>> StrokeDashArrayProperty =
            AvaloniaProperty.Register<Shape, AvaloniaList<double>>(nameof(StrokeDashArray));

        public static readonly StyledProperty<double> StrokeDashOffsetProperty =
            AvaloniaProperty.Register<Shape, double>(nameof(StrokeDashOffset));

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<Shape, double>(nameof(StrokeThickness));

        public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
            AvaloniaProperty.Register<Shape, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Flat);

        public static readonly StyledProperty<PenLineJoin> StrokeJoinProperty =
            AvaloniaProperty.Register<Shape, PenLineJoin>(nameof(StrokeJoin), PenLineJoin.Miter);

        private Matrix _transform = Matrix.Identity;
        private Geometry _definingGeometry;
        private Geometry _renderedGeometry;
        bool _calculateTransformOnArrange = false;

        static Shape()
        {
            AffectsMeasure<Shape>(StretchProperty, StrokeThicknessProperty);

            AffectsRender<Shape>(FillProperty, StrokeProperty, StrokeDashArrayProperty, StrokeDashOffsetProperty,
                StrokeThicknessProperty, StrokeLineCapProperty, StrokeJoinProperty);
        }

        public Geometry DefiningGeometry
        {
            get
            {
                if (_definingGeometry == null)
                {
                    _definingGeometry = CreateDefiningGeometry();
                }

                return _definingGeometry;
            }
        }

        public IBrush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public Geometry RenderedGeometry
        {
            get
            {
                if (_renderedGeometry == null && DefiningGeometry != null)
                {
                    if (_transform == Matrix.Identity)
                    {
                        _renderedGeometry = DefiningGeometry;
                    }
                    else
                    {
                        _renderedGeometry = DefiningGeometry.Clone();

                        if (_renderedGeometry.Transform == null ||
                            _renderedGeometry.Transform.Value == Matrix.Identity)
                        {
                            _renderedGeometry.Transform = new MatrixTransform(_transform);
                        }
                        else
                        {
                            _renderedGeometry.Transform = new MatrixTransform(
                                _renderedGeometry.Transform.Value * _transform);
                        }
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

        public IBrush Stroke
        {
            get { return GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public AvaloniaList<double> StrokeDashArray
        {
            get { return GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        public double StrokeDashOffset
        {
            get { return GetValue(StrokeDashOffsetProperty); }
            set { SetValue(StrokeDashOffsetProperty, value); }
        }

        public double StrokeThickness
        {
            get { return GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public PenLineCap StrokeLineCap
        {
            get { return GetValue(StrokeLineCapProperty); }
            set { SetValue(StrokeLineCapProperty, value); }
        }

        public PenLineJoin StrokeJoin
        {
            get { return GetValue(StrokeJoinProperty); }
            set { SetValue(StrokeJoinProperty, value); }
        }

        public override void Render(DrawingContext context)
        {
            var geometry = RenderedGeometry;

            if (geometry != null)
            {
                var pen = new Pen(Stroke, StrokeThickness, new DashStyle(StrokeDashArray, StrokeDashOffset),
                     StrokeLineCap, StrokeJoin);
                context.DrawGeometry(Fill, pen, geometry);
            }
        }

        /// <summary>
        /// Marks a property as affecting the shape's geometry.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateGeometry"/> to be called on the element.
        /// </remarks>
        protected static void AffectsGeometry<TShape>(params AvaloniaProperty[] properties)
            where TShape : Shape
        {
            foreach (var property in properties)
            {
                property.Changed.Subscribe(e =>
                {
                    var senderType = e.Sender.GetType().GetTypeInfo();
                    var affectedType = typeof(TShape).GetTypeInfo();

                    if (affectedType.IsAssignableFrom(senderType))
                    {
                        AffectsGeometryInvalidate(e);
                    }
                });
            }
        }

        protected abstract Geometry CreateDefiningGeometry();

        protected void InvalidateGeometry()
        {
            _renderedGeometry = null;
            _definingGeometry = null;
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            bool deferCalculateTransform;
            switch (Stretch)
            {
                case Stretch.Fill:
                case Stretch.UniformToFill:
                    deferCalculateTransform = double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height);
                    break;
                case Stretch.Uniform:
                    deferCalculateTransform = double.IsInfinity(availableSize.Width) && double.IsInfinity(availableSize.Height);
                    break;
                case Stretch.None:
                default:
                    deferCalculateTransform = false;
                    break;
            }

            if (deferCalculateTransform)
            {
                _calculateTransformOnArrange = true;
                return DefiningGeometry?.Bounds.Size ?? Size.Empty;
            }
            else
            {
                _calculateTransformOnArrange = false;
                return CalculateShapeSizeAndSetTransform(availableSize);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_calculateTransformOnArrange)
            {
                _calculateTransformOnArrange = false;
                CalculateShapeSizeAndSetTransform(finalSize);
            }

            return finalSize;
        }

        private Size CalculateShapeSizeAndSetTransform(Size availableSize)
        {
            if (DefiningGeometry != null)
            {
                // This should probably use GetRenderBounds(strokeThickness) but then the calculations
                // will multiply the stroke thickness as well, which isn't correct.
                var (size, transform) = CalculateSizeAndTransform(availableSize, DefiningGeometry.Bounds, Stretch);

                if (_transform != transform)
                {
                    _transform = transform;
                    _renderedGeometry = null;
                }

                return size;
            }

            return Size.Empty;
        }

        internal static (Size, Matrix) CalculateSizeAndTransform(Size availableSize, Rect shapeBounds, Stretch Stretch)
        {
            Size shapeSize = new Size(shapeBounds.Right, shapeBounds.Bottom);
            Matrix translate = Matrix.Identity;
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

            var transform = translate * Matrix.CreateScale(sx, sy);
            var size = new Size(shapeSize.Width * sx, shapeSize.Height * sy);
            return (size, transform);
        }

        private static void AffectsGeometryInvalidate(AvaloniaPropertyChangedEventArgs e)
        {
            if (!(e.Sender is Shape control))
            {
                return;
            }

            // If the geometry is invalidated when Bounds changes, only invalidate when the Size
            // portion changes.
            if (e.Property == BoundsProperty)
            {
                var oldBounds = (Rect)e.OldValue;
                var newBounds = (Rect)e.NewValue;

                if (oldBounds.Size == newBounds.Size)
                {
                    return;
                }
            }

            control.InvalidateGeometry();
        }
    }
}
