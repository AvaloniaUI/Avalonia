using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses the OpenType 'MVAR' (Metrics Variations) table. Provides font-wide metric
    /// deltas (ascender, descender, line gap, x-height, cap height, underline, strikeout,
    /// etc.) at a given active variation point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MVAR is the cousin of HVAR: where HVAR varies per-glyph horizontal metrics, MVAR
    /// varies the font-wide constants that line up multiple glyphs into a row of text
    /// (ascent + descent + line gap → line height; underline / strikeout positions and
    /// thicknesses; x-height / cap height for vertical alignment).
    /// </para>
    /// <para>
    /// Each MVAR value is addressed by a 4-character tag (e.g. <c>hasc</c> = horizontal
    /// ascender from OS/2's <c>sTypoAscender</c>, <c>unds</c> = underline thickness from
    /// post's <c>underlineThickness</c>). The header lists which tags are present along
    /// with their <c>(outer, inner)</c> coordinates into the embedded
    /// <see cref="ItemVariationStore"/>; the delta math is identical to HVAR.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Well-known OpenType MVAR value tags. Mapped to <see cref="FontMetrics"/> fields
    /// in <see cref="GlyphTypeface"/>'s clone constructor. The full list of standard
    /// tags is larger (~30 entries spanning sub/superscript offsets, caret slope, gasp
    /// ranges, vertical metrics); the set surfaced here is the one that maps onto our
    /// public FontMetrics surface today.
    /// </summary>
    internal static class MvarTags
    {
        public static readonly OpenTypeTag HorizontalAscender = OpenTypeTag.Parse("hasc");
        public static readonly OpenTypeTag HorizontalDescender = OpenTypeTag.Parse("hdsc");
        public static readonly OpenTypeTag HorizontalLineGap = OpenTypeTag.Parse("hlgp");
        public static readonly OpenTypeTag UnderlineSize = OpenTypeTag.Parse("unds");
        public static readonly OpenTypeTag UnderlineOffset = OpenTypeTag.Parse("undo");
        public static readonly OpenTypeTag StrikeoutSize = OpenTypeTag.Parse("strs");
        public static readonly OpenTypeTag StrikeoutOffset = OpenTypeTag.Parse("stro");
    }

    internal sealed class MvarTable
    {
        internal const string TableName = "MVAR";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const ushort SupportedMajorVersion = 1;
        private const int HeaderSize = 12;
        private const int ValueRecordSize = 8;

        private readonly ItemVariationStore _store;
        // Tag → (outer, inner) lookup map. Built at parse time; constant for the table's
        // lifetime. We use a Dictionary because the typical valueRecordCount is small
        // (~5-20) and consumers query a handful of well-known tags — a dictionary keeps
        // lookups O(1) without a more complex data structure.
        private readonly Dictionary<OpenTypeTag, (ushort outer, ushort inner)> _records;

        private MvarTable(
            ItemVariationStore store,
            Dictionary<OpenTypeTag, (ushort outer, ushort inner)> records)
        {
            _store = store;
            _records = records;
        }

        public ItemVariationStore Store => _store;

        public int RecordCount => _records.Count;

        public static bool TryLoad(
            GlyphTypeface glyphTypeface,
            int expectedAxisCount,
            [NotNullWhen(true)] out MvarTable? mvarTable)
        {
            mvarTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            var span = data.Span;
            if (span.Length < HeaderSize)
            {
                return false;
            }

            var majorVersion = BinaryPrimitives.ReadUInt16BigEndian(span);
            if (majorVersion != SupportedMajorVersion)
            {
                return false;
            }

            // Header layout: majorVersion(2) + minorVersion(2) + reserved(2) +
            // valueRecordSize(2) + valueRecordCount(2) + itemVariationStoreOffset(2).
            // minorVersion and reserved are skipped — we already validated majorVersion.
            var valueRecordSize = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));
            var valueRecordCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8));
            var ivsOffset = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(10));

            // Per spec v1.0 valueRecordSize is always 8. Reject other sizes rather than
            // attempt to parse a record layout we don't recognize.
            if (valueRecordSize != ValueRecordSize)
            {
                return false;
            }

            if (HeaderSize + valueRecordCount * ValueRecordSize > span.Length)
            {
                return false;
            }

            if (ivsOffset == 0 || ivsOffset >= data.Length)
            {
                return false;
            }

            if (!ItemVariationStore.TryLoad(data.Slice(ivsOffset), expectedAxisCount, out var store))
            {
                return false;
            }

            var records = new Dictionary<OpenTypeTag, (ushort outer, ushort inner)>(valueRecordCount);
            for (var i = 0; i < valueRecordCount; i++)
            {
                var recordPos = HeaderSize + i * ValueRecordSize;
                var tag = new OpenTypeTag(BinaryPrimitives.ReadUInt32BigEndian(span.Slice(recordPos, 4)));
                var outer = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(recordPos + 4, 2));
                var inner = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(recordPos + 6, 2));

                // Duplicate tags would be a malformed table; first-seen wins.
                records.TryAdd(tag, (outer, inner));
            }

            mvarTable = new MvarTable(store, records);
            return true;
        }

        /// <summary>
        /// Computes the delta for the font-wide metric identified by
        /// <paramref name="valueTag"/> at the supplied active variation point. Returns
        /// <c>false</c> when the font doesn't declare a value record for that tag
        /// (a common case — most variable fonts only vary a subset of metrics).
        /// </summary>
        public bool TryGetMetricDelta(OpenTypeTag valueTag, ReadOnlySpan<float> activeCoords, out float delta)
        {
            if (!_records.TryGetValue(valueTag, out var indices))
            {
                delta = 0f;
                return false;
            }

            return _store.TryGetDelta(indices.outer, indices.inner, activeCoords, out delta);
        }
    }
}
