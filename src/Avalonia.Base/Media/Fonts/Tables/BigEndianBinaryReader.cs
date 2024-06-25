// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// BinaryReader using big-endian encoding.
    /// </summary>
    [DebuggerDisplay("Start: {StartOfStream}, Position: {BaseStream.Position}")]
    internal class BigEndianBinaryReader : IDisposable
    {
        /// <summary>
        /// Buffer used for temporary storage before conversion into primitives
        /// </summary>
        private readonly byte[] _buffer = new byte[16];

        private readonly bool _leaveOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigEndianBinaryReader" /> class.
        /// Constructs a new binary reader with the given bit converter, reading
        /// to the given stream, using the given encoding.
        /// </summary>
        /// <param name="stream">Stream to read data from</param>
        /// <param name="leaveOpen">if set to <c>true</c> [leave open].</param>
        public BigEndianBinaryReader(Stream stream, bool leaveOpen)
        {
            BaseStream = stream;
            StartOfStream = stream.Position;
            _leaveOpen = leaveOpen;
        }

        private long StartOfStream { get; }

        /// <summary>
        /// Gets the underlying stream of the EndianBinaryReader.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Seeks within the stream.
        /// </summary>
        /// <param name="offset">Offset to seek to.</param>
        /// <param name="origin">Origin of seek operation. If SeekOrigin.Begin, the offset will be set to the start of stream position.</param>
        public void Seek(long offset, SeekOrigin origin)
        {
            // If SeekOrigin.Begin, the offset will be set to the start of stream position.
            if (origin == SeekOrigin.Begin)
            {
                offset += StartOfStream;
            }

            BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// Reads a single byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public byte ReadByte()
        {
            ReadInternal(_buffer, 1);
            return _buffer[0];
        }

        /// <summary>
        /// Reads a single signed byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public sbyte ReadSByte()
        {
            ReadInternal(_buffer, 1);
            return unchecked((sbyte)_buffer[0]);
        }

        public float ReadF2dot14()
        {
            const float f2Dot14ToFloat = 16384.0f;
            return ReadInt16() / f2Dot14ToFloat;
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream, using the bit converter
        /// for this reader. 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit integer read</returns>
        public short ReadInt16()
        {
            ReadInternal(_buffer, 2);

            return BinaryPrimitives.ReadInt16BigEndian(_buffer);
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

        /// <summary>
        /// Reads a fixed 32-bit value from the stream.
        /// 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit value read.</returns>
        public float ReadFixed()
        {
            ReadInternal(_buffer, 4);
            return BinaryPrimitives.ReadInt32BigEndian(_buffer) / 65536F;
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit integer read</returns>
        public int ReadInt32()
        {
            ReadInternal(_buffer, 4);

            return BinaryPrimitives.ReadInt32BigEndian(_buffer);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream.
        /// 8 bytes are read.
        /// </summary>
        /// <returns>The 64-bit integer read.</returns>
        public long ReadInt64()
        {
            ReadInternal(_buffer, 8);

            return BinaryPrimitives.ReadInt64BigEndian(_buffer);
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the stream.
        /// 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit unsigned integer read.</returns>
        public ushort ReadUInt16()
        {
            ReadInternal(_buffer, 2);

            return BinaryPrimitives.ReadUInt16BigEndian(_buffer);
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the stream representing an offset position.
        /// 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit unsigned integer read.</returns>
        public ushort ReadOffset16() => ReadUInt16();

        public TEnum ReadUInt16<TEnum>()
            where TEnum : struct, Enum
        {
            TryConvert(ReadUInt16(), out TEnum value);
            return value;
        }

        /// <summary>
        /// Reads array of 16-bit unsigned integers from the stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The 16-bit unsigned integer read.
        /// </returns>
        public ushort[] ReadUInt16Array(int length)
        {
            ushort[] data = new ushort[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = ReadUInt16();
            }

            return data;
        }

        /// <summary>
        /// Reads array of 16-bit unsigned integers from the stream to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read to.</param>
        public void ReadUInt16Array(Span<ushort> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = ReadUInt16();
            }
        }

        /// <summary>
        /// Reads array or 32-bit unsigned integers from the stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The 32-bit unsigned integer read.
        /// </returns>
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

            ReadInternal(data, length);

            return data;
        }

        /// <summary>
        /// Reads array of 16-bit unsigned integers from the stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The 16-bit signed integer read.
        /// </returns>
        public short[] ReadInt16Array(int length)
        {
            short[] data = new short[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = ReadInt16();
            }

            return data;
        }

        /// <summary>
        /// Reads an array of 16-bit signed integers from the stream to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read to.</param>
        public void ReadInt16Array(Span<short> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = ReadInt16();
            }
        }

        /// <summary>
        /// Reads a 8-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 1 bytes are read.
        /// </summary>
        /// <returns>The 8-bit unsigned integer read.</returns>
        public byte ReadUInt8()
        {
            ReadInternal(_buffer, 1);
            return _buffer[0];
        }

        /// <summary>
        /// Reads a 24-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 3 bytes are read.
        /// </summary>
        /// <returns>The 24-bit unsigned integer read.</returns>
        public int ReadUInt24()
        {
            byte highByte = ReadByte();
            return (highByte << 16) | ReadUInt16();
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit unsigned integer read.</returns>
        public uint ReadUInt32()
        {
            ReadInternal(_buffer, 4);

            return BinaryPrimitives.ReadUInt32BigEndian(_buffer);
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the stream representing an offset position.
        /// 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit unsigned integer read.</returns>
        public uint ReadOffset32() => ReadUInt32();

        /// <summary>
        /// Reads the specified number of bytes, returning them in a new byte array.
        /// If not enough bytes are available before the end of the stream, this
        /// method will return what is available.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The bytes read.</returns>
        public byte[] ReadBytes(int count)
        {
            byte[] ret = new byte[count];
            int index = 0;
            while (index < count)
            {
                int read = BaseStream.Read(ret, index, count - index);

                // Stream has finished half way through. That's fine, return what we've got.
                if (read == 0)
                {
                    byte[] copy = new byte[index];
                    Buffer.BlockCopy(ret, 0, copy, 0, index);
                    return copy;
                }

                index += read;
            }

            return ret;
        }

        /// <summary>
        /// Reads a string of a specific length, which specifies the number of bytes
        /// to read from the stream. These bytes are then converted into a string with
        /// the encoding for this reader.
        /// </summary>
        /// <param name="bytesToRead">The bytes to read.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>
        /// The string read from the stream.
        /// </returns>
        public string ReadString(int bytesToRead, Encoding encoding)
        {
            byte[] data = new byte[bytesToRead];
            ReadInternal(data, bytesToRead);
            return encoding.GetString(data, 0, data.Length);
        }

        /// <summary>
        /// Reads the uint32 string.
        /// </summary>
        /// <returns>a 4 character long UTF8 encoded string.</returns>
        public string ReadTag()
        {
            ReadInternal(_buffer, 4);

            return Encoding.UTF8.GetString(_buffer, 0, 4);
        }

        /// <summary>
        /// Reads an offset consuming the given nuber of bytes.
        /// </summary>
        /// <param name="size">The offset size in bytes.</param>
        /// <returns>The 32-bit signed integer representing the offset.</returns>
        /// <exception cref="InvalidOperationException">Size is not in range.</exception>
        public int ReadOffset(int size)
            => size switch
            {
                1 => ReadByte(),
                2 => (ReadByte() << 8) | (ReadByte() << 0),
                3 => (ReadByte() << 16) | (ReadByte() << 8) | (ReadByte() << 0),
                4 => (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | (ReadByte() << 0),
                _ => throw new InvalidOperationException(),
            };

        /// <summary>
        /// Reads the given number of bytes from the stream, throwing an exception
        /// if they can't all be read.
        /// </summary>
        /// <param name="data">Buffer to read into.</param>
        /// <param name="size">Number of bytes to read.</param>
        private void ReadInternal(byte[] data, int size)
        {
            int index = 0;

            while (index < size)
            {
                int read = BaseStream.Read(data, index, size - index);
                if (read == 0)
                {
                    throw new EndOfStreamException($"End of stream reached with {size - index} byte{(size - index == 1 ? "s" : string.Empty)} left to read.");
                }

                index += read;
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
            {
                BaseStream?.Dispose();
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
