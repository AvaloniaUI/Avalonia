using System;
using System.Runtime.InteropServices;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Represents a set of variation deltas from an ItemVariationStore.
    /// This is a ref struct for allocation-free access to delta data.
    /// </summary>
    /// <remarks>
    /// OpenType ItemVariationStore uses a mixed format for deltas:
    /// - Word deltas (16-bit, int16): More significant variations
    /// - Byte deltas (8-bit, int8): Smaller variations for space optimization
    /// 
    /// The wordDeltaCount determines how many deltas are 16-bit vs 8-bit.
    /// 
    /// Delta values are stored as integers but must be converted based on their target type:
    /// - FWORD deltas (coordinates, translations): No conversion needed (design units)
    /// - F2DOT14 deltas (scales, angles, alpha): Divide by 16384.0
    /// - Fixed deltas (Affine2x3 components): Divide by 65536.0
    /// </remarks>
    public ref struct DeltaSet
    {
        /// <summary>
        /// Creates an empty DeltaSet.
        /// </summary>
        public static DeltaSet Empty => new DeltaSet(ReadOnlySpan<byte>.Empty, 0, 0);

        private readonly ReadOnlySpan<byte> _data;
        private readonly ushort _wordDeltaCount;
        private readonly ushort _totalDeltaCount;

        internal DeltaSet(ReadOnlySpan<byte> data, ushort wordDeltaCount, ushort totalDeltaCount)
        {
            _data = data;
            _wordDeltaCount = wordDeltaCount;
            _totalDeltaCount = totalDeltaCount;
        }

        /// <summary>
        /// Gets whether this delta set is empty (no deltas available).
        /// </summary>
        public bool IsEmpty => _totalDeltaCount == 0;

        /// <summary>
        /// Gets the total number of deltas in this set (word + byte deltas).
        /// </summary>
        public int Count => _totalDeltaCount;

        /// <summary>
        /// Gets the number of word deltas (16-bit) in this set.
        /// </summary>
        public int WordDeltaCount => _wordDeltaCount;

        /// <summary>
        /// Gets the number of byte deltas (8-bit) in this set.
        /// </summary>
        public int ByteDeltaCount => _totalDeltaCount - _wordDeltaCount;

        /// <summary>
        /// Gets the word deltas (16-bit signed integers) as a ReadOnlySpan.
        /// </summary>
        public ReadOnlySpan<short> WordDeltas
        {
            get
            {
                if (_wordDeltaCount == 0)
                {
                    return ReadOnlySpan<short>.Empty;
                }

                var wordBytes = _data.Slice(0, _wordDeltaCount * 2);
                return MemoryMarshal.Cast<byte, short>(wordBytes);
            }
        }

        /// <summary>
        /// Gets the byte deltas (8-bit signed integers) as a ReadOnlySpan.
        /// </summary>
        public ReadOnlySpan<sbyte> ByteDeltas
        {
            get
            {
                var byteDeltaCount = _totalDeltaCount - _wordDeltaCount;
                if (byteDeltaCount == 0)
                {
                    return ReadOnlySpan<sbyte>.Empty;
                }

                var byteOffset = _wordDeltaCount * 2;
                var byteBytes = _data.Slice(byteOffset, byteDeltaCount);
                return MemoryMarshal.Cast<byte, sbyte>(byteBytes);
            }
        }

        /// <summary>
        /// Gets a delta value at the specified index, converting byte deltas to short for uniform access.
        /// </summary>
        /// <param name="index">The index of the delta (0 to Count-1).</param>
        /// <returns>The delta value as a 16-bit signed integer.</returns>
        /// <exception cref="IndexOutOfRangeException">If index is out of range.</exception>
        public short this[int index]
        {
            get
            {
                if (index < 0 || index >= _totalDeltaCount)
                {
                    throw new IndexOutOfRangeException($"Delta index {index} is out of range [0, {_totalDeltaCount})");
                }

                // Word deltas come first
                if (index < _wordDeltaCount)
                {
                    var wordBytes = _data.Slice(index * 2, 2);
                    return System.Buffers.Binary.BinaryPrimitives.ReadInt16BigEndian(wordBytes);
                }

                // Byte deltas come after word deltas
                var byteIndex = index - _wordDeltaCount;
                var byteOffset = (_wordDeltaCount * 2) + byteIndex;
                return (sbyte)_data[byteOffset];
            }
        }

        /// <summary>
        /// Tries to get a delta value at the specified index.
        /// </summary>
        /// <param name="index">The index of the delta.</param>
        /// <param name="delta">The delta value if successful.</param>
        /// <returns>True if the index is valid; otherwise false.</returns>
        public bool TryGetDelta(int index, out short delta)
        {
            delta = 0;

            if (index < 0 || index >= _totalDeltaCount)
            {
                return false;
            }

            delta = this[index];
            return true;
        }

        /// <summary>
        /// Gets a delta as an FWORD value (design units - no conversion needed).
        /// </summary>
        /// <param name="index">The index of the delta.</param>
        /// <returns>The delta value as a double in design units, or 0.0 if index is out of range.</returns>
        /// <remarks>
        /// FWORD deltas are used for:
        /// - Translation offsets (dx, dy)
        /// - Gradient coordinates (x0, y0, x1, y1, etc.)
        /// - Center points (centerX, centerY)
        /// - Radii values (r0, r1)
        /// </remarks>
        public double GetFWordDelta(int index)
        {
            if (index < 0 || index >= _totalDeltaCount)
            {
                return 0.0;
            }

            // FWORD: No conversion needed, deltas are in design units
            return this[index];
        }

        /// <summary>
        /// Gets a delta as an F2DOT14 value (fixed-point 2.14 format).
        /// </summary>
        /// <param name="index">The index of the delta.</param>
        /// <returns>The delta value as a double after F2DOT14 conversion, or 0.0 if index is out of range.</returns>
        /// <remarks>
        /// F2DOT14 deltas are used for:
        /// - Scale values (scaleX, scaleY, scale)
        /// - Rotation angles (angle)
        /// - Skew angles (xAngle, yAngle)
        /// - Alpha values (alpha)
        /// - Gradient stop offsets
        /// </remarks>
        public double GetF2Dot14Delta(int index)
        {
            if (index < 0 || index >= _totalDeltaCount)
            {
                return 0.0;
            }

            // F2DOT14: Signed fixed-point with 2 integer bits and 14 fractional bits
            // Convert by dividing by 2^14 (16384)
            return this[index] / 16384.0;
        }

        /// <summary>
        /// Gets a delta as a Fixed value (fixed-point 16.16 format).
        /// </summary>
        /// <param name="index">The index of the delta.</param>
        /// <returns>The delta value as a double after Fixed conversion, or 0.0 if index is out of range.</returns>
        /// <remarks>
        /// Fixed deltas are used for:
        /// - Affine2x3 matrix components (xx, yx, xy, yy, dx, dy)
        /// 
        /// Note: This assumes the delta is stored as 16-bit but represents a 16.16 fixed-point conceptually.
        /// In practice, Affine2x3 variations may need special handling depending on the font's encoding.
        /// </remarks>
        public double GetFixedDelta(int index)
        {
            if (index < 0 || index >= _totalDeltaCount)
            {
                return 0.0;
            }

            // Fixed: 16.16 format
            // Convert by dividing by 2^16 (65536)
            return this[index] / 65536.0;
        }

        /// <summary>
        /// Gets all deltas as 16-bit values, converting byte deltas to short.
        /// Note: This method allocates an array.
        /// </summary>
        /// <returns>An array containing all deltas as 16-bit signed integers.</returns>
        public short[] ToArray()
        {
            if (_totalDeltaCount == 0)
            {
                return Array.Empty<short>();
            }

            var result = new short[_totalDeltaCount];

            // Copy word deltas
            var wordDeltas = WordDeltas;
            for (int i = 0; i < wordDeltas.Length; i++)
            {
                result[i] = wordDeltas[i];
            }

            // Convert and copy byte deltas
            var byteDeltas = ByteDeltas;
            for (int i = 0; i < byteDeltas.Length; i++)
            {
                result[_wordDeltaCount + i] = byteDeltas[i];
            }

            return result;
        }
    }
}
