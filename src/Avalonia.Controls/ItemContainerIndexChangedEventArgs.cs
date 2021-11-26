using System;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerIndexChanged"/> event.
    /// </summary>
    public class ItemContainerIndexChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerIndexChangedEventArgs"/> class.
        /// </summary>
        /// <param name="container">The container control.</param>
        /// <param name="oldIndex">The old index in the items source.</param>
        /// <param name="newIndex">The new index in the items source.</param>
        public ItemContainerIndexChangedEventArgs(IControl container, int oldIndex, int newIndex)
        {
            Container = container;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        /// <summary>
        /// Gets the container control whose index was changed.
        /// </summary>
        public IControl Container { get; }

        /// <summary>
        /// Gets the old index of the item in the items source.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// Gets the new index of the item in the items source.
        /// </summary>
        public int NewIndex { get; }
    }
}
