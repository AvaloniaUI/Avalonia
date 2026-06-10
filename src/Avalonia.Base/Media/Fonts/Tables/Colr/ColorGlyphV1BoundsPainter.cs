using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Walks a resolved COLR v1 paint graph and accumulates two things, in one backend-free pass: the
    /// axis-aligned union of the painted outlines' control-point extents (in font space, Y-up — used to
    /// derive a colour glyph's bounds when the font ships no ClipList), and the set of referenced glyph
    /// ids (the colour drawing's cache dependencies — the layer outlines it re-fetches on every draw).
    /// Each referenced outline's box is read through <see cref="GlyphTypeface.TryGetGlyphInkBounds"/>,
    /// which interprets the outline (or reads the <c>glyf</c> header) <b>without building geometry</b> —
    /// so no render backend is required and no outline is materialised just to size the glyph.
    /// </summary>
    internal sealed class ColorGlyphV1BoundsPainter : IColorPainter
    {
        private readonly ColrContext _context;
        private readonly Stack<Matrix> _transformStack = new();
        private readonly List<ushort> _dependencies = new();
        private Matrix _accumulatedTransform = Matrix.Identity;
        private Rect? _pendingGlyph;
        private Rect _bounds;
        private bool _hasBounds;

        public ColorGlyphV1BoundsPainter(ColrContext context)
        {
            _context = context;
        }

        /// <summary>Whether any painted outline contributed to <see cref="Bounds"/>.</summary>
        public bool HasBounds => _hasBounds;

        /// <summary>The accumulated extent in font space (Y-up); meaningful only when <see cref="HasBounds"/>.</summary>
        public Rect Bounds => _bounds;

        /// <summary>The distinct glyph ids referenced by the paint graph, in first-seen order.</summary>
        public ushort[] Dependencies => _dependencies.Count == 0 ? Array.Empty<ushort>() : _dependencies.ToArray();

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

        // Layers and clips never enlarge the painted extent (a clip only ever shrinks it), so for a
        // conservative bound they need no handling here.
        public void PushLayer(CompositeMode mode) { }
        public void PopLayer() { }
        public void PushClip(Rect clipBox) { }
        public void PopClip() { }

        public void FillSolid(Color color) => CommitPendingGlyph();

        public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend)
            => CommitPendingGlyph();

        public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend)
            => CommitPendingGlyph();

        public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend)
            => CommitPendingGlyph();

        public void Glyph(ushort glyphId)
        {
            // Record the referenced glyph as a dependency (deduped) regardless of whether it has a
            // readable box — Draw re-fetches its outline either way, so the cache should pin it.
            if (!_dependencies.Contains(glyphId))
            {
                _dependencies.Add(glyphId);
            }

            // Read the control-point box (no geometry build) and apply the paint-graph transform
            // accumulated so far, as an axis-aligned box.
            _pendingGlyph = _context.GlyphTypeface.TryGetGlyphInkBounds(glyphId, out var box)
                ? ToRect(box).TransformToAABB(_accumulatedTransform)
                : null;
        }

        // A COLR v1 fill paints the most recently set glyph outline (PaintGlyph clips its sub-paint to
        // that outline), so the fill is what commits the outline's extent into the union.
        private void CommitPendingGlyph()
        {
            if (_pendingGlyph is { } rect)
            {
                _bounds = _hasBounds ? _bounds.Union(rect) : rect;
                _hasBounds = true;
                _pendingGlyph = null;
            }
        }

        private static Rect ToRect(GlyphBounds box) => new(box.XMin, box.YMin, box.Width, box.Height);
    }
}
