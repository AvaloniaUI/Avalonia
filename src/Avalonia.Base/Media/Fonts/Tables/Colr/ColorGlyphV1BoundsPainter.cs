using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Walks a resolved COLR v1 paint graph and accumulates two things, in one backend-free pass: the
    /// axis-aligned union of the painted clip regions (in font space, Y-up, used to derive a colour
    /// glyph's bounds when the font ships no ClipList), and the set of referenced glyph ids (the
    /// colour drawing's cache dependencies, the layer outlines it re-fetches on every draw). A fill
    /// paints its clip region: the intersection of the enclosing PaintGlyph outlines' control-point
    /// boxes and clip boxes, each taken in the transform it was declared in.
    /// Each referenced outline's box is read through <see cref="GlyphTypeface.TryGetGlyphInkBounds"/>,
    /// which interprets the outline (or reads the <c>glyf</c> header) <b>without building geometry</b>,
    /// so no render backend is required and no outline is materialised just to size the glyph.
    /// </summary>
    internal sealed class ColorGlyphV1BoundsPainter : IColorPainter
    {
        private readonly ColrContext _context;
        private readonly Stack<Matrix> _transformStack = new();

        // The clip regions in font space; a null entry is an empty region (nothing under it paints).
        private readonly Stack<Rect?> _clips = new();
        private readonly List<ushort> _dependencies = new();
        private Matrix _accumulatedTransform = Matrix.Identity;
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

        // Composite layers never enlarge the painted extent.
        public void PushLayer(CompositeMode mode) { }
        public void PopLayer() { }

        public void PushClip(Rect clipBox) => _clips.Push(Intersect(clipBox.TransformToAABB(_accumulatedTransform)));

        public void PopClip()
        {
            if (_clips.Count > 0)
            {
                _clips.Pop();
            }
        }

        public void PushGlyphClip(ushort glyphIndex)
        {
            // Record the referenced glyph as a dependency (deduped) regardless of whether it has a
            // readable box, Draw re-fetches its outline either way, so the cache should pin it.
            if (!_dependencies.Contains(glyphIndex))
            {
                _dependencies.Add(glyphIndex);
            }

            // Read the control-point box (no geometry build) and apply the paint-graph transform
            // accumulated so far, as an axis-aligned box. A glyph without a box clips everything away.
            _clips.Push(_context.GlyphTypeface.TryGetGlyphInkBounds(glyphIndex, out var box)
                ? Intersect(ToRect(box).TransformToAABB(_accumulatedTransform))
                : null);
        }

        public void FillSolid(Color color) => AddClipRegion();

        public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend)
            => AddClipRegion();

        public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend)
            => AddClipRegion();

        public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend)
            => AddClipRegion();

        // A fill paints its clip region; a fill outside any clip is unbounded and paints nothing.
        private void AddClipRegion()
        {
            if (_clips.Count > 0 && _clips.Peek() is { } rect)
            {
                _bounds = _hasBounds ? _bounds.Union(rect) : rect;
                _hasBounds = true;
            }
        }

        // Intersects a font-space box with the current clip region; null when the result is empty.
        private Rect? Intersect(Rect rect)
        {
            if (_clips.Count > 0)
            {
                if (_clips.Peek() is not { } outer)
                {
                    return null;
                }

                rect = outer.Intersect(rect);
            }

            return rect.Width > 0 && rect.Height > 0 ? rect : null;
        }

        private static Rect ToRect(GlyphBounds box) => new(box.XMin, box.YMin, box.Width, box.Height);
    }
}
