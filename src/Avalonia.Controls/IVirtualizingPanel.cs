using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel that can be used to virtualize items in an <see cref="ItemsControl"/>
    /// </summary>
    public interface IVirtualizingPanel : IPanel
    {
        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        IEnumerable<IControl> RealizedElements { get; }

        /// <summary>
        /// Gets the element for the specified index in the item source, if realized.
        /// </summary>
        /// <param name="index">The index in the item source.</param>
        /// <returns>
        /// The element; or null if not realized or index is invalid.
        /// </returns>
        IControl? GetElementForIndex(int index);

        /// <summary>
        /// Gets the index in the item source of the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>
        /// The index in the item source; or -1 if the control does not belong to this panel, is
        /// unrealized, or represents an element without an index.
        /// </returns>
        int GetElementIndex(IControl element);
    }
}
