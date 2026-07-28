// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// BinaryReader using big-endian encoding for ReadOnlySpan&lt;byte&gt;.
    /// </summary>
    [DebuggerDisplay("Start: {StartOfSpan}, Position: {Position}")]
    internal ref struct BigEndianBinaryReader
    {
        private readonly ReadOnlySpan<byte> _span;
        private int _position;
        private readonly int _startOfSpan;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigEndianBinaryReader" /> class.
        /// </summary>
        /// <param name="span">Span to read data from</param>
        public BigEndianBinaryReader(ReadOnlySpan<byte> span)
        {
            _span = span;
            _position = 0;
            _startOfSpan = 0;
        }

        private readonly int StartOfSpan => _startOfSpan;

        /// <summary>
        /// Gets the current position in the span.
        /// </summary>
        public readonly int Position => _position;

        /// <summary>
        /// Seeks within the span.
        /// </summary>
        /// <param name="offset">Offset to seek to.</param>
        public void Seek(int offset)
        {
            int absoluteOffset = _startOfSpan + offset;

            if (offset < 0 || absoluteOffset > _span.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _position = absoluteOffset;
        }

        public byte ReadByte()
        {
            EnsureAvailable(1);

            return _span[_position++];
        }

        public sbyte ReadSByte()
        {
            EnsureAvailable(1);

            return unchecked((sbyte)_span[_position++]);
        }

        public float ReadF2dot14()
        {
            const float f2Dot14ToFloat = 16384.0f;

            return ReadInt16() / f2Dot14ToFloat;
        }

        public short ReadInt16()
        {
            EnsureAvailable(2);

            short value = BinaryPrimitives.ReadInt16BigEndian(_span.Slice(_position, 2));

            _position += 2;

            return value;
        }

        public TEnum ReadInt16<TEnum>()
            where TEnum : struct, Enum
        {
            TryConvert(ReadUInt16(), out TEnum value);

            return value;
        }

        public short ReadFWORD() => ReadInt16();

        public short[] ReadFWORDArray(int length) => ReadInt16Array(length);

        public ushort ReadUFWORD() => ReadUInt16();

        public float ReadFixed()
        {
            EnsureAvailable(4);

            float value = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_position, 4)) / 65536F;

            _position += 4;

            return value;
        }

        public FontVersion ReadVersion16Dot16()
        {
            EnsureAvailable(4);

            uint value = BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(_position, 4));

            _position += 4;

            return new FontVersion(value);
        }

        public int ReadInt32()
        {
            EnsureAvailable(4);

            int value = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_position, 4));

            _position += 4;

            return value;
        }

        public long ReadInt64()
        {
            EnsureAvailable(8);

            long value = BinaryPrimitives.ReadInt64BigEndian(_span.Slice(_position, 8));

            _position += 8;

            return value;
        }

        public ushort ReadUInt16()
        {
            EnsureAvailable(2);

            ushort value = BinaryPrimitives.ReadUInt16BigEndian(_span.Slice(_position, 2));

            _position += 2;

            return value;
        }

        public ushort ReadOffset16() => ReadUInt16();

        public TEnum ReadUInt16<TEnum>()
            where TEnum : struct, Enum
        {
            TryConvert(ReadUInt16(), out TEnum value);

            return value;
        }

        public ushort[] ReadUInt16Array(int length)
        {
            ushort[] data = new ushort[length];

            for (int i = 0; i < length; i++)
            {
                data[i] = ReadUInt16();
            }

            return data;
        }

        public void ReadUInt16Array(Span<ushort> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = ReadUInt16();
            }
        }

        public uint[] ReadUInt32Array(int length)
        {
            uint[] data = new uint[length];

            for (int i = 0; i < length; i++)
            {
                data[i] = ReadUInt32();
            }

            return data;
        }

        public byte[] ReadUInt8Array(int length)
        {
            byte[] data = new byte[length];

            ReadBytesInternal(data, length);

            return data;
        }

        public short[] ReadInt16Array(int length)
        {
            short[] data = new short[length];

            for (int i = 0; i < length; i++)
            {
                data[i] = ReadInt16();
            }

            return data;
        }

        public void ReadInt16Array(Span<short> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = ReadInt16();
            }
        }

        public byte ReadUInt8()
        {
            EnsureAvailable(1);

            return _span[_position++];
        }

        public int ReadUInt24()
        {
            byte highByte = ReadByte();

            return (highByte << 16) | ReadUInt16();
        }

        public uint ReadUInt32()
        {
            EnsureAvailable(4);

            uint value = BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(_position, 4));

            _position += 4;

            return value;
        }

        public uint ReadOffset32() => ReadUInt32();

        public byte[] ReadBytes(int count)
        {
            int available = Math.Min(count, _span.Length - _position);

            byte[] ret = new byte[available];

            ReadBytesInternal(ret, available);

            return ret;
        }

        public string ReadString(int bytesToRead, Encoding encoding)
        {
            EnsureAvailable(bytesToRead);

            string result = encoding.GetString(_span.Slice(_position, bytesToRead));

            _position += bytesToRead;

            return result;
        }

        public string ReadTag()
        {
            EnsureAvailable(4);

            string tag = Encoding.UTF8.GetString(_span.Slice(_position, 4));

            _position += 4;

            return tag;
        }

        public int ReadOffset(int size)
            => size switch
            {
                1 => ReadByte(),
                2 => (ReadByte() << 8) | (ReadByte() << 0),
                3 => (ReadByte() << 16) | (ReadByte() << 8) | (ReadByte() << 0),
                4 => (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | (ReadByte() << 0),
                _ => throw new InvalidOperationException(),
            };

        private void ReadBytesInternal(byte[] data, int size)
        {
            EnsureAvailable(size);

            _span.Slice(_position, size).CopyTo(data);

            _position += size;
        }

        private readonly void EnsureAvailable(int size)
        {
            if (_position + size > _span.Length)
            {
                throw new InvalidOperationException($"End of span reached with {size - (_span.Length - _position)} byte{(size - (_span.Length - _position) == 1 ? "s" : string.Empty)} left to read.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvert<T, TEnum>(T input, out TEnum value)
            where T : struct, IConvertible, IFormattable, IComparable
            where TEnum : struct, Enum
        {
            if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<TEnum>())
            {
                value = Unsafe.As<T, TEnum>(ref input);
                return true;
            }

            value = default;
            return false;
        }
    }
}
