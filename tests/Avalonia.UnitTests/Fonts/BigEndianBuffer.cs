using System;
using System.Buffers.Binary;

namespace Avalonia.UnitTests
{
    /// <summary>
    /// A growable big-endian byte writer for hand-crafting OpenType table sub-structures
    /// (ItemVariationStores, cmap subtables, COLR paint graphs, ...) in tests.
    /// </summary>
    /// <remarks>
    /// OpenType is big-endian and pervasively offset-based: a header field holds the byte
    /// offset of a sub-table that is written later. <see cref="ReserveOffset16"/> /
    /// <see cref="ReserveOffset32"/> write a placeholder and return its position so the
    /// real value can be back-patched with <see cref="PatchUInt16"/> / <see cref="PatchUInt32"/>
    /// once the target's position is known (via <see cref="Position"/>). All multi-byte
    /// writes are big-endian.
    /// </remarks>
    public sealed class BigEndianBuffer
    {
        private byte[] _buffer;
        private int _length;

        public BigEndianBuffer(int initialCapacity = 64)
        {
            _buffer = new byte[Math.Max(4, initialCapacity)];
        }

        /// <summary>The number of bytes written so far — also the offset the next write lands at.</summary>
        public int Position => _length;

        public BigEndianBuffer UInt8(int value)
        {
            EnsureCapacity(1);
            _buffer[_length++] = checked((byte)value);
            return this;
        }

        public BigEndianBuffer Int8(int value)
        {
            EnsureCapacity(1);
            _buffer[_length++] = unchecked((byte)(sbyte)value);
            return this;
        }

        public BigEndianBuffer UInt16(int value)
        {
            EnsureCapacity(2);
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_length), checked((ushort)value));
            _length += 2;
            return this;
        }

        public BigEndianBuffer Int16(int value)
        {
            EnsureCapacity(2);
            BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(_length), checked((short)value));
            _length += 2;
            return this;
        }

        public BigEndianBuffer UInt24(int value)
        {
            EnsureCapacity(3);
            _buffer[_length++] = (byte)((value >> 16) & 0xFF);
            _buffer[_length++] = (byte)((value >> 8) & 0xFF);
            _buffer[_length++] = (byte)(value & 0xFF);
            return this;
        }

        public BigEndianBuffer UInt32(uint value)
        {
            EnsureCapacity(4);
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_length), value);
            _length += 4;
            return this;
        }

        public BigEndianBuffer Int32(int value)
        {
            EnsureCapacity(4);
            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(_length), value);
            _length += 4;
            return this;
        }

        /// <summary>Writes an F2DOT14 fixed-point value (the variation-coordinate / region format).</summary>
        public BigEndianBuffer F2Dot14(double value)
            => Int16((int)Math.Round(value * 16384.0));

        /// <summary>Writes a 16.16 fixed-point value.</summary>
        public BigEndianBuffer Fixed(double value)
            => Int32((int)Math.Round(value * 65536.0));

        /// <summary>Writes a 4-character tag (space-padded / truncated to 4 bytes).</summary>
        public BigEndianBuffer Tag(string tag)
        {
            Span<byte> bytes = stackalloc byte[4] { 0x20, 0x20, 0x20, 0x20 };
            var n = Math.Min(4, tag.Length);
            for (var i = 0; i < n; i++)
            {
                bytes[i] = (byte)tag[i];
            }

            return Bytes(bytes);
        }

        public BigEndianBuffer Bytes(ReadOnlySpan<byte> bytes)
        {
            EnsureCapacity(bytes.Length);
            bytes.CopyTo(_buffer.AsSpan(_length));
            _length += bytes.Length;
            return this;
        }

        /// <summary>Writes <paramref name="count"/> zero bytes (padding / placeholder data).</summary>
        public BigEndianBuffer Zeros(int count)
        {
            EnsureCapacity(count);
            _length += count; // already zero-initialized
            return this;
        }

        /// <summary>Writes a placeholder big-endian <c>uint16</c> and returns its position for later patching.</summary>
        public int ReserveOffset16()
        {
            var pos = _length;
            UInt16(0);
            return pos;
        }

        /// <summary>Writes a placeholder big-endian <c>uint32</c> and returns its position for later patching.</summary>
        public int ReserveOffset32()
        {
            var pos = _length;
            UInt32(0);
            return pos;
        }

        /// <summary>Back-patches a big-endian <c>uint16</c> at a previously reserved position.</summary>
        public BigEndianBuffer PatchUInt16(int position, int value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(position, 2), checked((ushort)value));
            return this;
        }

        /// <summary>Back-patches a big-endian <c>uint32</c> at a previously reserved position.</summary>
        public BigEndianBuffer PatchUInt32(int position, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(position, 4), value);
            return this;
        }

        public byte[] ToArray() => _buffer.AsSpan(0, _length).ToArray();

        private void EnsureCapacity(int additional)
        {
            var required = _length + additional;
            if (required <= _buffer.Length)
            {
                return;
            }

            var newCapacity = _buffer.Length * 2;
            while (newCapacity < required)
            {
                newCapacity *= 2;
            }

            Array.Resize(ref _buffer, newCapacity);
        }
    }
}
