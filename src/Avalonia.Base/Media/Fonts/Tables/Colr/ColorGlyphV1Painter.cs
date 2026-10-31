using System;
using System.Collections.Generic;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Implements painting for COLR v1 glyphs using Avalonia's DrawingContext.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Paint-graph transforms are pushed onto the drawing context rather than baked into the
    /// geometry: each cached design-space outline is drawn as-is (no per-draw transformed clone),
    /// the context maps gradient coordinates, which COLR defines in the same glyph space, without
    /// a manual brush transform, and a pushed clip box clips exactly under rotation / skew instead
    /// of as an axis-aligned approximation.
    /// </para>
    /// <para>
    /// A glyph clip (PaintGlyph) is pushed lazily. While nothing but fills follows it, each fill
    /// draws the outline geometry with its brush, which is the clip filled and avoids a geometry
    /// clip for the common PaintGlyph-over-fill graph. Any other operation (a transform, a clip, a
    /// layer) first pushes the outline as a geometry clip in the transform it was declared in;
    /// fills under it then fill a rectangle covering the clip region.
    /// </para>
    /// </remarks>
    internal sealed class ColorGlyphV1Painter : IColorPainter, IDisposable
    {
        private readonly DrawingContext _drawingContext;
        private readonly ColrContext _context;

        // Every push (transform / clip / layer) onto the caller's DrawingContext lands on this one
        // LIFO stack. The traverser pairs each push with a pop in correct nesting order, so the top
        // is always the matching state; on a mid-paint throw the traverser's pop is skipped, so
        // Dispose unwinds whatever remains, otherwise the pushed states would leak into the
        // caller's context and corrupt all subsequent drawing.
        private readonly Stack<IDisposable> _pushedStates = new Stack<IDisposable>();

        private readonly Stack<Matrix> _transformStack = new Stack<Matrix>();
        private readonly Stack<ClipEntry> _clips = new Stack<ClipEntry>();

        // The paint-graph transform relative to the painter's root space (font units, Y-up).
        private Matrix _transform = Matrix.Identity;

        public ColorGlyphV1Painter(DrawingContext drawingContext, ColrContext context)
        {
            _drawingContext = drawingContext;
            _context = context;
        }

        public void PushTransform(Matrix transform)
        {
            MaterializePendingClip();

            _pushedStates.Push(_drawingContext.PushTransform(transform));
            _transformStack.Push(_transform);
            _transform = transform * _transform;
        }

        public void PopTransform()
        {
            PopState();

            if (_transformStack.Count > 0)
            {
                _transform = _transformStack.Pop();
            }
        }

        public void PushLayer(CompositeMode mode)
        {
            MaterializePendingClip();

            // DrawingContext has no blend-mode layer, so every PaintComposite mode composites as
            // SrcOver: the backdrop is drawn, then the source over it, inside an opacity-1 layer.
            // Modes other than SrcOver render incorrectly.
            _pushedStates.Push(_drawingContext.PushOpacity(1.0));
        }

        public void PopLayer() => PopState();

        public void PushClip(Rect clipBox)
        {
            MaterializePendingClip();

            // The context transform applies to the clip, so the box clips exactly, even rotated.
            _pushedStates.Push(_drawingContext.PushClip(clipBox));
            _clips.Push(new ClipEntry(null, Intersect(clipBox.TransformToAABB(_transform)), isPushed: true));
        }

        public void PushGlyphClip(ushort glyphIndex)
        {
            MaterializePendingClip();

            // The cached design-space outline, used under the context transform with no per-draw
            // clone. A glyph without an outline clips everything away.
            var geometry = _context.GlyphTypeface.GetGlyphOutline(glyphIndex);

            var region = geometry is null ? (Rect?)null : Intersect(geometry.Bounds.TransformToAABB(_transform));

            _clips.Push(new ClipEntry(geometry, region, isPushed: false));
        }

        public void PopClip()
        {
            if (_clips.Count == 0)
            {
                return;
            }

            if (_clips.Pop().IsPushed)
            {
                PopState();
            }
        }

        private void PopState()
        {
            if (_pushedStates.Count > 0)
            {
                _pushedStates.Pop().Dispose();
            }
        }

        /// <summary>
        /// Unwinds any states still pushed onto the caller's <see cref="DrawingContext"/>, the
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

        public void FillSolid(Color color) => Fill(new ImmutableSolidColorBrush(color));

        public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend)
        {
            Fill(new ImmutableLinearGradientBrush(
                gradientStops: ToImmutableStops(stops),
                opacity: 1.0,
                spreadMethod: extend,
                startPoint: new RelativePoint(p0, RelativeUnit.Absolute),
                endPoint: new RelativePoint(p1, RelativeUnit.Absolute)));
        }

        public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend)
        {
            // A COLR PaintRadialGradient interpolates between circle 0 (c0, r0) at color-line
            // position 0 and circle 1 (c1, r1) at position 1. Avalonia's RadialGradientBrush is a
            // single circle (Center + radius) at stop offset 1 with a focal point (GradientOrigin)
            // at offset 0.
            if (MathUtilities.AreClose(c0.X, c1.X) && MathUtilities.AreClose(c0.Y, c1.Y))
            {
                FillConcentricRadialGradient(c1, r0, r1, stops, extend);
                return;
            }

            // Two distinct circles map circle 1 to Center + radius and circle 0's centre to the focal
            // point; circle 0's radius is dropped. That is exact only when r0 is 0. A general
            // two-circle gradient with r0 > 0 needs a two-point conical brush, which the drawing
            // backends do not expose.
            Fill(new ImmutableRadialGradientBrush(
                gradientStops: ToImmutableStops(stops),
                opacity: 1.0,
                spreadMethod: extend,
                center: new RelativePoint(c1, RelativeUnit.Absolute),
                gradientOrigin: new RelativePoint(c0, RelativeUnit.Absolute),
                radiusX: new RelativeScalar(r1, RelativeUnit.Absolute),
                radiusY: new RelativeScalar(r1, RelativeUnit.Absolute)));
        }

        /// <summary>
        /// Fills a radial gradient whose two circles share a centre. Color-line position t lies at
        /// radius r0 + t (r1 - r0); the brush spans the larger radius R, so each stop moves to brush
        /// offset (r0 + t (r1 - r0)) / R, in reverse order when r0 &gt; r1. Exact for the Pad extend;
        /// Repeat and Reflect repeat over [0, R] rather than over the [r0, r1] band, so they are exact
        /// only when the smaller radius is 0.
        /// </summary>
        private void FillConcentricRadialGradient(Point center, double r0, double r1, GradientStop[] stops,
            GradientSpreadMethod extend)
        {
            var radius = Math.Max(r0, r1);

            if (radius <= 0)
            {
                return;
            }

            var gradientStops = new ImmutableGradientStop[stops.Length];

            for (var i = 0; i < stops.Length; i++)
            {
                var stop = r0 > r1 ? stops[stops.Length - 1 - i] : stops[i];
                var offset = (r0 + stop.Offset * (r1 - r0)) / radius;
                gradientStops[i] = new ImmutableGradientStop(offset, stop.Color);
            }

            Fill(new ImmutableRadialGradientBrush(
                gradientStops: gradientStops,
                opacity: 1.0,
                spreadMethod: extend,
                center: new RelativePoint(center, RelativeUnit.Absolute),
                gradientOrigin: new RelativePoint(center, RelativeUnit.Absolute),
                radiusX: new RelativeScalar(radius, RelativeUnit.Absolute),
                radiusY: new RelativeScalar(radius, RelativeUnit.Absolute)));
        }

        public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend)
        {
            Fill(new ImmutableConicGradientBrush(
                gradientStops: ToImmutableStops(stops),
                opacity: 1.0,
                spreadMethod: extend,
                center: new RelativePoint(center, RelativeUnit.Absolute),
                angle: startAngle));
        }

        private static ImmutableGradientStop[] ToImmutableStops(GradientStop[] stops)
        {
            var gradientStops = new ImmutableGradientStop[stops.Length];

            for (var i = 0; i < stops.Length; i++)
            {
                gradientStops[i] = new ImmutableGradientStop(stops[i].Offset, stops[i].Color);
            }

            return gradientStops;
        }

        /// <summary>
        /// Fills the current clip region with <paramref name="brush"/>. Without any clip the region
        /// is unbounded, and nothing is drawn.
        /// </summary>
        private void Fill(IBrush brush)
        {
            if (_clips.Count == 0)
            {
                return;
            }

            var top = _clips.Peek();

            if (top.Region is not { } region)
            {
                return;
            }

            // A glyph clip not yet pushed was declared in the current transform (any push since
            // would have pushed it), so filling it is drawing the outline.
            if (!top.IsPushed && top.Geometry is not null)
            {
                _drawingContext.DrawGeometry(brush, null, top.Geometry);
                return;
            }

            // Otherwise cover the clip region, mapped from root space into the current space; the
            // pushed clips trim the rectangle to the exact region. A singular transform paints nothing.
            if (!_transform.TryInvert(out var inverse))
            {
                return;
            }

            _drawingContext.DrawRectangle(brush, null, region.TransformToAABB(inverse));
        }

        /// <summary>
        /// Pushes the innermost glyph clip as a geometry clip if it has not been pushed yet, before
        /// an operation that changes the transform, clip or layer it would otherwise be drawn under.
        /// </summary>
        private void MaterializePendingClip()
        {
            if (_clips.Count == 0)
            {
                return;
            }

            var top = _clips.Peek();

            if (top.IsPushed || top.Geometry is null)
            {
                return;
            }

            _pushedStates.Push(_drawingContext.PushGeometryClip(new PlatformGeometry(top.Geometry)));
            top.IsPushed = true;
        }

        /// <summary>
        /// Intersects a root-space rectangle with the current clip region; null when the result is
        /// empty, so everything under it is clipped away.
        /// </summary>
        private Rect? Intersect(Rect rect)
        {
            if (_clips.Count > 0)
            {
                if (_clips.Peek().Region is not { } outer)
                {
                    return null;
                }

                rect = outer.Intersect(rect);
            }

            return rect.Width > 0 && rect.Height > 0 ? rect : null;
        }

        private sealed class ClipEntry
        {
            public ClipEntry(IGeometryImpl? geometry, Rect? region, bool isPushed)
            {
                Geometry = geometry;
                Region = region;
                IsPushed = isPushed;
            }

            /// <summary>The glyph outline of a PaintGlyph clip; null for a clip box.</summary>
            public IGeometryImpl? Geometry { get; }

            /// <summary>The axis-aligned extent of the clip region in root space; null when empty.</summary>
            public Rect? Region { get; }

            /// <summary>Whether the clip has been pushed onto the drawing context.</summary>
            public bool IsPushed { get; set; }
        }
    }
}
