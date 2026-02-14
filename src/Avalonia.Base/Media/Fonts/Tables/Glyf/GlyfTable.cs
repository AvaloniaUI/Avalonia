using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Avalonia.Platform;
using Avalonia.Logging;
using Avalonia.Utilities;

using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Colr;

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
        /// Builds the glyph outline into the provided geometry context. Returns false for empty glyphs.
        /// Coordinates are in font design units. Composite glyphs are supported.
        /// </summary>
        public bool TryBuildGlyphGeometry(int glyphIndex, Matrix transform, IGeometryContext context)
        {
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

                var startPointIndex = 0;

                for (var contourIndex = 0; contourIndex < endPtsOfContours.Length; contourIndex++)
                {
                    var endPointIndex = endPtsOfContours[contourIndex];
                    var pointCount = endPointIndex - startPointIndex + 1;

                    if (pointCount == 0)
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
                        var firstPoint = new Point(xCoords[startPointIndex], yCoords[startPointIndex]);
                        context.BeginFigure(transform.Transform(firstPoint), true);

                        // Start processing from the next point (or wrap to first if only one point)
                        int i = pointCount == 1 ? startPointIndex : startPointIndex + 1;
                        int processingStartIndex = i;

                        var maxSegments = Math.Max(1, pointCount * 3);
                        var segmentsProcessed = 0;

                        while (segmentsProcessed++ < maxSegments)
                        {
                            // Wrap index to contour range
                            int currentIdx = startPointIndex + ((i - startPointIndex) % pointCount);

                            var curFlag = flags[currentIdx];
                            var curIsOnCurve = (curFlag & GlyphFlag.OnCurvePoint) != 0;
                            var curPoint = new Point(xCoords[currentIdx], yCoords[currentIdx]);

                            if (curIsOnCurve)
                            {
                                // Simple line to on-curve point
                                context.LineTo(transform.Transform(curPoint));
                                i++;
                            }
                            else
                            {
                                // Current is off-curve, look ahead
                                int nextIdx = startPointIndex + ((i + 1 - startPointIndex) % pointCount);
                                var nextFlag = flags[nextIdx];
                                var nextIsOnCurve = (nextFlag & GlyphFlag.OnCurvePoint) != 0;
                                var nextPoint = new Point(xCoords[nextIdx], yCoords[nextIdx]);

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
                        // First point is off-curve -> create implied start between last and first
                        var lastIdx = endPointIndex;
                        var firstX = xCoords[startPointIndex];
                        var firstY = yCoords[startPointIndex];
                        var lastX = xCoords[lastIdx];
                        var lastY = yCoords[lastIdx];

                        var impliedStartX = (lastX + firstX) / 2.0;
                        var impliedStartY = (lastY + firstY) / 2.0;
                        var impliedStart = new Point(impliedStartX, impliedStartY);

                        context.BeginFigure(transform.Transform(impliedStart), true);

                        int idxWalker = 0; // offset from startPointIndex
                        var maxSegments = pointCount * 3;
                        var segmentsProcessed = 0;

                        while (segmentsProcessed++ < maxSegments)
                        {
                            int curIdx = startPointIndex + idxWalker;
                            int nextIdxOffset = idxWalker == pointCount - 1 ? 0 : idxWalker + 1;
                            int nextIdx = startPointIndex + nextIdxOffset;

                            var curFlag = flags[curIdx];
                            var curIsOnCurve = (curFlag & GlyphFlag.OnCurvePoint) != 0;
                            var curPoint = new Point(xCoords[curIdx], yCoords[curIdx]);

                            if (curIsOnCurve)
                            {
                                context.LineTo(transform.Transform(curPoint));
                                idxWalker = nextIdxOffset;
                            }
                            else
                            {
                                var nextFlag = flags[nextIdx];
                                var nextIsOnCurve = (nextFlag & GlyphFlag.OnCurvePoint) != 0;
                                var nextPoint = new Point(xCoords[nextIdx], yCoords[nextIdx]);

                                if (nextIsOnCurve)
                                {
                                    context.QuadraticBezierTo(
                                        transform.Transform(curPoint),
                                        transform.Transform(nextPoint)
                                    );
                                    idxWalker = nextIdxOffset == pointCount - 1 ? 0 : nextIdxOffset + 1;
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
                                    idxWalker = nextIdxOffset == pointCount - 1 ? 0 : nextIdxOffset + 1;
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
                // Return rented buffers to pool
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

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "ArcTo {0} {1} rot={2} large={3} sweep={4}", point, size, rotationAngle, isLargeArc, sweepDirection);
                }

                var tp = _matrix.Transform(point);

                _inner.ArcTo(tp, size, rotationAngle, isLargeArc, sweepDirection);
            }

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "BeginFigure {0} filled={1}", startPoint, isFilled);
                }

                var tp = _matrix.Transform(startPoint);

                _inner.BeginFigure(tp, isFilled);
            }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "CubicBezierTo cp1={0} cp2={1} end={2}", controlPoint1, controlPoint2, endPoint);
                }

                _inner.CubicBezierTo(_matrix.Transform(controlPoint1), _matrix.Transform(controlPoint2), _matrix.Transform(endPoint));
            }

            public void QuadraticBezierTo(Point controlPoint, Point endPoint)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "QuadraticBezierTo cp={0} end={1}", controlPoint, endPoint);
                }

                _inner.QuadraticBezierTo(_matrix.Transform(controlPoint), _matrix.Transform(endPoint));
            }

            public void LineTo(Point endPoint)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "LineTo {0}", endPoint);
                }

                _inner.LineTo(_matrix.Transform(endPoint));
            }

            public void EndFigure(bool isClosed)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "EndFigure closed={0}", isClosed);
                }

                _inner.EndFigure(isClosed);
            }

            public void SetFillRule(FillRule fillRule)
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "SetFillRule {0}", fillRule);
                }

                _inner.SetFillRule(fillRule);
            }

            public void Dispose()
            {
                if (Logger.TryGet(LogEventLevel.Debug, LogArea.Visual, out var log))
                {
                    log.Log(_inner, "Dispose TransformingGeometryContext");
                }
            }
        }
    }
}
