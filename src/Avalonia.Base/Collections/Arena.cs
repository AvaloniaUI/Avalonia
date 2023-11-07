using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Collections;

/// <summary>
/// Reduces memory allocations by handing out <see cref="Span{T}"/> views of larger array chunks.
/// </summary>
/// <remarks>
/// Intended for scenarios in which many short-lived collections are required simultaneously.
/// </remarks>
internal class Arena<T>
{
    public const int DefaultChunkSize = 256;
    private readonly int _chunkSize;

    private GCHandle _array;
    private int _offset;

    private static readonly ThreadLocal<Arena<T>> s_threadArenas = new(() => new(DefaultChunkSize));
    
    /// <summary>
    /// Gets an <see cref="Arena{T}"/> for the current thread with the default chunk size.
    /// </summary>
    public static Arena<T> Current => s_threadArenas.Value!;

    public Arena(int chunkSize = DefaultChunkSize)
    {
        _chunkSize = chunkSize;
        _array = GCHandle.Alloc(null, GCHandleType.Weak);
    }

    ~Arena()
    {
        _array.Free();
    }

    /// <summary>
    /// Reserves and returns a section of the <see cref="Arena{T}"/>'s memory.
    /// </summary>
    /// <param name="size">The length of the returned <see cref="Span{T}"/>.</param>
    /// <returns>A <see cref="Span{T}"/> which points to a segment of a larger array allocated by the <see cref="Arena{T}"/>. <strong>Do not</strong> store long-term references to this object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than zero.</exception>
    public Span<T> Acquire(int size)
    {
        switch (size)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(size));
            case 0:
                return Span<T>.Empty;
        }

        if (size > _chunkSize)
        {
            Logging.Logger.Sink?.Log(Logging.LogEventLevel.Verbose, "MemAlloc", this, "A span which is larger than the chunk size was requested. This always requires the allocation of a new array.");
            return new Span<T>(new T[size]);
        }

        var array = (T[]?)_array.Target;

        if (array == null || _offset + size > _chunkSize)
        {
            _array.Target = array = new T[_chunkSize];
            _offset = 0;
        }

        var span = new Span<T>(array, _offset, size);

        _offset += size;

        return span;
    }

    /// <summary>
    /// Reserves a section of the <see cref="Arena{T}"/>'s memory, fills it with the values of <paramref name="source"/>, then returns it.
    /// </summary>
    /// <param name="source">A collection, the values of which will be copied into the returned <see cref="Span{T}"/>.</param>
    /// <returns>A <see cref="Span{T}"/> which points to a segment of a larger array allocated by the <see cref="Arena{T}"/>. <strong>Do not</strong> store long-term references to this object.</returns>
    public Span<T> AcquireCopyOf(IList<T> source)
    {
        var span = Acquire(source.Count);

        for (var i = 0; i < source.Count; i++)
        {
            span[i] = source[i];
        }

        return span;
    }

    /// <inheritdoc cref="AcquireCopyOf(IList{T})"/>
    public Span<T> AcquireCopyOf(ICollection<T> source)
    {
        var span = Acquire(source.Count);

        if (source.Count > 0)
        {
            int i = 0;
            foreach (var item in source)
            {
                span[i++] = item;
            }
        }

        return span;
    }
}
