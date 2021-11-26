using System;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerUnrealized"/> event.
    /// </summary>
    public class ItemContainerUnrealizedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerUnrealizedEventArgs"/> class.
        /// </summary>
        /// <param name="container">The container control that was realized.</param>
        /// <param name="index">The index in the items source of the realized item</param>
        public ItemContainerUnrealizedEventArgs(IControl container, int index)
        {
            Container = container;
            Index = index;
        }

        /// <summary>
        /// Gets the container control that was unrealized.
        /// </summary>
        public IControl Container { get; }

        /// <summary>
        /// Gets the index of the item in the items source that the container was displaying.
        /// </summary>
        public int Index { get; }
    }
}
