using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses the OpenType 'VVAR' (Vertical Metrics Variations) table. Provides
    /// per-glyph vertical-advance and top-side-bearing deltas at a given active
    /// variation point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// VVAR is HVAR's vertical counterpart: where HVAR carries the per-glyph advance
    /// width and side-bearing deltas needed for horizontal text layout, VVAR carries
    /// the equivalents for vertical layout (CJK in tategaki, Mongolian, classical
    /// scripts). Horizontal-text fonts typically don't ship VVAR — Inter Variable
    /// doesn't, for example — and the layout pipeline silently sees no deltas.
    /// </para>
    /// <para>
    /// Structure differs from HVAR by one extra field: a fifth offset to a vertical-
    /// origin mapping (the per-glyph offset of the typographic vertical origin from
    /// the top of the design grid). That field is parsed here for header validation
    /// but not yet exposed — the matching <c>VORG</c> base table isn't read by
    /// Avalonia, so a delta against it has no consumer.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vvar"/>.
    /// </para>
    /// </remarks>
    internal sealed class VvarTable
    {
        internal const string TableName = "VVAR";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const ushort SupportedMajorVersion = 1;

        private readonly ItemVariationStore _store;
        private readonly DeltaSetIndexMap? _advanceMap;
        private readonly DeltaSetIndexMap? _tsbMap;
        private readonly DeltaSetIndexMap? _bsbMap;

        private VvarTable(
            ItemVariationStore store,
            DeltaSetIndexMap? advanceMap,
            DeltaSetIndexMap? tsbMap,
            DeltaSetIndexMap? bsbMap)
        {
            _store = store;
            _advanceMap = advanceMap;
            _tsbMap = tsbMap;
            _bsbMap = bsbMap;
        }

        public ItemVariationStore Store => _store;

        public static bool TryLoad(
            GlyphTypeface glyphTypeface,
            int expectedAxisCount,
            [NotNullWhen(true)] out VvarTable? vvarTable)
        {
            vvarTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            // VVAR's header is 24 bytes — one Offset32 longer than HVAR because of the
            // vorgMappingOffset at the tail.
            var span = data.Span;
            if (span.Length < 24)
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
            var tsbMapOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(12));
            var bsbMapOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(16));
            // vorgMappingOffset = data.Slice(20) — parsed and validated for header
            // soundness but not stored; we have no consumer for vertical-origin deltas.

            if (ivsOffset <= 0 || ivsOffset >= data.Length)
            {
                return false;
            }

            if (!ItemVariationStore.TryLoad(data.Slice(ivsOffset), expectedAxisCount, out var store))
            {
                return false;
            }

            // The three glyph-keyed index maps are optional — offset 0 means "use direct
            // mapping (glyph ID == inner index, outer = 0)". The offsets are attacker-controlled,
            // so validate them unsigned against the table length before slicing (as the IVS
            // offset is checked above): a hostile offset degrades to "no VVAR" rather than
            // throwing out of the typeface constructor.
            DeltaSetIndexMap? advanceMap = null;
            DeltaSetIndexMap? tsbMap = null;
            DeltaSetIndexMap? bsbMap = null;

            if (advanceMapOffset != 0)
            {
                if (advanceMapOffset >= (uint)data.Length ||
                    !DeltaSetIndexMap.TryLoad(data.Slice((int)advanceMapOffset), out advanceMap))
                {
                    return false;
                }
            }

            if (tsbMapOffset != 0)
            {
                if (tsbMapOffset >= (uint)data.Length ||
                    !DeltaSetIndexMap.TryLoad(data.Slice((int)tsbMapOffset), out tsbMap))
                {
                    return false;
                }
            }

            if (bsbMapOffset != 0)
            {
                if (bsbMapOffset >= (uint)data.Length ||
                    !DeltaSetIndexMap.TryLoad(data.Slice((int)bsbMapOffset), out bsbMap))
                {
                    return false;
                }
            }

            vvarTable = new VvarTable(store, advanceMap, tsbMap, bsbMap);
            return true;
        }

        /// <summary>
        /// Computes the advance-height delta for <paramref name="glyphIndex"/> at the
        /// supplied active variation coordinates. The delta is in font design units
        /// and is added to the <c>vmtx</c> advance.
        /// </summary>
        public bool TryGetAdvanceHeightDelta(int glyphIndex, ReadOnlySpan<float> activeCoords, out float delta)
            => TryGetDelta(_advanceMap, glyphIndex, activeCoords, out delta);

        /// <summary>
        /// Computes the top-side-bearing delta for <paramref name="glyphIndex"/>.
        /// Returns <c>true</c> with <c>delta = 0</c> when the font carries no TSB
        /// mapping (typical — most variable fonts that ship VVAR only carry advance
        /// deltas, like AdobeBlank2VF).
        /// </summary>
        public bool TryGetTopSideBearingDelta(int glyphIndex, ReadOnlySpan<float> activeCoords, out float delta)
        {
            if (_tsbMap is null)
            {
                delta = 0f;
                return true;
            }

            return TryGetDelta(_tsbMap, glyphIndex, activeCoords, out delta);
        }

        private bool TryGetDelta(DeltaSetIndexMap? map, int glyphIndex, ReadOnlySpan<float> activeCoords, out float delta)
        {
            delta = 0f;

            int outerIndex;
            int innerIndex;

            if (map is null)
            {
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
