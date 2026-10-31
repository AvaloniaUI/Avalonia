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
                    // Short format: uint16 values stored divided by 2
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
        /// Resolves the gvar deltas of the phantom points that carry a glyph's varied metrics.
        /// </summary>
        /// <remarks>
        /// A composite glyph with a USE_MY_METRICS component takes that component's phantom
        /// points instead of its own, recursively, so its metrics come from the component's
        /// hmtx / vmtx entry and the component's own deltas. When several components carry the
        /// flag the last one wins, as in FreeType and HarfBuzz.
        /// </remarks>
        /// <param name="glyphIndex">The glyph to resolve.</param>
        /// <param name="gvarTable">
        /// The font's gvar table. <c>null</c>, or an empty <paramref name="activeCoords"/>, only
        /// resolves <paramref name="metricsGlyphIndex"/> and leaves the deltas at zero.
        /// </param>
        /// <param name="activeCoords">Normalized variation coordinates in fvar axis order.</param>
        /// <param name="metricsGlyphIndex">
        /// The glyph whose hmtx / vmtx entry the deltas apply to: <paramref name="glyphIndex"/>
        /// itself unless a USE_MY_METRICS component supplies the metrics.
        /// </param>
        /// <param name="xDeltas">
        /// Output: x deltas of the four phantom points (left side bearing, advance width, top
        /// origin, advance height). Must be zero-initialized; only added to.
        /// </param>
        /// <param name="yDeltas">Output: y deltas of the four phantom points, as <paramref name="xDeltas"/>.</param>
        /// <returns>
        /// <c>false</c> when the glyph is out of range, malformed, or its USE_MY_METRICS chain
        /// does not end within <see cref="GlyphDecycler.MaxTraversalDepth"/> steps.
        /// </returns>
        public bool TryGetPhantomDeltas(
            int glyphIndex,
            GvarTable? gvarTable,
            ReadOnlySpan<float> activeCoords,
            out int metricsGlyphIndex,
            Span<float> xDeltas,
            Span<float> yDeltas)
        {
            metricsGlyphIndex = glyphIndex;

            for (var depth = 0; depth < GlyphDecycler.MaxTraversalDepth; depth++)
            {
                if (!TryGetPhantomPointOwner(metricsGlyphIndex, out var pointCount, out var metricsComponent))
                {
                    return false;
                }

                if (metricsComponent < 0)
                {
                    if (gvarTable is not null && !activeCoords.IsEmpty)
                    {
                        GlyphVariationReader.TryGetPhantomDeltas(
                            gvarTable, metricsGlyphIndex, activeCoords, pointCount, xDeltas, yDeltas);
                    }

                    return true;
                }

                metricsGlyphIndex = metricsComponent;
            }

            return false;
        }

        /// <summary>
        /// Reads the number of points that precede a glyph's phantom points in gvar, and the
        /// USE_MY_METRICS component of a composite glyph (-1 when there is none).
        /// </summary>
        private bool TryGetPhantomPointOwner(int glyphIndex, out int pointCount, out int metricsComponent)
        {
            pointCount = 0;
            metricsComponent = -1;

            if (!TryGetGlyphData(glyphIndex, out var data))
            {
                return false;
            }

            if (data.IsEmpty)
            {
                // An empty glyph (e.g. whitespace) has only its phantom points.
                return true;
            }

            var span = data.Span;

            if (span.Length < 10)
            {
                return false;
            }

            var numberOfContours = BinaryPrimitives.ReadInt16BigEndian(span);

            if (numberOfContours >= 0)
            {
                if (numberOfContours > 0)
                {
                    // The last contour end point is the highest point index.
                    var lastEndPointOffset = 10 + (numberOfContours - 1) * 2;

                    if (lastEndPointOffset + 2 > span.Length)
                    {
                        return false;
                    }

                    pointCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(lastEndPointOffset)) + 1;
                }

                return true;
            }

            try
            {
                var compositeGlyph = CompositeGlyph.Create(span.Slice(10));

                try
                {
                    var components = compositeGlyph.Components;
                    pointCount = components.Length;

                    foreach (var component in components)
                    {
                        if ((component.Flags & CompositeFlags.UseMyMetrics) != 0)
                        {
                            metricsComponent = component.GlyphIndex;
                        }
                    }
                }
                finally
                {
                    compositeGlyph.Dispose();
                }

                return true;
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                // A component record runs past the end of the glyph data: word fields fail their
                // slice, byte arguments their indexer.
                return false;
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
            bool[]? onCurveRented = null;
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

                // Build the deformed Point[] once. The shared EmitContour walker only sees
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

                onCurveRented = ArrayPool<bool>.Shared.Rent(pointCount);
                var onCurve = onCurveRented.AsSpan(0, pointCount);

                for (var i = 0; i < pointCount; i++)
                {
                    onCurve[i] = (flags[i] & GlyphFlag.OnCurvePoint) != 0;
                }

                var startPointIndex = 0;

                for (var contourIndex = 0; contourIndex < endPtsOfContours.Length; contourIndex++)
                {
                    var endPointIndex = endPtsOfContours[contourIndex];
                    var contourPointCount = endPointIndex - startPointIndex + 1;

                    EmitContour(
                        points.Slice(startPointIndex, contourPointCount),
                        onCurve.Slice(startPointIndex, contourPointCount),
                        transform,
                        context);

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
                if (onCurveRented != null)
                {
                    ArrayPool<bool>.Shared.Return(onCurveRented);
                }
                // Return SimpleGlyph's rented short buffers to pool
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
        /// <param name="offsetDelta">The variation delta for the component's x/y offset.</param>
        /// <returns>A transformation matrix that should be applied to the component glyph.</returns>
        private static Matrix CreateComponentTransform(GlyphComponent component, Vector offsetDelta)
        {
            var flags = component.Flags;

            double tx = 0, ty = 0;

            if ((flags & CompositeFlags.ArgsAreXYValues) != 0)
            {
                tx = component.Arg1 + offsetDelta.X;
                ty = component.Arg2 + offsetDelta.Y;
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
                return BuildCompositeGlyphGeometry(descriptor.CompositeGlyph, glyphIndex, context, transform, decycler, gvarTable, activeCoords);
            }
        }

        /// <summary>
        /// Builds the geometry for a composite glyph by recursively processing its components.
        /// </summary>
        /// <param name="compositeGlyph">The composite glyph containing component references and transformations.</param>
        /// <param name="glyphIndex">The composite glyph's index in the font. Used to look up its gvar entry.</param>
        /// <param name="context">The geometry context that receives the constructed glyph geometry.</param>
        /// <param name="transform">The transformation matrix to apply to all component glyphs.</param>
        /// <param name="decycler">A <see cref="GlyphDecycler"/> instance used to prevent infinite recursion when building composite glyphs.</param>
        /// <param name="gvarTable">
        /// Optional gvar table. The composite's own entry moves the component offsets, and each
        /// component glyph applies its own entry to its outline.
        /// </param>
        /// <param name="activeCoords">Normalized variation coordinates in fvar axis order.</param>
        /// <returns>true if at least one component was successfully processed; otherwise, false.</returns>
        private bool BuildCompositeGlyphGeometry(
            CompositeGlyph compositeGlyph,
            int glyphIndex,
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
                    return BuildPointMatchedComposite(components, glyphIndex, context, transform, decycler, gvarTable, activeCoords);
                }

                using var offsetDeltas = ComponentOffsetDeltas.Compute(glyphIndex, components, gvarTable, activeCoords);

                var hasGeometry = false;

                for (var i = 0; i < components.Length; i++)
                {
                    var component = components[i];
                    var componentTransform = CreateComponentTransform(component, offsetDeltas[i]);
                    var combinedTransform = componentTransform * transform;

                    var wrappedContext = new TransformingGeometryContext(context, combinedTransform);

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
        /// The variation deltas a composite glyph's own gvar entry applies to its component offsets.
        /// </summary>
        /// <remarks>
        /// In a composite's gvar entry each component is one point whose coordinates are its x/y
        /// offset, followed by the four phantom points. Each component point forms a contour of its
        /// own, so a component the entry does not reference keeps a zero delta instead of inheriting
        /// one through interpolation. Point-matched components have no offset to move and always
        /// report a zero delta; they follow the varied points they are matched onto.
        /// </remarks>
        private readonly ref struct ComponentOffsetDeltas
        {
            private readonly float[]? _x;
            private readonly float[]? _y;

            private ComponentOffsetDeltas(float[] x, float[] y)
            {
                _x = x;
                _y = y;
            }

            public Vector this[int componentIndex]
                => _x is null ? default : new Vector(_x[componentIndex], _y![componentIndex]);

            public static ComponentOffsetDeltas Compute(
                int glyphIndex,
                ReadOnlySpan<GlyphComponent> components,
                GvarTable? gvarTable,
                ReadOnlySpan<float> activeCoords)
            {
                if (gvarTable is null || activeCoords.IsEmpty)
                {
                    return default;
                }

                var count = components.Length;
                var endPoints = ArrayPool<ushort>.Shared.Rent(count);
                var originalX = ArrayPool<short>.Shared.Rent(count);
                var originalY = ArrayPool<short>.Shared.Rent(count);
                var deltaX = ArrayPool<float>.Shared.Rent(count);
                var deltaY = ArrayPool<float>.Shared.Rent(count);

                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var component = components[i];
                        var isOffset = (component.Flags & CompositeFlags.ArgsAreXYValues) != 0;

                        endPoints[i] = (ushort)i;
                        originalX[i] = isOffset ? component.Arg1 : (short)0;
                        originalY[i] = isOffset ? component.Arg2 : (short)0;
                        deltaX[i] = 0;
                        deltaY[i] = 0;
                    }

                    GlyphVariationReader.TryApplyDeltas(
                        gvarTable, glyphIndex, activeCoords,
                        endPoints.AsSpan(0, count), originalX.AsSpan(0, count), originalY.AsSpan(0, count),
                        deltaX.AsSpan(0, count), deltaY.AsSpan(0, count));

                    for (var i = 0; i < count; i++)
                    {
                        if ((components[i].Flags & CompositeFlags.ArgsAreXYValues) == 0)
                        {
                            deltaX[i] = 0;
                            deltaY[i] = 0;
                        }
                    }

                    return new ComponentOffsetDeltas(deltaX, deltaY);
                }
                finally
                {
                    ArrayPool<ushort>.Shared.Return(endPoints);
                    ArrayPool<short>.Shared.Return(originalX);
                    ArrayPool<short>.Shared.Return(originalY);
                }
            }

            public void Dispose()
            {
                if (_x is not null)
                {
                    ArrayPool<float>.Shared.Return(_x);
                    ArrayPool<float>.Shared.Return(_y!);
                }
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
            int glyphIndex,
            IGeometryContext context,
            Matrix transform,
            GlyphDecycler decycler,
            GvarTable? gvarTable,
            ReadOnlySpan<float> activeCoords)
        {
            var outline = new ResolvedOutline(64);
            using var offsetDeltas = ComponentOffsetDeltas.Compute(glyphIndex, components, gvarTable, activeCoords);

            try
            {
                for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    var component = components[componentIndex];
                    var componentStart = outline.PointCount;

                    // Resolve the component's varied points into composite space with its 2x2
                    // scale applied (but not the placement offset, which is computed next), so a
                    // later component is matched onto the points as they are at this instance.
                    if (!TryResolveSimpleGlyphPoints(
                            component.GlyphIndex, CreateComponentScale(component), decycler, outline, gvarTable, activeCoords))
                    {
                        return false;
                    }

                    Vector offset;

                    if ((component.Flags & CompositeFlags.ArgsAreXYValues) != 0)
                    {
                        // Signed x/y offset (the unscaled-offset default), moved by the
                        // composite's own variation deltas.
                        offset = new Vector(component.Arg1, component.Arg2) + offsetDeltas[componentIndex];
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
        private bool TryResolveSimpleGlyphPoints(
            int glyphIndex,
            Matrix transform,
            GlyphDecycler decycler,
            ResolvedOutline outline,
            GvarTable? gvarTable,
            ReadOnlySpan<float> activeCoords)
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
            float[]? deltaXRented = null;
            float[]? deltaYRented = null;

            try
            {
                var ends = simpleGlyph.EndPtsOfContours;
                var flags = simpleGlyph.Flags;
                var xCoords = simpleGlyph.XCoordinates;
                var yCoords = simpleGlyph.YCoordinates;
                var pointCount = xCoords.Length;

                if (gvarTable is not null && !activeCoords.IsEmpty)
                {
                    deltaXRented = ArrayPool<float>.Shared.Rent(pointCount);
                    deltaYRented = ArrayPool<float>.Shared.Rent(pointCount);
                    deltaXRented.AsSpan(0, pointCount).Clear();
                    deltaYRented.AsSpan(0, pointCount).Clear();

                    GlyphVariationReader.TryApplyDeltas(
                        gvarTable, glyphIndex, activeCoords,
                        ends, xCoords, yCoords,
                        deltaXRented.AsSpan(0, pointCount), deltaYRented.AsSpan(0, pointCount));
                }

                var start = 0;

                for (var contourIndex = 0; contourIndex < ends.Length; contourIndex++)
                {
                    int end = ends[contourIndex];

                    for (var i = start; i <= end; i++)
                    {
                        var x = xCoords[i] + (deltaXRented?[i] ?? 0f);
                        var y = yCoords[i] + (deltaYRented?[i] ?? 0f);
                        var point = transform.Transform(new Point(x, y));
                        outline.AddPoint(point, (flags[i] & GlyphFlag.OnCurvePoint) != 0);
                    }

                    outline.EndContour();
                    start = end + 1;
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
