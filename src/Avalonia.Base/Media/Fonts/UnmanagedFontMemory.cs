using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a memory manager for unmanaged font data, providing functionality to access and manage font memory
    /// and OpenType table data.
    /// </summary>
    /// <remarks>This class encapsulates unmanaged memory containing font data and provides methods to
    /// retrieve specific OpenType table data. It ensures thread-safe access to the memory and supports pinning for
    /// interoperability scenarios. Instances of this class must be properly disposed to release unmanaged
    /// resources.</remarks>
    internal sealed unsafe class UnmanagedFontMemory : MemoryManager<byte>, IFontMemory
    {
        private IntPtr _ptr;
        private int _length;
        private int _pinCount;

        // Reader/writer lock to protect lifetime and cache access.
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Represents a cache of font table data, where each entry maps an OpenType tag to its corresponding byte data.
        /// </summary>
        /// <remarks>This dictionary is used to store preloaded font table data for efficient access.  The
        /// keys are OpenType tags, which identify specific font tables, and the values are the corresponding  byte data
        /// stored as read-only memory. This ensures that the data cannot be modified after being loaded into the
        /// cache.</remarks>
        private readonly Dictionary<OpenTypeTag, ReadOnlyMemory<byte>> _tableCache = [];

        private UnmanagedFontMemory(IntPtr ptr, int length)
        {
            _ptr = ptr;
            _length = length;
        }

        /// <summary>
        /// Attempts to retrieve the memory region corresponding to the specified OpenType table tag.
        /// </summary>
        /// <remarks>This method searches for the specified OpenType table in the font data and retrieves
        /// its memory region if found. The method performs bounds checks to ensure the requested table is valid and
        /// safely accessible. If the table is not found or the font data is invalid, the method returns <see
        /// langword="false"/>.</remarks>
        /// <param name="tag">The <see cref="OpenTypeTag"/> identifying the table to retrieve. Must not be <see cref="OpenTypeTag.None"/>.</param>
        /// <param name="table">When this method returns, contains the memory region of the requested table if the operation succeeds;
        /// otherwise, contains the default value.</param>
        /// <returns><see langword="true"/> if the table memory was successfully retrieved; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the font memory has been disposed.</exception>
        public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
        {
            table = default;

            // Validate tag
            if (tag == OpenTypeTag.None)
            {
                return false;
            }

            _lock.EnterUpgradeableReadLock();

            try
            {
                if (_ptr == IntPtr.Zero || _length < 12)
                {
                    return false;
                }

                // Create a span over the unmanaged memory (read-only view)
                var fontData = Memory.Span;

                // Minimal SFNT header: 4 (sfnt) + 2 (numTables) + 6 (rest) = 12
                if (fontData.Length < 12)
                {
                    return false;
                }

                // Check cache first
                if (_tableCache.TryGetValue(tag, out var cached))
                {
                    table = cached;

                    return true;
                }

                // Parse table directory
                var numTables = BinaryPrimitives.ReadUInt16BigEndian(fontData.Slice(4, 2));
                var recordsStart = 12;
                var requiredDirectoryBytes = checked(recordsStart + numTables * 16);

                if (fontData.Length < requiredDirectoryBytes)
                {
                    return false;
                }

                for (int i = 0; i < numTables; i++)
                {
                    var entryOffset = recordsStart + i * 16;
                    var entrySlice = fontData.Slice(entryOffset, 16);
                    var entryTag = (OpenTypeTag)BinaryPrimitives.ReadUInt32BigEndian(entrySlice.Slice(0, 4));

                    if (entryTag != tag)
                    {
                        continue;
                    }

                    var offset = BinaryPrimitives.ReadUInt32BigEndian(entrySlice.Slice(8, 4));
                    var length = BinaryPrimitives.ReadUInt32BigEndian(entrySlice.Slice(12, 4));

                    // Bounds checks - ensure values fit within the span
                    if (offset > (uint)fontData.Length || length > (uint)fontData.Length)
                    {
                        return false;
                    }

                    if (offset + length > (uint)fontData.Length)
                    {
                        return false;
                    }

                    // Safe to cast to int for Slice since we validated bounds
                    table = Memory.Slice((int)offset, (int)length);

                    // Acquire write lock to update cache
                    _lock.EnterWriteLock();

                    try
                    {
                        // Cache the result for faster subsequent lookups
                        _tableCache[tag] = table;

                        return true;
                    }
                    finally
                    {
                        // Release write lock
                        _lock.ExitWriteLock();
                    }
                }

                return false;
            }
            finally
            {
                // Release upgradeable read lock
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Loads font data from the specified stream into unmanaged memory.
        /// </summary>
        public static UnmanagedFontMemory LoadFromStream(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream is not readable", nameof(stream));
            }

            if (stream.CanSeek)
            {
                var length = checked((int)stream.Length);
                var ptr = Marshal.AllocHGlobal(length);
                var buffer = ArrayPool<byte>.Shared.Rent(8192);

                try
                {
                    var remaining = length;
                    var offset = 0;

                    while (remaining > 0)
                    {
                        var toRead = Math.Min(buffer.Length, remaining);
                        var read = stream.Read(buffer, 0, toRead);

                        if (read == 0)
                        {
                            break;
                        }

                        Marshal.Copy(buffer, 0, ptr + offset, read);

                        offset += read;

                        remaining -= read;
                    }

                    return new UnmanagedFontMemory(ptr, offset);
                }
                catch
                {
                    Marshal.FreeHGlobal(ptr);
                    throw;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                using var ms = new MemoryStream();

                stream.CopyTo(ms);

                var len = checked((int)ms.Length);

                var buffer = ms.GetBuffer();

                // GetBuffer may return a larger array than the actual data length.
                return CreateFromBytes(new ReadOnlySpan<byte>(buffer, 0, len));
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="UnmanagedFontMemory"/> from the specified byte data.
        /// </summary>
        /// <remarks>The method allocates unmanaged memory to store the provided byte data. The caller is
        /// responsible for ensuring that the returned <see cref="UnmanagedFontMemory"/> instance is properly disposed
        /// to release the allocated memory.</remarks>
        /// <param name="data">A read-only span of bytes representing the font data. The span must not be empty.</param>
        /// <returns>An instance of <see cref="UnmanagedFontMemory"/> that encapsulates the unmanaged memory containing the font
        /// data.</returns>
        private static UnmanagedFontMemory CreateFromBytes(ReadOnlySpan<byte> data)
        {
            var len = data.Length;
            var ptr = Marshal.AllocHGlobal(len);

            try
            {
                if (len > 0)
                {
                    unsafe
                    {
                        data.CopyTo(new Span<byte>((void*)ptr, len));
                    }
                }

                return new UnmanagedFontMemory(ptr, len);
            }
            catch
            {
                Marshal.FreeHGlobal(ptr);
                throw;
            }
        }

        // Implement MemoryManager<byte> members on the owner
        public override Span<byte> GetSpan()
        {
            _lock.EnterReadLock();

            try
            {
                if (_ptr == IntPtr.Zero || _length <= 0)
                {
                    return Span<byte>.Empty;
                }

                unsafe
                {
                    return new Span<byte>((void*)_ptr.ToPointer(), _length);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if (elementIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementIndex));
            }

            // Increment pin count first to prevent dispose racing with pin.
            Interlocked.Increment(ref _pinCount);

            // Validate state under lock
            _lock.EnterReadLock();

            try
            {
                if (_ptr == IntPtr.Zero || _length == 0)
                {
                    return new MemoryHandle();
                }

                if (elementIndex > _length)
                {
                    throw new ArgumentOutOfRangeException(nameof(elementIndex));
                }

                unsafe
                {
                    var p = (byte*)_ptr.ToPointer() + elementIndex;
                    return new MemoryHandle(p);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public override void Unpin()
        {
            // Decrement pin count
            Interlocked.Decrement(ref _pinCount);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            // Always use lock for disposal since we don't have a finalizer
            _lock.EnterWriteLock();

            try
            {
                if (Volatile.Read(ref _pinCount) > 0)
                {
                    throw new InvalidOperationException("Cannot dispose while memory is pinned.");
                }

                if (_ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_ptr);
                    _ptr = IntPtr.Zero;
                }

                _length = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
                _lock.Dispose();
            }
        }
    }
}
