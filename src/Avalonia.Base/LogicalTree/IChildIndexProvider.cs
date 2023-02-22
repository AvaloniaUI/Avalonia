#nullable enable
using System;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Child's index and total count information provider used by list-controls (ListBox, StackPanel, etc.)
    /// </summary>
    /// <remarks>
    /// Used by nth-child and nth-last-child selectors. 
    /// </remarks>
    public interface IChildIndexProvider
    {
        /// <summary>
        /// Gets child's actual index in order of the original source.
        /// </summary>
        /// <param name="child">Logical child.</param>
        /// <returns>Index or -1 if child was not found.</returns>
        int GetChildIndex(ILogical child);

        /// <summary>
        /// Total children count or null if source is infinite.
        /// Some Avalonia features might not work if <see cref="TryGetTotalCount"/> returns false, for instance: nth-last-child selector.
        /// </summary>
        bool TryGetTotalCount(out int count);

        /// <summary>
        /// Notifies subscriber when a child's index was changed.
        /// </summary>
        event EventHandler<ChildIndexChangedEventArgs>? ChildIndexChanged;
    }
}
