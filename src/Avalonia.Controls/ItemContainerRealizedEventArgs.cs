using System;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerRealized"/> event.
    /// </summary>
    public class ItemContainerRealizedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerRealizedEventArgs"/> class.
        /// </summary>
        /// <param name="container">The container control that was realized.</param>
        /// <param name="index">The index in the items source of the realized item</param>
        /// <param name="item">The item that will be displayed in the realized container.</param>
        public ItemContainerRealizedEventArgs(IControl container, int index, object? item)
        {
            Container = container;
            Index = index;
            Item = item;
        }

        /// <summary>
        /// Gets the container control that was realized.
        /// </summary>
        public IControl Container { get; }

        /// <summary>
        /// Gets the index in the items source of the realized item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the item that will be displayed in the realized container.
        /// </summary>
        public object? Item { get; }
    }
}
