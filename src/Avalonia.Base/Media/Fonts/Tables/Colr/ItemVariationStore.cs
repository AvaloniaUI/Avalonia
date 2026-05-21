using System;
using System.Buffers.Binary;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Represents an ItemVariationStore for OpenType font variations.
    /// Stores delta values that can be applied to font data based on variation axis coordinates.
    /// </summary>
    /// <remarks>
    /// The ItemVariationStore is used in multiple OpenType tables (COLR, GDEF, GPOS, etc.)
    /// to provide variation data for variable fonts. It organizes deltas into a two-level
    /// hierarchy: ItemVariationData arrays (outer level) containing DeltaSets (inner level).
    /// 
    /// See OpenType spec: https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#item-variation-store
    /// </remarks>
    internal sealed class ItemVariationStore
    {
        private readonly ReadOnlyMemory<byte> _data;
        private readonly uint _baseOffset;
        private readonly ushort _format;
        private readonly uint _variationRegionListOffset;
        private readonly ushort _itemVariationDataCount;

        private ItemVariationStore(
            ReadOnlyMemory<byte> data,
            uint baseOffset,
            ushort format,
            uint variationRegionListOffset,
            ushort itemVariationDataCount)
        {
            _data = data;
            _baseOffset = baseOffset;
            _format = format;
            _variationRegionListOffset = variationRegionListOffset;
            _itemVariationDataCount = itemVariationDataCount;
        }

        /// <summary>
        /// Gets the format of this ItemVariationStore (currently only 1 is defined).
        /// </summary>
        public ushort Format => _format;

        /// <summary>
        /// Gets the number of ItemVariationData arrays in this store.
        /// </summary>
        public ushort ItemVariationDataCount => _itemVariationDataCount;

        /// <summary>
        /// Loads an ItemVariationStore from the specified data.
        /// </summary>
        /// <param name="data">The complete table data (e.g., COLR table data).</param>
        /// <param name="offset">The offset within the data where the ItemVariationStore starts.</param>
        /// <returns>An ItemVariationStore instance, or null if the data is invalid.</returns>
        public static ItemVariationStore? Load(ReadOnlyMemory<byte> data, uint offset)
        {
            if (offset == 0 || offset >= data.Length)
            {
                return null;
            }

            var span = data.Span.Slice((int)offset);

            // ItemVariationStore format:
            // uint16 format (must be 1)
            // Offset32 variationRegionListOffset
            // uint16 itemVariationDataCount
            // Offset32 itemVariationDataOffsets[itemVariationDataCount]

            if (span.Length < 8) // format (2) + variationRegionListOffset (4) + itemVariationDataCount (2)
            {
                return null;
            }

            var format = BinaryPrimitives.ReadUInt16BigEndian(span);
            if (format != 1)
            {
                return null; // Only format 1 is defined
            }

            var variationRegionListOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(2));
            var itemVariationDataCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));

            return new ItemVariationStore(
                data,
                offset,
                format,
                variationRegionListOffset,
                itemVariationDataCount);
        }

        /// <summary>
        /// Tries to get a complete delta set for the specified (outer, inner) index pair.
        /// </summary>
        /// <param name="outerIndex">The outer index (ItemVariationData index).</param>
        /// <param name="innerIndex">The inner index (DeltaSet index within ItemVariationData).</param>
        /// <param name="deltaSet">A DeltaSet ref struct providing format-aware access to deltas.</param>
        /// <returns>True if the delta set was found; otherwise false.</returns>
        /// <remarks>
        /// This method returns a DeltaSet ref struct that provides allocation-free access to both
        /// word deltas (16-bit) and byte deltas (8-bit), along with methods for uniform access.
        /// </remarks>
        public bool TryGetDeltaSet(ushort outerIndex, ushort innerIndex, out DeltaSet deltaSet)
        {
            deltaSet = DeltaSet.Empty;

            // Validate outer index
            if (outerIndex >= _itemVariationDataCount)
            {
                return false;
            }

            var span = _data.Span;
            var storeSpan = span.Slice((int)_baseOffset);

            // Read the offset to the ItemVariationData for the outer index
            var offsetsStart = 8;
            var itemDataOffsetPos = offsetsStart + (outerIndex * 4);

            if (itemDataOffsetPos + 4 > storeSpan.Length)
            {
                return false;
            }

            var itemVariationDataOffset = BinaryPrimitives.ReadUInt32BigEndian(storeSpan.Slice(itemDataOffsetPos));
            var absoluteItemDataOffset = _baseOffset + itemVariationDataOffset;

            if (absoluteItemDataOffset >= _data.Length)
            {
                return false;
            }

            var itemDataSpan = span.Slice((int)absoluteItemDataOffset);

            if (itemDataSpan.Length < 6)
            {
                return false;
            }

            var itemCount = BinaryPrimitives.ReadUInt16BigEndian(itemDataSpan);
            var wordDeltaCount = BinaryPrimitives.ReadUInt16BigEndian(itemDataSpan.Slice(2));
            var regionIndexCount = BinaryPrimitives.ReadUInt16BigEndian(itemDataSpan.Slice(4));

            if (innerIndex >= itemCount)
            {
                return false;
            }

            var longWordCount = wordDeltaCount;
            var shortDeltaCount = regionIndexCount - wordDeltaCount;
            var deltaSetSize = (longWordCount * 2) + shortDeltaCount;

            var regionIndexesSize = regionIndexCount * 2;
            var deltaSetsStart = 6 + regionIndexesSize;
            var targetDeltaSetOffset = deltaSetsStart + (innerIndex * deltaSetSize);

            if (targetDeltaSetOffset + deltaSetSize > itemDataSpan.Length)
            {
                return false;
            }

            var deltaSetSpan = itemDataSpan.Slice(targetDeltaSetOffset, deltaSetSize);

            // Create DeltaSet with the raw data
            deltaSet = new DeltaSet(deltaSetSpan, wordDeltaCount, regionIndexCount);

            return true;
        }
    }
}
