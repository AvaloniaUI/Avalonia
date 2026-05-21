using System;
using System.Buffers.Binary;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Represents a DeltaSetIndexMap table for COLR v1 font variations.
    /// Maps glyph/entry indices to variation data indices.
    /// </summary>
    /// <remarks>
    /// The DeltaSetIndexMap provides mappings from indices (glyph IDs or other) to 
    /// (outer, inner) index pairs for ItemVariationStore lookups. This enables 
    /// efficient access to variation data.
    /// 
    /// See OpenType spec: https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#associating-target-items-to-variation-data
    /// </remarks>
    internal sealed class DeltaSetIndexMap
    {
        private readonly ReadOnlyMemory<byte> _data;
        private readonly byte _format;
        private readonly byte _entryFormat;
        private readonly uint _mapCount;
        private readonly uint _mapDataOffset;

        private DeltaSetIndexMap(
            ReadOnlyMemory<byte> data,
            byte format,
            byte entryFormat,
            uint mapCount,
            uint mapDataOffset)
        {
            _data = data;
            _format = format;
            _entryFormat = entryFormat;
            _mapCount = mapCount;
            _mapDataOffset = mapDataOffset;
        }

        /// <summary>
        /// Gets the format of this DeltaSetIndexMap (0 or 1).
        /// </summary>
        public byte Format => _format;

        /// <summary>
        /// Gets the number of mapping entries.
        /// </summary>
        public uint MapCount => _mapCount;

        /// <summary>
        /// Loads a DeltaSetIndexMap from the specified data.
        /// </summary>
        /// <param name="data">The raw table data containing the DeltaSetIndexMap.</param>
        /// <param name="offset">The offset within the data where the DeltaSetIndexMap starts.</param>
        /// <returns>A DeltaSetIndexMap instance, or null if the data is invalid.</returns>
        public static DeltaSetIndexMap? Load(ReadOnlyMemory<byte> data, uint offset)
        {
            if (offset >= data.Length)
            {
                return null;
            }

            var span = data.Span.Slice((int)offset);

            // Minimum size check: format (1) + entryFormat (1) + mapCount (varies by format)
            if (span.Length < 2)
            {
                return null;
            }

            var format = span[0];

            // Only formats 0 and 1 are defined
            if (format > 1)
            {
                return null;
            }

            var entryFormat = span[1];

            uint mapCount;
            uint mapDataOffset;

            if (format == 0)
            {
                // Format 0:
                // uint8 format = 0
                // uint8 entryFormat
                // uint16 mapCount

                if (span.Length < 4)
                {
                    return null;
                }

                mapCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
                mapDataOffset = 4; // Map data starts immediately after header
            }
            else // format == 1
            {
                // Format 1:
                // uint8 format = 1
                // uint8 entryFormat
                // uint32 mapCount

                if (span.Length < 6)
                {
                    return null;
                }

                mapCount = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(2));
                mapDataOffset = 6; // Map data starts immediately after header
            }

            return new DeltaSetIndexMap(data.Slice((int)offset), format, entryFormat, mapCount, mapDataOffset);
        }

        /// <summary>
        /// Gets the (outer, inner) delta set index pair for the specified map index.
        /// </summary>
        /// <param name="mapIndex">The index to look up (e.g., glyph ID).</param>
        /// <param name="outerIndex">The outer index for ItemVariationStore lookup.</param>
        /// <param name="innerIndex">The inner index for ItemVariationStore lookup.</param>
        /// <returns>True if the lookup was successful; otherwise false.</returns>
        public bool TryGetDeltaSetIndex(uint mapIndex, out ushort outerIndex, out ushort innerIndex)
        {
            outerIndex = 0;
            innerIndex = 0;

            // If mapIndex is out of range, return false
            if (mapIndex >= _mapCount)
            {
                return false;
            }

            var span = _data.Span;
            var dataOffset = (int)_mapDataOffset;

            // entryFormat specifies the size and packing of entries:
            // Bits 0-3: Size of inner index minus 1 (in bytes)
            // Bits 4-7: Size of outer index minus 1 (in bytes)
            //
            // Common values:
            // 0x00 = 1-byte inner, 1-byte outer (2 bytes total)
            // 0x10 = 1-byte inner, 2-byte outer (3 bytes total)
            // 0x01 = 2-byte inner, 1-byte outer (3 bytes total)
            // 0x11 = 2-byte inner, 2-byte outer (4 bytes total)

            var innerSizeBytes = (_entryFormat & 0x0F) + 1;
            var outerSizeBytes = ((_entryFormat >> 4) & 0x0F) + 1;
            var entrySize = innerSizeBytes + outerSizeBytes;

            var entryOffset = dataOffset + (int)(mapIndex * entrySize);

            // Ensure we have enough data for this entry
            if (entryOffset + entrySize > span.Length)
            {
                return false;
            }

            var entrySpan = span.Slice(entryOffset, entrySize);

            // Read outer index (comes first)
            switch (outerSizeBytes)
            {
                case 1:
                    outerIndex = entrySpan[0];
                    break;
                case 2:
                    outerIndex = BinaryPrimitives.ReadUInt16BigEndian(entrySpan);
                    break;
                case 3:
                    outerIndex = (ushort)((entrySpan[0] << 16) | (entrySpan[1] << 8) | entrySpan[2]);
                    break;
                case 4:
                    outerIndex = (ushort)BinaryPrimitives.ReadUInt32BigEndian(entrySpan);
                    break;
                default:
                    return false;
            }

            // Read inner index (comes after outer)
            var innerSpan = entrySpan.Slice(outerSizeBytes);
            switch (innerSizeBytes)
            {
                case 1:
                    innerIndex = innerSpan[0];
                    break;
                case 2:
                    innerIndex = BinaryPrimitives.ReadUInt16BigEndian(innerSpan);
                    break;
                case 3:
                    innerIndex = (ushort)((innerSpan[0] << 16) | (innerSpan[1] << 8) | innerSpan[2]);
                    break;
                case 4:
                    innerIndex = (ushort)BinaryPrimitives.ReadUInt32BigEndian(innerSpan);
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get a complete delta set for the specified variation index.
        /// </summary>
        /// <param name="itemVariationStore">The ItemVariationStore to retrieve deltas from.</param>
        /// <param name="variationIndex">The index to look up in the DeltaSetIndexMap.</param>
        /// <param name="deltaSet">A DeltaSet ref struct providing format-aware access to deltas.</param>
        /// <returns>True if variation deltas were found; otherwise false.</returns>
        /// <remarks>
        /// This method uses the DeltaSetIndexMap to map the variation index to an (outer, inner) index pair,
        /// then retrieves the corresponding delta set from the ItemVariationStore.
        /// The DeltaSet ref struct provides allocation-free access to both word and byte deltas.
        /// </remarks>
        public bool TryGetVariationDeltaSet(
            ItemVariationStore itemVariationStore,
            uint variationIndex,
            out DeltaSet deltaSet)
        {
            deltaSet = DeltaSet.Empty;

            if (itemVariationStore == null)
            {
                return false;
            }

            // Map the variation index to (outer, inner) indices
            if (!TryGetDeltaSetIndex(variationIndex, out var outerIndex, out var innerIndex))
            {
                return false;
            }

            // Delegate to ItemVariationStore
            return itemVariationStore.TryGetDeltaSet(outerIndex, innerIndex, out deltaSet);
        }
    }
}
