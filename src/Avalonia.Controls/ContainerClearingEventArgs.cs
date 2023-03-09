using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ItemsControl.ContainerClearing"/> event.
    /// </summary>
    public class ContainerClearingEventArgs : EventArgs
    {
        public ContainerClearingEventArgs(Control container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the prepared container.
        /// </summary>
        public Control Container { get; }
    }
}
