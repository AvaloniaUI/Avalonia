using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Reads the header of the OpenType 'GDEF' (Glyph Definition) table and, from version 1.3, its
    /// item variation store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Variable fonts resolve the device records of GPOS, JSTF and MATH through GDEF's variation
    /// store: a record whose device offset points to a VariationIndex table names an (outer, inner)
    /// delta set in this store. Only the store is read; glyph classes, attachment points, ligature
    /// carets and mark sets are left to the shaper.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef"/>.
    /// </para>
    /// </remarks>
    internal sealed class GdefTable
    {
        internal const string TableName = "GDEF";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const ushort SupportedMajorVersion = 1;
        private const int HeaderSize = 12;
        private const int Version13HeaderSize = 18;

        private GdefTable(int minorVersion, ItemVariationStore? variationStore)
        {
            MinorVersion = minorVersion;
            VariationStore = variationStore;
        }

        /// <summary>
        /// Gets the minor version of the table.
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        /// Gets the item variation store, or <c>null</c> when the table is older than 1.3, has no
        /// store, or the store is malformed or declares a different axis count than the font.
        /// </summary>
        public ItemVariationStore? VariationStore { get; }

        public static bool TryLoad(GlyphTypeface glyphTypeface, int expectedAxisCount, [NotNullWhen(true)] out GdefTable? gdefTable)
        {
            gdefTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            return TryLoad(data, expectedAxisCount, out gdefTable);
        }

        public static bool TryLoad(ReadOnlyMemory<byte> data, int expectedAxisCount, [NotNullWhen(true)] out GdefTable? gdefTable)
        {
            gdefTable = null;

            var span = data.Span;

            if (span.Length < HeaderSize || BinaryPrimitives.ReadUInt16BigEndian(span) != SupportedMajorVersion)
            {
                return false;
            }

            var minorVersion = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
            ItemVariationStore? store = null;

            // Version 1.3 appends a 32-bit store offset after the 1.2 mark glyph sets offset. A
            // later minor version keeps that layout and may append more.
            if (minorVersion >= 3 && span.Length >= Version13HeaderSize)
            {
                var storeOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(14));

                if (storeOffset != 0 && storeOffset < (uint)span.Length)
                {
                    ItemVariationStore.TryLoad(data.Slice((int)storeOffset), expectedAxisCount, out store);
                }
            }

            gdefTable = new GdefTable(minorVersion, store);

            return true;
        }
    }
}
