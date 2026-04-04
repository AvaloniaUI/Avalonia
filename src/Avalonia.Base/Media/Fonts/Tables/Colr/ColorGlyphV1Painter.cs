using System;
using System.Collections.Generic;
using Avalonia.Media.Immutable;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Implements painting for COLR v1 glyphs using Avalonia's DrawingContext.
    /// </summary>
    internal sealed class ColorGlyphV1Painter : IColorPainter
    {
        private readonly DrawingContext _drawingContext;
        private readonly ColrContext _context;
        private readonly Stack<IDisposable> _stateStack = new Stack<IDisposable>();

        // Track the pending glyph that needs to be painted with the next fill
        // In COLR v1, there's a 1:1 mapping between glyph and fill operations
        private Geometry? _pendingGlyph;
        
        // Track the accumulated transform that should be applied to geometry and brushes
        private Matrix _accumulatedTransform = Matrix.Identity;
        private readonly Stack<Matrix> _transformStack = new Stack<Matrix>();

        public ColorGlyphV1Painter(DrawingContext drawingContext, ColrContext context)
        {
            _drawingContext = drawingContext;
            _context = context;
        }

        public void PushTransform(Matrix transform)
        {
            _transformStack.Push(_accumulatedTransform);
            _accumulatedTransform = transform * _accumulatedTransform;
        }

        public void PopTransform()
        {
            if (_transformStack.Count > 0)
            {
                _accumulatedTransform = _transformStack.Pop();
            }
        }

        public void PushLayer(CompositeMode mode)
        {
            // COLR v1 composite modes are not fully supported in the base drawing context
            // For now, we use opacity layers to provide basic composition support
            // TODO: Implement proper blend mode support when available
            _stateStack.Push(_drawingContext.PushOpacity(1.0));
        }

        public void PopLayer()
        {
            if (_stateStack.Count > 0)
            {
                _stateStack.Pop().Dispose();
            }
        }

        public void PushClip(Rect clipBox)
        {
            // Transform the clip box with accumulated transforms
            var transformedClip = clipBox.TransformToAABB(_accumulatedTransform);

            _stateStack.Push(_drawingContext.PushClip(transformedClip));
        }

        public void PopClip()
        {
            if (_stateStack.Count > 0)
            {
                _stateStack.Pop().Dispose();
            }
        }

        public void FillSolid(Color color)
        {
            // Render the pending glyph with this solid color
            if (_pendingGlyph != null)
            {
                var brush = new ImmutableSolidColorBrush(color);

                _drawingContext.DrawGeometry(brush, null, _pendingGlyph);

                _pendingGlyph = null;
            }
        }

        /// <summary>
        /// Creates a brush transform that applies any accumulated transforms.
        /// </summary>
        private ImmutableTransform? CreateBrushTransform()
        {
            return _accumulatedTransform != Matrix.Identity ? new ImmutableTransform(_accumulatedTransform) : null;
        }

        public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend)
        {
            if (_pendingGlyph != null)
            {
                var gradientStops = new ImmutableGradientStop[stops.Length];

                for (var i = 0; i < stops.Length; i++)
                {
                    gradientStops[i] = new ImmutableGradientStop(stops[i].Offset, stops[i].Color);
                }

                var brush = new ImmutableLinearGradientBrush(
                    gradientStops: gradientStops,
                    opacity: 1.0,
                    transform: CreateBrushTransform(),
                    transformOrigin: new RelativePoint(0, 0, RelativeUnit.Absolute),
                    spreadMethod: extend,
                    startPoint: new RelativePoint(p0, RelativeUnit.Absolute),
                    endPoint: new RelativePoint(p1, RelativeUnit.Absolute));

                _drawingContext.DrawGeometry(brush, null, _pendingGlyph);
                _pendingGlyph = null;
            }
        }

        public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend)
        {
            if (_pendingGlyph != null)
            {
                // Avalonia's RadialGradientBrush doesn't support two-point gradients with different radii
                // We approximate by using the larger circle as the gradient
                var center = r1 > r0 ? c1 : c0;
                var radius = Math.Max(r0, r1);

                var gradientStops = new ImmutableGradientStop[stops.Length];

                for (var i = 0; i < stops.Length; i++)
                {
                    gradientStops[i] = new ImmutableGradientStop(stops[i].Offset, stops[i].Color);
                }

                var brush = new ImmutableRadialGradientBrush(
                    gradientStops: gradientStops,
                    opacity: 1.0,
                    transform: CreateBrushTransform(),
                    transformOrigin: new RelativePoint(0, 0, RelativeUnit.Absolute),
                    spreadMethod: extend,
                    center: new RelativePoint(center, RelativeUnit.Absolute),
                    gradientOrigin: new RelativePoint(center, RelativeUnit.Absolute),
                    radiusX: new RelativeScalar(radius, RelativeUnit.Absolute),
                    radiusY: new RelativeScalar(radius, RelativeUnit.Absolute));

                _drawingContext.DrawGeometry(brush, null, _pendingGlyph);
                _pendingGlyph = null;
            }
        }

        public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend)
        {
            if (_pendingGlyph != null)
            {
                var gradientStops = new ImmutableGradientStop[stops.Length];

                for (var i = 0; i < stops.Length; i++)
                {
                    gradientStops[i] = new ImmutableGradientStop(stops[i].Offset, stops[i].Color);
                }

                var brush = new ImmutableConicGradientBrush(
                    gradientStops: gradientStops,
                    opacity: 1.0,
                    transform: CreateBrushTransform(),
                    transformOrigin: new RelativePoint(0, 0, RelativeUnit.Absolute),
                    spreadMethod: extend,
                    center: new RelativePoint(center, RelativeUnit.Absolute),
                    angle: startAngle);

                _drawingContext.DrawGeometry(brush, null, _pendingGlyph);

                _pendingGlyph = null;
            }
        }

        public void Glyph(ushort glyphId)
        {
            // Store the glyph geometry to be rendered when we encounter the fill
            var geometry = _context.GlyphTypeface.GetGlyphOutline(glyphId, _accumulatedTransform);

            if (geometry != null)
            {              
                _pendingGlyph = geometry;
            }
        }
    }
}
