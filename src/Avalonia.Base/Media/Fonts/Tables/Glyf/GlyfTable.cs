using System;
using System.Buffers;
using System.Buffers.Binary;
using Avalonia.Platform;
using Avalonia.Logging;

namespace Avalonia.Media.Fonts.Tables.Glyf
{
    /// <summary>
    /// Reader for the 'glyf' table. Provides on-demand access to individual glyph data using the 'loca' index.
    /// Designed for high-performance lookups on the hot path.
    /// </summary>
    internal sealed class GlyfTable
    {
        internal const string TableName = "glyf";

        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private readonly ReadOnlyMemory<byte> _glyfData;
        private readonly LocaTable _locaTable;

        private GlyfTable(ReadOnlyMemory<byte> glyfData, LocaTable locaTable)
        {
            _glyfData = glyfData;
            _locaTable = locaTable;
        }

        /// <summary>
        /// Gets the total number of glyphs defined in the font.
        /// </summary>
        public int GlyphCount => _locaTable.GlyphCount;

        /// <summary>
        /// Attempts to load the 'glyf' table from the specified font data.
        /// </summary>
        /// <remarks>This method does not throw an exception if the 'glyf' table cannot be loaded.
        /// Instead, it returns <see langword="false"/> and sets <paramref name="glyfTable"/> to <see
        /// langword="null"/>.</remarks>
        /// <param name="glyphTypeface">The glyph typeface from which to retrieve the 'glyf' table.</param>
        /// <param name="head">The 'head' table containing font header information required for loading the 'glyf' table.</param>
        /// <param name="maxp">The 'maxp' table providing maximum profile information needed to interpret the 'glyf' table.</param>
        /// <param name="glyfTable">When this method returns, contains the loaded 'glyf' table if successful; otherwise, <see langword="null"/>.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the 'glyf' table was successfully loaded; otherwise, <see langword="false"/>.</returns>
        public static bool TryLoad(GlyphTypeface glyphTypeface, HeadTable head, MaxpTable maxp, out GlyfTable? glyfTable)
        {
            glyfTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var glyfTableData))
            {
                return false;
            }

            var locaTable = LocaTable.Load(glyphTypeface, head, maxp);

            if (locaTable == null)
            {
                return false;
            }

            glyfTable = new GlyfTable(glyfTableData, locaTable);

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the raw glyph data for the specified glyph index.
        /// </summary>
        /// <remarks>If the glyph exists but has no data (for example, a missing or empty glyph), the
        /// method returns true and sets the out parameter to an empty memory region. If the glyph index is invalid or
        /// out of range, the method returns false and the out parameter is set to an empty memory region.</remarks>
        /// <param name="glyphIndex">The zero-based index of the glyph to retrieve data for.</param>
        /// <param name="data">When this method returns, contains the glyph data as a read-only memory region if the glyph exists;
        /// otherwise, contains an empty memory region.</param>
        /// <returns>true if the glyph data was found and assigned to the out parameter; otherwise, false.</returns>
        public bool TryGetGlyphData(int glyphIndex, out ReadOnlyMemory<byte> data)
        {
            if (!_locaTable.TryGetOffsets(glyphIndex, out var start, out var end))
            {
                data = ReadOnlyMemory<byte>.Empty;
                return false;
            }

            if (start == end)
            {
                data = ReadOnlyMemory<byte>.Empty;
                return true;
            }

            // Additional safety check for glyf table bounds
            if (start < 0 || end > _glyfData.Length || start > end)
            {
                data = ReadOnlyMemory<byte>.Empty;

                return false;
            }

            data = _glyfData.Slice(start, end - start);

            return true;
        }

