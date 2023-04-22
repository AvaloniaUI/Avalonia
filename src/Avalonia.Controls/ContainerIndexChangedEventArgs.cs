using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerIndexChanged"/> event.
    /// </summary>
    public class ContainerIndexChangedEventArgs : EventArgs
    {
        public ContainerIndexChangedEventArgs(Control container, int oldIndex, int newIndex)
        {
            Container = container;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        /// <summary>
        /// Get the container for which the index changed.
        /// </summary>
        public Control Container { get; }

        /// <summary>
        /// Gets the index of the container after the change.
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        /// Gets the index of the container before the change.
        /// </summary>
        public int OldIndex { get; }
    }
}
