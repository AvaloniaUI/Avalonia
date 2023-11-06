using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Collections;

/// <summary>
/// Reduces memory allocations by handing out <see cref="Span{T}"/> views of larger array chunks.
/// </summary>
/// <remarks>
/// Intended for scenarios in which many short-lived collections are required simultaneously.
/// </remarks>
public class Arena<T>
{
    private readonly int _chunkSize;

    private T[] _array;
    private int _offset;

    public Arena(int chunkSize = 256)
    {
        _chunkSize = chunkSize;
        _array = new T[_chunkSize];
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
            Debug.Fail("Requested an span which is larger than the chunk size. This always requires the allocation of a new array.");
            return new Span<T>(new T[size]);
        }

        if (_offset + size > _chunkSize)
        {
            _array = new T[_chunkSize];
            _offset = 0;
        }

        var span = new Span<T>(_array, _offset, size);

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

        int i = 0;
        foreach (var item in source)
        {
            span[i++] = item;
        }

        return span;
    }
}
