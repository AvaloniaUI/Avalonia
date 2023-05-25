// This source file is adapted from the Collections.Pooled.
// (https://github.com/jtmueller/Collections.Pooled/tree/master/Collections.Pooled/)

using System;
using System.Collections.Generic;

namespace Avalonia.Collections.Pooled
{
    /// <summary>
    /// Represents a read-only collection of pooled elements that can be accessed by index
    /// </summary>
    /// <typeparam name="T">The type of elements in the read-only pooled list.</typeparam>

    internal interface IReadOnlyPooledList<T> : IReadOnlyList<T>
    {
#pragma warning disable CS0419
        /// <summary>
        /// Gets a <see cref="System.ReadOnlySpan{T}"/> for the items currently in the collection.
        /// </summary>
#pragma warning restore CS0419
        ReadOnlySpan<T> Span { get; }
    }
}
