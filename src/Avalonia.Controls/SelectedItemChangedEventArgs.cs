using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="TreeView.SelectedItemChanged"/> event.
    /// </summary>
    public class SelectedItemChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedItemChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The event being raised.</param>
        /// <param name="newItem">The items added to the selection.</param>
        /// <param name="oldItem">The items removed from the selection.</param>
        public SelectedItemChangedEventArgs(RoutedEvent routedEvent, object newItem, object oldItem)
                : base(routedEvent)
        {
            NewItem = newItem;
            OldItem = oldItem;
        }

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public object NewItem { get; }

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public object OldItem { get; }
    }
}
