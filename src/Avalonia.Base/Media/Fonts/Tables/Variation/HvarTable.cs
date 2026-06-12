using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses the OpenType 'HVAR' (Horizontal Metrics Variations) table. Provides
    /// per-glyph advance-width, left-side-bearing, and right-side-bearing deltas
    /// at a given active variation point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// HVAR is the table that makes variable-text layout work. Without it, a font
    /// laid out at <c>wght=900</c> uses default-instance (<c>wght=400</c>) advance
    /// widths, so the bolder glyphs overlap the next-glyph slot. HVAR carries the
    /// extra width each glyph needs at higher weights.
    /// </para>
    /// <para>
    /// Internally HVAR wraps an <see cref="ItemVariationStore"/> for the actual
    /// delta math, plus up to three <see cref="DeltaSetIndexMap"/> instances that
    /// map glyph IDs to <c>(outer, inner)</c> store coordinates — one each for
    /// advance, LSB, and RSB. When a mapping is absent the glyph ID is used as the
    /// inner index directly with outer = 0.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hvar"/>.
    /// </para>
    /// </remarks>
    internal sealed class HvarTable
    {
        internal const string TableName = "HVAR";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const ushort SupportedMajorVersion = 1;

        private readonly ItemVariationStore _store;
        private readonly DeltaSetIndexMap? _advanceMap;
        private readonly DeltaSetIndexMap? _lsbMap;
        private readonly DeltaSetIndexMap? _rsbMap;

        private HvarTable(
            ItemVariationStore store,
            DeltaSetIndexMap? advanceMap,
            DeltaSetIndexMap? lsbMap,
            DeltaSetIndexMap? rsbMap)
        {
            _store = store;
            _advanceMap = advanceMap;
            _lsbMap = lsbMap;
            _rsbMap = rsbMap;
        }

        public ItemVariationStore Store => _store;

        public static bool TryLoad(
            GlyphTypeface glyphTypeface,
            int expectedAxisCount,
            [NotNullWhen(true)] out HvarTable? hvarTable)
        {
            hvarTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            var span = data.Span;
            if (span.Length < 20)
            {
                return false;
            }

            var majorVersion = BinaryPrimitives.ReadUInt16BigEndian(span);
            if (majorVersion != SupportedMajorVersion)
            {
                return false;
            }

            var ivsOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4));
            var advanceMapOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));
            var lsbMapOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(12));
            var rsbMapOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(16));

            if (ivsOffset <= 0 || ivsOffset >= data.Length)
            {
                return false;
            }

            if (!ItemVariationStore.TryLoad(data.Slice(ivsOffset), expectedAxisCount, out var store))
            {
                return false;
            }

            // The three index maps are optional — offset 0 means "use direct mapping". The
            // offsets are attacker-controlled, so validate them unsigned against the table
            // length before slicing (mirroring the IVS offset check above): a hostile offset
            // must degrade to "no HVAR" instead of throwing out of the typeface constructor.
            DeltaSetIndexMap? advanceMap = null;
            DeltaSetIndexMap? lsbMap = null;
            DeltaSetIndexMap? rsbMap = null;

            if (advanceMapOffset != 0)
            {
                if (advanceMapOffset >= (uint)data.Length ||
                    !DeltaSetIndexMap.TryLoad(data.Slice((int)advanceMapOffset), out advanceMap))
                {
                    return false;
                }
            }

            if (lsbMapOffset != 0)
            {
                if (lsbMapOffset >= (uint)data.Length ||
                    !DeltaSetIndexMap.TryLoad(data.Slice((int)lsbMapOffset), out lsbMap))
                {
                    return false;
                }
            }

            if (rsbMapOffset != 0)
            {
                if (rsbMapOffset >= (uint)data.Length ||
                    !DeltaSetIndexMap.TryLoad(data.Slice((int)rsbMapOffset), out rsbMap))
                {
                    return false;
                }
            }

            hvarTable = new HvarTable(store, advanceMap, lsbMap, rsbMap);
            return true;
        }

        /// <summary>
        /// Computes the advance-width delta for <paramref name="glyphIndex"/> at the
        /// supplied active variation coordinates. The delta is in font design units
        /// and is added to the <c>hmtx</c> advance.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the lookup succeeded; <c>false</c> when the active coords
        /// are an invalid shape or the underlying store is malformed. A successful
        /// lookup with no contribution (e.g. variation coords all at default) returns
        /// <c>true</c> with <c>delta = 0</c>.
        /// </returns>
        public bool TryGetAdvanceDelta(int glyphIndex, ReadOnlySpan<float> activeCoords, out float delta)
            => TryGetDelta(_advanceMap, glyphIndex, activeCoords, out delta);

        /// <summary>
        /// Computes the left-side-bearing delta for <paramref name="glyphIndex"/>.
        /// Returns <c>true</c> with <c>delta = 0</c> when the font carries no LSB
        /// mapping (the common case — most variable fonts only carry advance deltas).
        /// </summary>
        public bool TryGetLeftSideBearingDelta(int glyphIndex, ReadOnlySpan<float> activeCoords, out float delta)
        {
            if (_lsbMap is null)
            {
                delta = 0f;
                return true;
            }

            return TryGetDelta(_lsbMap, glyphIndex, activeCoords, out delta);
        }

        private bool TryGetDelta(DeltaSetIndexMap? map, int glyphIndex, ReadOnlySpan<float> activeCoords, out float delta)
        {
            delta = 0f;

            int outerIndex;
            int innerIndex;

            if (map is null)
            {
                // Direct mapping: glyph ID is the inner index, outer = 0.
                outerIndex = 0;
                innerIndex = glyphIndex;
            }
            else
            {
                if (!map.TryGetIndices(glyphIndex, out outerIndex, out innerIndex))
                {
                    return false;
                }
            }

            return _store.TryGetDelta(outerIndex, innerIndex, activeCoords, out delta);
        }
    }
}
