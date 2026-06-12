using System;
using System.Collections.Generic;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Implements painting for COLR v1 glyphs using Avalonia's DrawingContext.
    /// </summary>
    /// <remarks>
    /// Paint-graph transforms are pushed onto the drawing context rather than baked into the
    /// geometry: each cached design-space outline is drawn as-is (no per-draw transformed clone),
    /// the context maps gradient coordinates — which COLR defines in the same glyph space — without
    /// a manual brush transform, and a pushed clip box clips exactly under rotation / skew instead
    /// of as an axis-aligned approximation.
    /// </remarks>
    internal sealed class ColorGlyphV1Painter : IColorPainter, IDisposable
    {
        private readonly DrawingContext _drawingContext;
        private readonly ColrContext _context;

        // Every push (transform / clip / layer) onto the caller's DrawingContext lands on this one
        // LIFO stack. The traverser pairs each push with a pop in correct nesting order, so the top
        // is always the matching state; on a mid-paint throw the traverser's pop is skipped, so
        // Dispose unwinds whatever remains — otherwise the pushed states would leak into the
        // caller's context and corrupt all subsequent drawing.
        private readonly Stack<IDisposable> _pushedStates = new Stack<IDisposable>();

        // Track the pending glyph that needs to be painted with the next fill
        // In COLR v1, there's a 1:1 mapping between glyph and fill operations
        private IGeometryImpl? _pendingGlyph;

        public ColorGlyphV1Painter(DrawingContext drawingContext, ColrContext context)
        {
            _drawingContext = drawingContext;
            _context = context;
        }

        public void PushTransform(Matrix transform)
        {
            _pushedStates.Push(_drawingContext.PushTransform(transform));
        }

        public void PopTransform() => PopState();

        public void PushLayer(CompositeMode mode)
        {
            // COLR v1 composite modes are not fully supported in the base drawing context
            // For now, we use opacity layers to provide basic composition support
            // TODO: Implement proper blend mode support when available
            _pushedStates.Push(_drawingContext.PushOpacity(1.0));
        }

        public void PopLayer() => PopState();

        public void PushClip(Rect clipBox)
        {
            // The context transform applies to the clip, so the box clips exactly — even rotated.
            _pushedStates.Push(_drawingContext.PushClip(clipBox));
        }

        public void PopClip() => PopState();

        private void PopState()
        {
            if (_pushedStates.Count > 0)
            {
                _pushedStates.Pop().Dispose();
            }
        }

        /// <summary>
        /// Unwinds any states still pushed onto the caller's <see cref="DrawingContext"/> — the
        /// normal case leaves none (every push was popped during traversal), but a throw mid-paint
        /// leaves the in-flight states here, and disposing in reverse-push (LIFO) order restores
        /// the context exactly.
        /// </summary>
        public void Dispose()
        {
            while (_pushedStates.Count > 0)
            {
                _pushedStates.Pop().Dispose();
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
                // A COLR PaintRadialGradient interpolates between circle 0 (c0, r0) at color-line
                // position 0 and circle 1 (c1, r1) at position 1. Avalonia's RadialGradientBrush models
                // the position-1 circle as Center + radius and position 0 as a focal *point*
                // (GradientOrigin), so map circle 1 → Center/radius and circle 0's centre → the focal
                // point. The focal radius r0 has no equivalent and is approximated as 0 — exact for the
                // common focal-radial case (r0 == 0), an approximation otherwise.
                var gradientStops = new ImmutableGradientStop[stops.Length];

                for (var i = 0; i < stops.Length; i++)
                {
                    gradientStops[i] = new ImmutableGradientStop(stops[i].Offset, stops[i].Color);
                }

                var brush = new ImmutableRadialGradientBrush(
                    gradientStops: gradientStops,
                    opacity: 1.0,
                    spreadMethod: extend,
                    center: new RelativePoint(c1, RelativeUnit.Absolute),
                    gradientOrigin: new RelativePoint(c0, RelativeUnit.Absolute),
                    radiusX: new RelativeScalar(r1, RelativeUnit.Absolute),
                    radiusY: new RelativeScalar(r1, RelativeUnit.Absolute));

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
                    spreadMethod: extend,
                    center: new RelativePoint(center, RelativeUnit.Absolute),
                    angle: startAngle);

                _drawingContext.DrawGeometry(brush, null, _pendingGlyph);

                _pendingGlyph = null;
            }
        }

        public void Glyph(ushort glyphId)
        {
            // The cached design-space outline, drawn under the context transform — no per-draw clone.
            var geometry = _context.GlyphTypeface.GetGlyphOutline(glyphId);

            if (geometry != null)
            {
                _pendingGlyph = geometry;
            }
        }
    }
}
