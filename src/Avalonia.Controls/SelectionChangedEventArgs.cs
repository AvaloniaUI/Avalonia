using System.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="SelectingItemsControl.SelectionChanged"/> event.
    /// </summary>
    public class SelectionChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The event being raised.</param>
        /// <param name="removedItems">The items removed from the selection.</param>
        /// <param name="addedItems">The items added to the selection.</param>
        public SelectionChangedEventArgs(RoutedEvent routedEvent, IList removedItems, IList addedItems)
            : base(routedEvent)
        {
            RemovedItems = removedItems;
            AddedItems = addedItems;
        }

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public IList AddedItems { get; }

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public IList RemovedItems { get; }
    }
}
