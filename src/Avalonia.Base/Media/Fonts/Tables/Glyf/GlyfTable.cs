using System;
using System.Buffers;
using System.Buffers.Binary;
using Avalonia.Platform;
using Avalonia.Logging;
using Avalonia.Media.Fonts.Tables.Variation;

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
        /// <param name="glyphIds">The glyph indices to read.</param>
        /// <param name="bounds">Output; must be at least as long as <paramref name="glyphIds"/>.</param>
        public void GetGlyphBounds(ReadOnlySpan<ushort> glyphIds, Span<GlyphBounds> bounds)
        {
            var glyf = _glyfData.Span;
            var loca = _locaTable.RawData;
            var shortFormat = _locaTable.IsShortFormat;
            var glyphCount = _locaTable.GlyphCount;
            var entrySize = shortFormat ? 2 : 4;

            for (var i = 0; i < glyphIds.Length; i++)
            {
                bounds[i] = default;

                int gid = glyphIds[i];

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
        /// <param name="glyphIndex">The index of the glyph to render.</param>
        /// <param name="transform">Transform applied to every emitted point.</param>
        /// <param name="context">Geometry context that receives the contour commands.</param>
        /// <param name="gvarTable">
        /// Optional gvar table for variation deformation. When non-null and
        /// <paramref name="activeCoords"/> is non-empty, each glyph's contour points are
        /// deformed via gvar before being emitted.
        /// </param>
        /// <param name="activeCoords">
        /// Normalized variation coordinates in fvar axis order. Empty (default) means
        /// "no variation requested" — the table is consulted only when both this span
        /// and <paramref name="gvarTable"/> are present.
        /// </param>
        public bool TryBuildGlyphGeometry(
            int glyphIndex,
            Matrix transform,
            IGeometryContext context,
            GvarTable? gvarTable = null,
            ReadOnlySpan<float> activeCoords = default)
        {
            // TrueType outlines use the non-zero winding rule. The default geometry fill
            // rule in Avalonia is EvenOdd, which would XOR overlapping contours (e.g. the
            // crossbar and diagonal strokes of 'A', or composites where an accent overlaps
            // its base glyph) and leave gaps where they intersect.
            context.SetFillRule(FillRule.NonZero);

            var decycler = GlyphDecycler.Rent();

            try
            {
                return TryBuildGlyphGeometryInternal(glyphIndex, context, transform, decycler, gvarTable, activeCoords);
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
        /// Builds the geometry for a simple glyph. Applies gvar deformation first (when
        /// <paramref name="gvarTable"/> and <paramref name="activeCoords"/> are both
        /// provided), then emits contour commands from the deformed control points.
        /// </summary>
        /// <param name="simpleGlyph">The simple glyph containing contour data, flags, and coordinates.</param>
        /// <param name="context">The geometry context that receives the constructed glyph geometry.</param>
        /// <param name="transform">The transformation matrix to apply to all coordinates.</param>
        /// <param name="glyphIndex">The glyph's index in the font. Used to look up its gvar entry.</param>
        /// <param name="gvarTable">Optional gvar table. <c>null</c> skips deformation.</param>
        /// <param name="activeCoords">Normalized variation coordinates (fvar order).</param>
        /// <returns>true if the glyph geometry was successfully built; otherwise, false.</returns>
        private static bool BuildSimpleGlyphGeometry(
            SimpleGlyph simpleGlyph,
            IGeometryContext context,
            Matrix transform,
            int glyphIndex,
            GvarTable? gvarTable,
            ReadOnlySpan<float> activeCoords)
        {
            Point[]? pointsRented = null;
            float[]? deltaXRented = null;
            float[]? deltaYRented = null;

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
                var pointCount = xCoords.Length;

                // Build the deformed Point[] once. The contour walker (below) only sees
                // points[i] — the variation-vs-no-variation branch happens here and the
                // walker is shared. For unvaried glyphs we still pay the Point-array
                // construction cost (a memcpy-like loop), but the prior implementation
                // constructed a fresh Point per indexed access anyway, so this isn't a
                // regression at the call-rate of GetGlyphOutline (one call per glyph,
                // not per pixel).
                pointsRented = ArrayPool<Point>.Shared.Rent(pointCount);
                var points = pointsRented.AsSpan(0, pointCount);

                if (gvarTable is not null && !activeCoords.IsEmpty)
                {
                    deltaXRented = ArrayPool<float>.Shared.Rent(pointCount);
                    deltaYRented = ArrayPool<float>.Shared.Rent(pointCount);
                    var deltaX = deltaXRented.AsSpan(0, pointCount);
                    var deltaY = deltaYRented.AsSpan(0, pointCount);
                    deltaX.Clear();
                    deltaY.Clear();

                    GlyphVariationReader.TryApplyDeltas(
                        gvarTable, glyphIndex, activeCoords,
                        endPtsOfContours, xCoords, yCoords,
                        deltaX, deltaY);

                    for (var i = 0; i < pointCount; i++)
                    {
                        points[i] = new Point(xCoords[i] + deltaX[i], yCoords[i] + deltaY[i]);
                    }
                }
                else
                {
                    for (var i = 0; i < pointCount; i++)
                    {
                        points[i] = new Point(xCoords[i], yCoords[i]);
                    }
                }

                var startPointIndex = 0;

                for (var contourIndex = 0; contourIndex < endPtsOfContours.Length; contourIndex++)
                {
                    var endPointIndex = endPtsOfContours[contourIndex];
                    var contourPointCount = endPointIndex - startPointIndex + 1;

                    if (contourPointCount == 0)
                    {
                        startPointIndex = endPointIndex + 1;
                        continue;
                    }

                    // Check if first point is on-curve
                    var firstFlag = flags[startPointIndex];
                    var firstIsOnCurve = (firstFlag & GlyphFlag.OnCurvePoint) != 0;

                    if (firstIsOnCurve)
                    {
                        // Normal case: start from first on-curve point
                        context.BeginFigure(transform.Transform(points[startPointIndex]), true);

                        // Start processing from the next point (or wrap to first if only one point)
                        int i = contourPointCount == 1 ? startPointIndex : startPointIndex + 1;
                        int processingStartIndex = i;

                        var maxSegments = Math.Max(1, contourPointCount * 3);
                        var segmentsProcessed = 0;

                        while (segmentsProcessed++ < maxSegments)
                        {
                            // Wrap index to contour range
                            int currentIdx = startPointIndex + ((i - startPointIndex) % contourPointCount);

                            var curFlag = flags[currentIdx];
                            var curIsOnCurve = (curFlag & GlyphFlag.OnCurvePoint) != 0;
                            var curPoint = points[currentIdx];

                            if (curIsOnCurve)
                            {
                                // Simple line to on-curve point
                                context.LineTo(transform.Transform(curPoint));
                                i++;
                            }
                            else
                            {
                                // Current is off-curve, look ahead
                                int nextIdx = startPointIndex + ((i + 1 - startPointIndex) % contourPointCount);
                                var nextFlag = flags[nextIdx];
                                var nextIsOnCurve = (nextFlag & GlyphFlag.OnCurvePoint) != 0;
                                var nextPoint = points[nextIdx];

                                if (nextIsOnCurve)
                                {
                                    // Quadratic curve to next on-curve point
                                    context.QuadraticBezierTo(
                                        transform.Transform(curPoint),
                                        transform.Transform(nextPoint)
                                    );
                                    // Advance past the on-curve point
                                    i += 2;
                                }
                                else
                                {
                                    // Two consecutive off-curve points -> implied midpoint
                                    var impliedX = (curPoint.X + nextPoint.X) / 2.0;
                                    var impliedY = (curPoint.Y + nextPoint.Y) / 2.0;
                                    var impliedPoint = new Point(impliedX, impliedY);

                                    context.QuadraticBezierTo(
                                        transform.Transform(curPoint),
                                        transform.Transform(impliedPoint)
                                    );
                                    // Advance to the second off-curve point for next iteration
                                    i++;
                                }
                            }

                            // Check if we've wrapped back to start
                            int checkIdx = startPointIndex + ((i - startPointIndex) % contourPointCount);
                            if (checkIdx == processingStartIndex && segmentsProcessed > 0)
                            {
                                break;
                            }
                        }

                        context.EndFigure(true);
                    }
                    else
                    {
                        // First point is off-curve -> create implied start between last and first
                        var lastIdx = endPointIndex;
                        var first = points[startPointIndex];
                        var last = points[lastIdx];

                        var impliedStartX = (last.X + first.X) / 2.0;
                        var impliedStartY = (last.Y + first.Y) / 2.0;
                        var impliedStart = new Point(impliedStartX, impliedStartY);

                        context.BeginFigure(transform.Transform(impliedStart), true);

                        int idxWalker = 0; // offset from startPointIndex
                        var maxSegments = contourPointCount * 3;
                        var segmentsProcessed = 0;

                        while (segmentsProcessed++ < maxSegments)
                        {
                            int curIdx = startPointIndex + idxWalker;
                            int nextIdxOffset = idxWalker == contourPointCount - 1 ? 0 : idxWalker + 1;
                            int nextIdx = startPointIndex + nextIdxOffset;

                            var curFlag = flags[curIdx];
                            var curIsOnCurve = (curFlag & GlyphFlag.OnCurvePoint) != 0;
                            var curPoint = points[curIdx];

                            if (curIsOnCurve)
                            {
                                context.LineTo(transform.Transform(curPoint));
                                idxWalker = nextIdxOffset;
                            }
                            else
                            {
                                var nextFlag = flags[nextIdx];
                                var nextIsOnCurve = (nextFlag & GlyphFlag.OnCurvePoint) != 0;
                                var nextPoint = points[nextIdx];

                                if (nextIsOnCurve)
                                {
                                    context.QuadraticBezierTo(
                                        transform.Transform(curPoint),
                                        transform.Transform(nextPoint)
                                    );
                                    idxWalker = nextIdxOffset == contourPointCount - 1 ? 0 : nextIdxOffset + 1;
                                }
                                else
                                {
                                    // Two consecutive off-curve points -> implied midpoint
                                    var impliedX = (curPoint.X + nextPoint.X) / 2.0;
                                    var impliedY = (curPoint.Y + nextPoint.Y) / 2.0;
                                    var impliedPoint = new Point(impliedX, impliedY);

                                    context.QuadraticBezierTo(
                                        transform.Transform(curPoint),
                                        transform.Transform(impliedPoint)
                                    );
                                    idxWalker = nextIdxOffset == contourPointCount - 1 ? 0 : nextIdxOffset + 1;
                                }
                            }

                            // Stop when we've wrapped back to the beginning
                            if (idxWalker == 0 && segmentsProcessed > 1)
                            {
                                break;
                            }
                        }

                        context.EndFigure(true);
                    }

                    startPointIndex = endPointIndex + 1;
                }

                return true;
            }
            finally
            {
                if (deltaXRented != null)
                {
                    ArrayPool<float>.Shared.Return(deltaXRented);
                }
                if (deltaYRented != null)
                {
                    ArrayPool<float>.Shared.Return(deltaYRented);
                }
                if (pointsRented != null)
                {
                    ArrayPool<Point>.Shared.Return(pointsRented);
                }
                // Return SimpleGlyph's rented short buffers to pool
                simpleGlyph.Dispose();
            }
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
        /// <param name="gvarTable">Optional gvar table for variation deformation. <c>null</c> skips deformation.</param>
        /// <param name="activeCoords">Normalized variation coordinates in fvar axis order. Empty span means no variation.</param>
        /// <returns>true if the glyph geometry was successfully built and added to the context; otherwise, false.</returns>
        private bool TryBuildGlyphGeometryInternal(
            int glyphIndex,
            IGeometryContext context,
            Matrix transform,
            GlyphDecycler decycler,
            GvarTable? gvarTable,
            ReadOnlySpan<float> activeCoords)
        {
            using var guard = decycler.Enter(glyphIndex);

            if (!TryGetGlyphData(glyphIndex, out var glyphData) || glyphData.IsEmpty)
            {
                return false;
            }

            var descriptor = new GlyphDescriptor(glyphData);

            if (descriptor.IsSimpleGlyph)
            {
                return BuildSimpleGlyphGeometry(descriptor.SimpleGlyph, context, transform, glyphIndex, gvarTable, activeCoords);
            }
            else
            {
                return BuildCompositeGlyphGeometry(descriptor.CompositeGlyph, context, transform, decycler, gvarTable, activeCoords);
            }
        }

        /// <summary>
        /// Builds the geometry for a composite glyph by recursively processing its components.
        /// </summary>
        /// <param name="compositeGlyph">The composite glyph containing component references and transformations.</param>
        /// <param name="context">The geometry context that receives the constructed glyph geometry.</param>
        /// <param name="transform">The transformation matrix to apply to all component glyphs.</param>
        /// <param name="decycler">A <see cref="GlyphDecycler"/> instance used to prevent infinite recursion when building composite glyphs.</param>
        /// <param name="gvarTable">Optional gvar table. Passed through to each child glyph for independent deformation.</param>
        /// <param name="activeCoords">Normalized variation coordinates in fvar axis order.</param>
        /// <returns>true if at least one component was successfully processed; otherwise, false.</returns>
        private bool BuildCompositeGlyphGeometry(
            CompositeGlyph compositeGlyph,
            IGeometryContext context,
            Matrix transform,
            GlyphDecycler decycler,
            GvarTable? gvarTable,
            ReadOnlySpan<float> activeCoords)
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

                    // Variation context propagates: each child glyph applies its own gvar
                    // entry independently. Composite-level gvar (which would deform the
                    // component offsets) is not yet applied — accented characters get
                    // correctly-thickened components but at the designer's default
                    // placement, which is a follow-up.
                    if (TryBuildGlyphGeometryInternal(component.GlyphIndex, wrappedContext, Matrix.Identity, decycler, gvarTable, activeCoords))
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
        /// <paramref name="transform"/>. Mirrors the on/off-curve walking of
        /// <see cref="BuildSimpleGlyphGeometry"/> but over an already-resolved point buffer.
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

                if (pointCount <= 0)
                {
                    startPointIndex = endPointIndex + 1;
                    continue;
                }

                if (onCurve[startPointIndex])
                {
                    context.BeginFigure(transform.Transform(points[startPointIndex]), true);

                    int i = pointCount == 1 ? startPointIndex : startPointIndex + 1;
                    int processingStartIndex = i;

                    var maxSegments = Math.Max(1, pointCount * 3);
                    var segmentsProcessed = 0;

                    while (segmentsProcessed++ < maxSegments)
                    {
                        int currentIdx = startPointIndex + ((i - startPointIndex) % pointCount);
                        var curPoint = points[currentIdx];

                        if (onCurve[currentIdx])
                        {
                            context.LineTo(transform.Transform(curPoint));
                            i++;
                        }
                        else
                        {
                            int nextIdx = startPointIndex + ((i + 1 - startPointIndex) % pointCount);
                            var nextPoint = points[nextIdx];

                            if (onCurve[nextIdx])
                            {
                                context.QuadraticBezierTo(transform.Transform(curPoint), transform.Transform(nextPoint));
                                i += 2;
                            }
                            else
                            {
                                var implied = new Point((curPoint.X + nextPoint.X) / 2.0, (curPoint.Y + nextPoint.Y) / 2.0);
                                context.QuadraticBezierTo(transform.Transform(curPoint), transform.Transform(implied));
                                i++;
                            }
                        }

                        int checkIdx = startPointIndex + ((i - startPointIndex) % pointCount);
                        if (checkIdx == processingStartIndex && segmentsProcessed > 0)
                        {
                            break;
                        }
                    }

                    context.EndFigure(true);
                }
                else
                {
                    var firstPoint = points[startPointIndex];
                    var lastPoint = points[endPointIndex];
                    var impliedStart = new Point((lastPoint.X + firstPoint.X) / 2.0, (lastPoint.Y + firstPoint.Y) / 2.0);

                    context.BeginFigure(transform.Transform(impliedStart), true);

                    int idxWalker = 0;
                    var maxSegments = pointCount * 3;
                    var segmentsProcessed = 0;

                    while (segmentsProcessed++ < maxSegments)
                    {
                        int curIdx = startPointIndex + idxWalker;
                        int nextIdxOffset = idxWalker == pointCount - 1 ? 0 : idxWalker + 1;
                        int nextIdx = startPointIndex + nextIdxOffset;

                        var curPoint = points[curIdx];

                        if (onCurve[curIdx])
                        {
                            context.LineTo(transform.Transform(curPoint));
                            idxWalker = nextIdxOffset;
                        }
                        else
                        {
                            var nextPoint = points[nextIdx];

                            if (onCurve[nextIdx])
                            {
                                context.QuadraticBezierTo(transform.Transform(curPoint), transform.Transform(nextPoint));
                                idxWalker = nextIdxOffset == pointCount - 1 ? 0 : nextIdxOffset + 1;
                            }
                            else
                            {
                                var implied = new Point((curPoint.X + nextPoint.X) / 2.0, (curPoint.Y + nextPoint.Y) / 2.0);
                                context.QuadraticBezierTo(transform.Transform(curPoint), transform.Transform(implied));
                                idxWalker = nextIdxOffset == pointCount - 1 ? 0 : nextIdxOffset + 1;
                            }
                        }

                        if (idxWalker == 0 && segmentsProcessed > 1)
                        {
                            break;
                        }
                    }

                    context.EndFigure(true);
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