        /// <summary>
        /// Reads a glyph's bounding box from its 'glyf' header without parsing contours.
        /// </summary>
        /// <remarks>
        /// The values are the control-point bounding box stored in the glyph header
        /// (the min/max of all on- and off-curve points), in font design units. This is a
        /// slight superset of the rendered ink bounds for glyphs with off-curve points.
        /// Composite glyphs carry their overall bounding box in the header too, so no
        /// recursion is needed. Returns <see langword="true"/> with all-zero bounds for
        /// empty glyphs (e.g. whitespace); returns <see langword="false"/> when the glyph
        /// index is out of range or the glyph data is too short to contain a header.
        /// </remarks>
        /// <param name="glyphIndex">The zero-based glyph index.</param>
        /// <param name="xMin">The minimum x coordinate of the bounding box.</param>
        /// <param name="yMin">The minimum y coordinate of the bounding box.</param>
        /// <param name="xMax">The maximum x coordinate of the bounding box.</param>
        /// <param name="yMax">The maximum y coordinate of the bounding box.</param>
        /// <returns><see langword="true"/> if bounds were resolved (including empty glyphs); otherwise <see langword="false"/>.</returns>
        public bool TryGetGlyphBounds(int glyphIndex, out short xMin, out short yMin, out short xMax, out short yMax)
        {
            xMin = 0;
            yMin = 0;
            xMax = 0;
            yMax = 0;

            if (!TryGetGlyphData(glyphIndex, out var data))
            {
                // Out of range.
                return false;
            }

            if (data.IsEmpty)
            {
                // Empty glyph (e.g. whitespace): valid, zero bounds.
                return true;
            }

            var span = data.Span;

            // Glyph header: int16 numberOfContours, then int16 xMin, yMin, xMax, yMax.
            if (span.Length < 10)
            {
                return false;
            }

            xMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(2, 2));
            yMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4, 2));
            xMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6, 2));
            yMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8, 2));

            return true;
        }

        /// <summary>
        /// Reads bounding boxes for a batch of glyphs into <paramref name="bounds"/>.
        /// </summary>
        /// <remarks>
        /// The hot path for ink-bounds computation. The <c>glyf</c> and <c>loca</c> spans are
        /// fetched once for the whole batch (not per glyph), and offsets and headers are read
        /// directly — no per-glyph <see cref="ReadOnlyMemory{T}.Span"/> conversion, no
        /// intermediate slices, no nested call chain. Out-of-range, empty, or malformed
        /// glyphs are written as the default (zero) box.
        /// </remarks>
        /// <param name="glyphIndices">The glyph indices to read.</param>
        /// <param name="bounds">Output; must be at least as long as <paramref name="glyphIndices"/>.</param>
        public void GetGlyphBounds(ReadOnlySpan<ushort> glyphIndices, Span<GlyphBounds> bounds)
        {
            var glyf = _glyfData.Span;
            var loca = _locaTable.RawData;
            var shortFormat = _locaTable.IsShortFormat;
            var glyphCount = _locaTable.GlyphCount;
            var entrySize = shortFormat ? 2 : 4;

            for (var i = 0; i < glyphIndices.Length; i++)
            {
                bounds[i] = default;

                int gid = glyphIndices[i];

                if ((uint)gid >= (uint)glyphCount)
                {
                    continue;
                }

                var locaOffset = gid * entrySize;

                // Need both loca[gid] and loca[gid + 1].
                if (locaOffset + (2 * entrySize) > loca.Length)
                {
                    continue;
                }

                int start, end;

                if (shortFormat)
                {
                    start = BinaryPrimitives.ReadUInt16BigEndian(loca.Slice(locaOffset)) * 2;
                    end = BinaryPrimitives.ReadUInt16BigEndian(loca.Slice(locaOffset + 2)) * 2;
                }
                else
                {
                    start = (int)BinaryPrimitives.ReadUInt32BigEndian(loca.Slice(locaOffset));
                    end = (int)BinaryPrimitives.ReadUInt32BigEndian(loca.Slice(locaOffset + 4));
                }

                // Empty (start == end) or malformed glyph → leave the zero box.
                if (end - start < 10 || start < 0 || (uint)end > (uint)glyf.Length)
                {
                    continue;
                }

                bounds[i] = new GlyphBounds(
                    BinaryPrimitives.ReadInt16BigEndian(glyf.Slice(start + 2)),
                    BinaryPrimitives.ReadInt16BigEndian(glyf.Slice(start + 4)),
                    BinaryPrimitives.ReadInt16BigEndian(glyf.Slice(start + 6)),
                    BinaryPrimitives.ReadInt16BigEndian(glyf.Slice(start + 8)));
            }
        }

        /// <summary>
        /// Builds the glyph outline into the provided geometry context. Returns false for empty glyphs.
        /// Coordinates are in font design units. Composite glyphs are supported.
        /// </summary>
        public bool TryBuildGlyphGeometry(int glyphIndex, Matrix transform, IGeometryContext context)
        {
            // TrueType outlines use the non-zero winding rule. The default geometry fill
            // rule in Avalonia is EvenOdd, which would XOR overlapping contours (e.g. the
            // crossbar and diagonal strokes of 'A', or composites where an accent overlaps
            // its base glyph) and leave gaps where they intersect.
            context.SetFillRule(FillRule.NonZero);

            var decycler = GlyphDecycler.Rent();

            try
            {
                return TryBuildGlyphGeometryInternal(glyphIndex, context, transform, decycler);
            }
            catch (DecyclerException ex)
            {
                if (Logger.TryGet(LogEventLevel.Warning, LogArea.Visual, out var log))
                {
                    log.Log(this, "Glyph {0} processing failed: {1}", glyphIndex, ex.Message);
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                GlyphDecycler.Return(decycler);
            }
        }

        /// <summary>
        /// Builds the geometry for a simple glyph by processing its contours and converting them into geometry commands.
        /// </summary>
        /// <param name="simpleGlyph">The simple glyph containing contour data, flags, and coordinates.</param>
        /// <param name="context">The geometry context that receives the constructed glyph geometry.</param>
        /// <param name="transform">The transformation matrix to apply to all coordinates.</param>
        /// <returns>true if the glyph geometry was successfully built; otherwise, false.</returns>
        private static bool BuildSimpleGlyphGeometry(SimpleGlyph simpleGlyph, IGeometryContext context, Matrix transform)
        {
            try
            {
                var endPtsOfContours = simpleGlyph.EndPtsOfContours;

                if (endPtsOfContours.Length == 0)
                {
                    return false;
                }

                var flags = simpleGlyph.Flags;
                var xCoords = simpleGlyph.XCoordinates;
                var yCoords = simpleGlyph.YCoordinates;
                var pointCount = flags.Length;

                // Materialise the points once so every contour goes through the single
                // EmitContour walker shared with the composite point-matching path.
                var points = ArrayPool<Point>.Shared.Rent(pointCount);
                var onCurve = ArrayPool<bool>.Shared.Rent(pointCount);

                try
                {
                    for (var i = 0; i < pointCount; i++)
                    {
                        points[i] = new Point(xCoords[i], yCoords[i]);
                        onCurve[i] = (flags[i] & GlyphFlag.OnCurvePoint) != 0;
                    }

                    var startPointIndex = 0;

                    for (var contourIndex = 0; contourIndex < endPtsOfContours.Length; contourIndex++)
                    {
                        var endPointIndex = endPtsOfContours[contourIndex];
                        var contourPointCount = endPointIndex - startPointIndex + 1;

                        EmitContour(
                            points.AsSpan(startPointIndex, contourPointCount),
                            onCurve.AsSpan(startPointIndex, contourPointCount),
                            transform,
                            context);

                        startPointIndex = endPointIndex + 1;
                    }
                }
                finally
                {
                    ArrayPool<Point>.Shared.Return(points);
                    ArrayPool<bool>.Shared.Return(onCurve);
                }

                return true;
            }
            finally
            {
                // Return rented buffers to pool
                simpleGlyph.Dispose();
            }
        }

        /// <summary>
        /// Emits one contour's segments to the geometry context, applying <paramref name="transform"/>.
        /// </summary>
        /// <remarks>
        /// Implements the TrueType on/off-curve walk once for every caller: the figure starts at
        /// the first point when it is on-curve, at the last point when only that one is on-curve,
        /// and at their implied midpoint when both are off-curve; consecutive off-curve points
        /// imply an on-curve midpoint between them; the contour closes back to the start through
        /// a trailing off-curve control point when one is pending.
        /// </remarks>
        private static void EmitContour(
            ReadOnlySpan<Point> points,
            ReadOnlySpan<bool> onCurve,
            Matrix transform,
            IGeometryContext context)
        {
            var pointCount = points.Length;

            if (pointCount == 0)
            {
                return;
            }

            Point figureStart;
            int walkStart;
            int walkCount;

            if (onCurve[0])
            {
                figureStart = points[0];
                walkStart = 1;
                walkCount = pointCount - 1;
            }
            else if (onCurve[pointCount - 1])
            {
                // The last point is consumed as the start; the walk covers the rest.
                figureStart = points[pointCount - 1];
                walkStart = 0;
                walkCount = pointCount - 1;
            }
            else
            {
                var first = points[0];
                var last = points[pointCount - 1];
                figureStart = new Point((first.X + last.X) / 2.0, (first.Y + last.Y) / 2.0);
                walkStart = 0;
                walkCount = pointCount;
            }

            context.BeginFigure(transform.Transform(figureStart), true);

            var pendingControl = default(Point);
            var hasPendingControl = false;

            for (var i = 0; i < walkCount; i++)
            {
                var index = walkStart + i;
                var point = points[index];

                if (onCurve[index])
                {
                    if (hasPendingControl)
                    {
                        context.QuadraticBezierTo(transform.Transform(pendingControl), transform.Transform(point));
                        hasPendingControl = false;
                    }
                    else
                    {
                        context.LineTo(transform.Transform(point));
                    }
                }
                else
                {
                    if (hasPendingControl)
                    {
                        // Two consecutive off-curve points -> implied on-curve midpoint.
                        var implied = new Point((pendingControl.X + point.X) / 2.0, (pendingControl.Y + point.Y) / 2.0);
                        context.QuadraticBezierTo(transform.Transform(pendingControl), transform.Transform(implied));
                    }

                    pendingControl = point;
                    hasPendingControl = true;
                }
            }

            // Close back to the start: through the trailing control point when one is pending,
            // with an explicit line otherwise (EndFigure's implicit close is then zero-length).
            if (hasPendingControl)
            {
                context.QuadraticBezierTo(transform.Transform(pendingControl), transform.Transform(figureStart));
            }
            else
            {
                context.LineTo(transform.Transform(figureStart));
            }

            context.EndFigure(true);
        }

        /// <summary>
        /// Creates a transformation matrix for a composite glyph component based on its flags and transformation parameters.
        /// </summary>
        /// <param name="component">The glyph component containing transformation information.</param>
        /// <returns>A transformation matrix that should be applied to the component glyph.</returns>
        private static Matrix CreateComponentTransform(GlyphComponent component)
        {
            var flags = component.Flags;

            double tx = 0, ty = 0;

            if ((flags & CompositeFlags.ArgsAreXYValues) != 0)
            {
                tx = component.Arg1;
                ty = component.Arg2;
            }

            double m11, m12, m21, m22;

            if ((flags & CompositeFlags.WeHaveAScale) != 0)
            {
                m11 = m22 = component.Scale;
                m12 = m21 = 0;
            }
            else if ((flags & CompositeFlags.WeHaveAnXAndYScale) != 0)
            {
                m11 = component.ScaleX;
                m22 = component.ScaleY;
                m12 = m21 = 0;
            }
            else if ((flags & CompositeFlags.WeHaveATwoByTwo) != 0)
            {
                m11 = component.ScaleX;
                m12 = component.Scale01;
                m21 = component.Scale10;
                m22 = component.ScaleY;
            }
            else
            {
                m11 = m22 = 1.0;
                m12 = m21 = 0;
            }

            return new Matrix(m11, m12, m21, m22, tx, ty);
        }

        /// <summary>
        /// Attempts to build the geometry for the specified glyph and adds it to the provided geometry context.
        /// </summary>
        /// <remarks>This method processes both simple and composite glyphs. For composite glyphs,
        /// recursion is used and the visited set prevents cycles. The method returns false if the glyph is empty,
        /// invalid, or has already been processed.</remarks>
        /// <param name="glyphIndex">The index of the glyph to process. Must correspond to a valid glyph in the font.</param>
        /// <param name="context">The geometry context that receives the constructed glyph geometry.</param>
        /// <param name="transform">The transformation matrix to apply to the glyph geometry.</param>
        /// <param name="decycler">A <see cref="GlyphDecycler"/> instance used to prevent infinite recursion when building composite glyphs.</param>
        /// <returns>true if the glyph geometry was successfully built and added to the context; otherwise, false.</returns>
        private bool TryBuildGlyphGeometryInternal(int glyphIndex, IGeometryContext context, Matrix transform, GlyphDecycler decycler)
        {
            using var guard = decycler.Enter(glyphIndex);

            if (!TryGetGlyphData(glyphIndex, out var glyphData) || glyphData.IsEmpty)
            {
                return false;
            }

            var descriptor = new GlyphDescriptor(glyphData);

            if (descriptor.IsSimpleGlyph)
            {
                return BuildSimpleGlyphGeometry(descriptor.SimpleGlyph, context, transform);
            }
            else
            {
                return BuildCompositeGlyphGeometry(descriptor.CompositeGlyph, context, transform, decycler);
            }
        }

        /// <summary>
        /// Builds the geometry for a composite glyph by recursively processing its components.
        /// </summary>
        /// <param name="compositeGlyph">The composite glyph containing component references and transformations.</param>
        /// <param name="context">The geometry context that receives the constructed glyph geometry.</param>
        /// <param name="transform">The transformation matrix to apply to all component glyphs.</param>
        /// <param name="decycler">A <see cref="GlyphDecycler"/> instance used to prevent infinite recursion when building composite glyphs.</param>
        /// <returns>true if at least one component was successfully processed; otherwise, false.</returns>
        private bool BuildCompositeGlyphGeometry(CompositeGlyph compositeGlyph, IGeometryContext context, Matrix transform, GlyphDecycler decycler)
        {
            try
            {
                var components = compositeGlyph.Components;

                if (components.Length == 0)
                {
                    return false;
                }

                // When ARGS_ARE_XY_VALUES is clear, arg1/arg2 are point numbers: the component
                // is placed by making one of its points coincide with a point in the
                // already-assembled glyph (point matching), not by an x/y offset. The streaming
                // loop below doesn't retain points, so route those composites through the
                // materialising path instead. The flag is computed once while parsing.
                if (compositeGlyph.UsesPointMatching)
                {
                    return BuildPointMatchedComposite(components, context, transform, decycler);
                }

                var hasGeometry = false;

                foreach (var component in components)
                {
                    var componentTransform = CreateComponentTransform(component);
                    var combinedTransform = componentTransform * transform;

                    var wrappedContext = new TransformingGeometryContext(context, combinedTransform);

                    if (TryBuildGlyphGeometryInternal(component.GlyphIndex, wrappedContext, Matrix.Identity, decycler))
                    {
                        hasGeometry = true;
                    }
                }

                return hasGeometry;
            }
            finally
            {
                // Return rented buffer to pool
                compositeGlyph.Dispose();
            }
        }

        /// <summary>
        /// Builds a composite glyph in which at least one component is placed by point matching.
        /// </summary>
        /// <remarks>
        /// Unlike the streaming fast path, this materialises every component's transformed points
        /// into a single pooled buffer so a point-matched component can be aligned to a point of
        /// the already-assembled glyph, then emits the assembled contours. Only reached for the
        /// rare composites that actually use point matching; all buffers are pooled and released
        /// before returning. Returns <see langword="false"/> (no outline) rather than an incorrect
        /// one for cases not yet supported: a component that is itself composite, or a point index
        /// that is out of range or refers to a phantom point (which this reader does not
        /// materialise).
        /// </remarks>
        private bool BuildPointMatchedComposite(
            ReadOnlySpan<GlyphComponent> components,
            IGeometryContext context,
            Matrix transform,
            GlyphDecycler decycler)
        {
            var outline = new ResolvedOutline(64);

            try
            {
                foreach (var component in components)
                {
                    var componentStart = outline.PointCount;

                    // Resolve the component's points into composite space with its 2x2 scale
                    // applied (but not the placement offset, which is computed next).
                    if (!TryResolveSimpleGlyphPoints(component.GlyphIndex, CreateComponentScale(component), decycler, outline))
                    {
                        return false;
                    }

                    Vector offset;

                    if ((component.Flags & CompositeFlags.ArgsAreXYValues) != 0)
                    {
                        // Signed x/y offset (the unscaled-offset default).
                        offset = new Vector(component.Arg1, component.Arg2);
                    }
                    else
                    {
                        // Point matching: arg1 is a point already placed by an earlier component,
                        // arg2 is a point of this component. They are unsigned point numbers, but
                        // CompositeGlyph parses the raw bytes/words as signed, so reinterpret to
                        // unsigned here (two's-complement round-trip).
                        var argsAreWords = (component.Flags & CompositeFlags.ArgsAreWords) != 0;
                        int parentPoint = argsAreWords ? (ushort)component.Arg1 : (byte)component.Arg1;
                        int componentPoint = argsAreWords ? (ushort)component.Arg2 : (byte)component.Arg2;

                        var componentPointCount = outline.PointCount - componentStart;

                        if (parentPoint >= componentStart || componentPoint >= componentPointCount)
                        {
                            // Out of range, or references a phantom point this reader does not
                            // materialise — bail rather than place the component incorrectly.
                            return false;
                        }

                        offset = outline.GetPoint(parentPoint) - outline.GetPoint(componentStart + componentPoint);
                    }

                    outline.TranslateRange(componentStart, outline.PointCount, offset);
                }

                if (outline.PointCount == 0)
                {
                    return false;
                }

                EmitResolvedOutline(outline, transform, context);

                return true;
            }
            finally
            {
                outline.Dispose();
            }
        }

        /// <summary>
        /// Appends a simple glyph's points (transformed by <paramref name="transform"/>) and contour
        /// boundaries to <paramref name="outline"/>. Returns <see langword="false"/> for a component
        /// that is itself composite (nested point matching is not supported yet); an empty glyph
        /// contributes no points and returns <see langword="true"/>.
        /// </summary>
        private bool TryResolveSimpleGlyphPoints(int glyphIndex, Matrix transform, GlyphDecycler decycler, ResolvedOutline outline)
        {
            using var guard = decycler.Enter(glyphIndex);

            if (!TryGetGlyphData(glyphIndex, out var glyphData) || glyphData.IsEmpty)
            {
                return true;
            }

            var descriptor = new GlyphDescriptor(glyphData);

            if (!descriptor.IsSimpleGlyph)
            {
                return false;
            }

            var simpleGlyph = descriptor.SimpleGlyph;

            try
            {
                var ends = simpleGlyph.EndPtsOfContours;
                var flags = simpleGlyph.Flags;
                var xCoords = simpleGlyph.XCoordinates;
                var yCoords = simpleGlyph.YCoordinates;

                var start = 0;

                for (var contourIndex = 0; contourIndex < ends.Length; contourIndex++)
                {
                    int end = ends[contourIndex];

                    for (var i = start; i <= end; i++)
                    {
                        var point = transform.Transform(new Point(xCoords[i], yCoords[i]));
                        outline.AddPoint(point, (flags[i] & GlyphFlag.OnCurvePoint) != 0);
                    }

                    outline.EndContour();
                    start = end + 1;
                }

                return true;
            }
            finally
            {
                simpleGlyph.Dispose();
            }
        }

        /// <summary>
        /// Builds the 2x2 scale/transform of a composite component (without any translation; the
        /// placement offset is applied separately).
        /// </summary>
        private static Matrix CreateComponentScale(GlyphComponent component)
        {
            var flags = component.Flags;

            double m11, m12, m21, m22;

            if ((flags & CompositeFlags.WeHaveAScale) != 0)
            {
                m11 = m22 = component.Scale;
                m12 = m21 = 0;
            }
            else if ((flags & CompositeFlags.WeHaveAnXAndYScale) != 0)
            {
                m11 = component.ScaleX;
                m22 = component.ScaleY;
                m12 = m21 = 0;
            }
            else if ((flags & CompositeFlags.WeHaveATwoByTwo) != 0)
            {
                m11 = component.ScaleX;
                m12 = component.Scale01;
                m21 = component.Scale10;
                m22 = component.ScaleY;
            }
            else
            {
                m11 = m22 = 1.0;
                m12 = m21 = 0;
            }

            return new Matrix(m11, m12, m21, m22, 0, 0);
        }

        /// <summary>
        /// Emits the contours of a materialised outline to the geometry context, applying
        /// <paramref name="transform"/>, via the shared <see cref="EmitContour"/> walker.
        /// </summary>
        private static void EmitResolvedOutline(ResolvedOutline outline, Matrix transform, IGeometryContext context)
        {
            var points = outline.Points;
            var onCurve = outline.OnCurve;
            var contourEnds = outline.ContourEnds;

            var startPointIndex = 0;

            for (var contourIndex = 0; contourIndex < contourEnds.Length; contourIndex++)
            {
                var endPointIndex = contourEnds[contourIndex];
                var pointCount = endPointIndex - startPointIndex + 1;

                if (pointCount > 0)
                {
                    EmitContour(
                        points.Slice(startPointIndex, pointCount),
                        onCurve.Slice(startPointIndex, pointCount),
                        transform,
                        context);
                }

                startPointIndex = endPointIndex + 1;
            }
        }

        /// <summary>
        /// A growable, pooled buffer of resolved (transformed) outline points used only by the
        /// point-matching composite path. Backing arrays are rented from <see cref="ArrayPool{T}"/>
        /// and returned on <see cref="Dispose"/>.
        /// </summary>
        private sealed class ResolvedOutline : IDisposable
        {
            private Point[] _points;
            private bool[] _onCurve;
            private int[] _contourEnds;

            public ResolvedOutline(int capacity)
            {
                _points = ArrayPool<Point>.Shared.Rent(capacity);
                _onCurve = ArrayPool<bool>.Shared.Rent(capacity);
                _contourEnds = ArrayPool<int>.Shared.Rent(16);
            }

            public int PointCount { get; private set; }

            public int ContourCount { get; private set; }

            public ReadOnlySpan<Point> Points => _points.AsSpan(0, PointCount);

            public ReadOnlySpan<bool> OnCurve => _onCurve.AsSpan(0, PointCount);

            public ReadOnlySpan<int> ContourEnds => _contourEnds.AsSpan(0, ContourCount);

            public Point GetPoint(int index) => _points[index];

            public void AddPoint(Point point, bool onCurve)
            {
                if (PointCount >= _points.Length)
                {
                    Grow(ref _points, PointCount);
                    Grow(ref _onCurve, PointCount);
                }

                _points[PointCount] = point;
                _onCurve[PointCount] = onCurve;
                PointCount++;
            }

            public void EndContour()
            {
                if (ContourCount >= _contourEnds.Length)
                {
                    Grow(ref _contourEnds, ContourCount);
                }

                _contourEnds[ContourCount++] = PointCount - 1;
            }

            public void TranslateRange(int fromInclusive, int toExclusive, Vector offset)
            {
                for (var i = fromInclusive; i < toExclusive; i++)
                {
                    _points[i] += offset;
                }
            }

            private static void Grow<T>(ref T[] array, int count)
            {
                var bigger = ArrayPool<T>.Shared.Rent(array.Length * 2);
                array.AsSpan(0, count).CopyTo(bigger);
                ArrayPool<T>.Shared.Return(array);
                array = bigger;
            }

            public void Dispose()
            {
                if (_points != null)
                {
                    ArrayPool<Point>.Shared.Return(_points);
                    _points = null!;
                }

                if (_onCurve != null)
                {
                    ArrayPool<bool>.Shared.Return(_onCurve);
                    _onCurve = null!;
                }

                if (_contourEnds != null)
                {
                    ArrayPool<int>.Shared.Return(_contourEnds);
                    _contourEnds = null!;
                }
            }
        }

        /// <summary>
        /// Wrapper that applies a matrix transform to coordinates before delegating to the real context.
        /// </summary>
        private sealed class TransformingGeometryContext : IGeometryContext
        {
            private readonly IGeometryContext _inner;
            private readonly Matrix _matrix;

            public TransformingGeometryContext(IGeometryContext inner, Matrix matrix)
            {
                _inner = inner;
                _matrix = matrix;
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
            {
                _inner.ArcTo(_matrix.Transform(point), size, rotationAngle, isLargeArc, sweepDirection, isStroked);
            }

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                _inner.BeginFigure(_matrix.Transform(startPoint), isFilled);
            }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
            {
                _inner.CubicBezierTo(_matrix.Transform(controlPoint1), _matrix.Transform(controlPoint2), _matrix.Transform(endPoint), isStroked);
            }

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
            {
                _inner.QuadraticBezierTo(_matrix.Transform(controlPoint), _matrix.Transform(endPoint), isStroked);
            }

            public void LineTo(Point endPoint, bool isStroked = true)
            {
                _inner.LineTo(_matrix.Transform(endPoint), isStroked);
            }

            public void EndFigure(bool isClosed)
            {
                _inner.EndFigure(isClosed);
            }

            public void SetFillRule(FillRule fillRule)
            {
                _inner.SetFillRule(fillRule);
            }

            public void Dispose()
            {
            }
        }
    }
}
