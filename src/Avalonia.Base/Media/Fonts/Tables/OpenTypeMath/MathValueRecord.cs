using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables.OpenTypeMath
{
    /// <summary>
    /// Reads MathValueRecords: a design-unit value followed by an offset to a Device or
    /// VariationIndex table, relative to the table that contains the record.
    /// </summary>
    /// <remarks>
    /// A VariationIndex table (delta format 0x8000) names a delta set in GDEF's item variation store,
    /// which varies the value at the typeface's variation instance. Device tables of formats 1 to 3
    /// hold per-ppem hinting adjustments and are ignored, since text is not hinted per ppem.
    /// </remarks>
    internal static class MathValueRecord
    {
        public const int Size = 4;

        private const int VariationIndexSize = 6;
        private const ushort VariationIndexFormat = 0x8000;

        /// <summary>
        /// Reads the record at <paramref name="recordOffset"/> in <paramref name="parent"/>, the bytes
        /// from the start of the containing table to the end of the MATH table.
        /// </summary>
        /// <remarks>
        /// The caller checks that the record lies inside <paramref name="parent"/>. Without a store or
        /// region scalers, and for a device offset that is out of range or not a VariationIndex, the
        /// value is returned as designed.
        /// </remarks>
        public static int Read(
            ReadOnlySpan<byte> parent,
            int recordOffset,
            ItemVariationStore? store,
            ReadOnlySpan<float> regionScalers)
        {
            int value = BinaryPrimitives.ReadInt16BigEndian(parent.Slice(recordOffset));

            if (store is null || regionScalers.IsEmpty)
            {
                return value;
            }

            int deviceOffset = BinaryPrimitives.ReadUInt16BigEndian(parent.Slice(recordOffset + 2));

            if (deviceOffset == 0 || deviceOffset > parent.Length - VariationIndexSize)
            {
                return value;
            }

            var device = parent.Slice(deviceOffset);

            if (BinaryPrimitives.ReadUInt16BigEndian(device.Slice(4)) != VariationIndexFormat)
            {
                return value;
            }

            var outer = BinaryPrimitives.ReadUInt16BigEndian(device);
            var inner = BinaryPrimitives.ReadUInt16BigEndian(device.Slice(2));

            if (!store.TryGetDeltaWithScalers(outer, inner, regionScalers, out var delta))
            {
                return value;
            }

            // Rounded like the MVAR deltas applied to the font metrics, so a font's metrics and
            // its MATH values agree at every instance.
            return value + (int)MathF.Round(delta);
        }
    }
}
