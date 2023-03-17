using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerPrepared"/> event.
    /// </summary>
    public class ContainerPreparedEventArgs : EventArgs
    {
        public ContainerPreparedEventArgs(Control container, int index)
        {
            Container = container;
            Index = index;
        }

        /// <summary>
        /// Gets the prepared container.
        /// </summary>
        public Control Container { get; }

        /// <summary>
        /// Gets the index of the item the container was prepared for.
        /// </summary>
        public int Index { get; }
    }
}
