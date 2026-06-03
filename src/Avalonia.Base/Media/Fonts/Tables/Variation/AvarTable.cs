using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses the OpenType 'avar' (axis variations) table. Provides per-axis segment maps
    /// that remap normalized coordinates after the linear fvar normalization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// avar is the font designer's tool for compensating for non-perceptually-linear axes.
    /// The classic example is the weight axis: fvar's linear mapping makes "halfway between
    /// Regular and Black" land at wght=550, but the font designer might prefer that point to
    /// look like wght=600 visually. avar lets them bend the normalized coordinate so that
    /// e.g. a user request for 0.5 actually maps to 0.45 internally.
    /// </para>
    /// <para>
    /// The table is optional — many variable fonts ship without one and rely on the linear
    /// normalization. When present, every requested normalized coordinate goes through
    /// <see cref="Remap"/> before reaching gvar / HVAR / VVAR / MVAR.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>.
    /// This parser handles avar v1.0 (segment maps only). v2.0 adds axis-value-table
    /// extensions for variation-dependent default values; that's a follow-up if a use case
    /// surfaces.
    /// </para>
    /// </remarks>
    internal sealed class AvarTable
    {
        internal const string TableName = "avar";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const ushort SupportedMajorVersion = 1;

        // Per the spec, every segment map must include the three identity-mapped boundary
        // entries (-1, 0, +1). We don't require this in parsing — corrupt fonts that omit
        // them still get a reasonable result because Remap clamps + extrapolates — but it
        // matters for understanding what "zero entries" means: it means the entire table
        // was empty for this axis, not just the identity points.
        private readonly ImmutableArray<ImmutableArray<SegmentMapEntry>> _segmentMaps;

        private AvarTable(ImmutableArray<ImmutableArray<SegmentMapEntry>> segmentMaps)
        {
            _segmentMaps = segmentMaps;
        }

        /// <summary>
        /// Gets the number of axes the table provides segment maps for. Should equal the
        /// fvar axis count; the spec requires this and platforms differ on how strictly
        /// they enforce it.
        /// </summary>
        public int AxisCount => _segmentMaps.Length;

        /// <summary>
        /// Remaps a normalized axis coordinate through this axis's segment map.
        /// Returns <paramref name="normalizedCoordinate"/> unchanged when
        /// <paramref name="axisIndex"/> is out of range or when the axis has no segment
        /// map entries (degenerate avar — treat as identity).
        /// </summary>
        /// <param name="axisIndex">
        /// Zero-based axis index, matching the fvar axis order.
        /// </param>
        /// <param name="normalizedCoordinate">
        /// The linearly-normalized coordinate in <c>[-1, 1]</c> (the output of fvar
        /// normalization, before avar correction).
        /// </param>
        /// <returns>
        /// The avar-corrected normalized coordinate, also in <c>[-1, 1]</c>.
        /// </returns>
        public float Remap(int axisIndex, float normalizedCoordinate)
        {
            if ((uint)axisIndex >= (uint)_segmentMaps.Length)
            {
                return normalizedCoordinate;
            }

            var map = _segmentMaps[axisIndex];
            if (map.Length == 0)
            {
                return normalizedCoordinate;
            }

            // Spec-mandated clamp: the table's domain is exactly [-1, 1]. Anything outside
            // that range is clamped before lookup. fvar normalization already guarantees the
            // range, but a malformed coord (or a future caller passing arbitrary values)
            // should not extrapolate off the end of the segment list.
            if (normalizedCoordinate <= -1f)
            {
                return -1f;
            }

            if (normalizedCoordinate >= 1f)
            {
                return 1f;
            }

            // Segment maps are sorted ascending by fromCoordinate. Find the surrounding
            // entries and linearly interpolate. The axis count is small (typically 3..15
            // entries) so a linear scan is faster than a binary search and avoids the
            // overhead of computing midpoints.
            for (var i = 1; i < map.Length; i++)
            {
                var hi = map[i];
                if (normalizedCoordinate <= hi.From)
                {
                    var lo = map[i - 1];

                    var range = hi.From - lo.From;
                    if (range <= 0f)
                    {
                        // Degenerate segment — both endpoints at the same input. Pick
                        // the high mapping; in a well-formed table this never happens.
                        return hi.To;
                    }

                    var t = (normalizedCoordinate - lo.From) / range;
                    return lo.To + t * (hi.To - lo.To);
                }
            }

            // The coordinate is beyond the last segment entry. In a spec-compliant table
            // the final entry's From should be +1 so we never reach here; return its
            // mapped value as a safe fallback.
            return map[map.Length - 1].To;
        }

        public static bool TryLoad(
            GlyphTypeface glyphTypeface,
            [NotNullWhen(true)] out AvarTable? avarTable)
        {
            avarTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            var span = data.Span;
            if (span.Length < 8)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(span);

            var majorVersion = reader.ReadUInt16();
            _ = reader.ReadUInt16(); // minorVersion — v1.0 and v2.0 share the segment-map prefix.

            if (majorVersion != SupportedMajorVersion)
            {
                return false;
            }

            _ = reader.ReadUInt16(); // Reserved field, set to 0 per spec.
            var axisCount = reader.ReadUInt16();

            if (axisCount == 0)
            {
                return false;
            }

            var mapsBuilder = ImmutableArray.CreateBuilder<ImmutableArray<SegmentMapEntry>>(axisCount);

            for (var i = 0; i < axisCount; i++)
            {
                var positionMapCount = reader.ReadUInt16();

                if (positionMapCount == 0)
                {
                    mapsBuilder.Add(ImmutableArray<SegmentMapEntry>.Empty);
                    continue;
                }

                var entries = ImmutableArray.CreateBuilder<SegmentMapEntry>(positionMapCount);
                for (var j = 0; j < positionMapCount; j++)
                {
                    var from = reader.ReadF2dot14();
                    var to = reader.ReadF2dot14();
                    entries.Add(new SegmentMapEntry(from, to));
                }

                mapsBuilder.Add(entries.MoveToImmutable());
            }

            avarTable = new AvarTable(mapsBuilder.MoveToImmutable());
            return true;
        }

        /// <summary>
        /// A single (fromCoordinate, toCoordinate) entry in an axis segment map. Both
        /// values are in the OpenType normalized range <c>[-1, 1]</c>.
        /// </summary>
        private readonly struct SegmentMapEntry
        {
            public SegmentMapEntry(float from, float to)
            {
                From = from;
                To = to;
            }

            public float From { get; }

            public float To { get; }
        }
    }
}
