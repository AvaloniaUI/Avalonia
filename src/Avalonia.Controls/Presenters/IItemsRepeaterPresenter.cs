using System;

namespace Avalonia.Controls.Presenters
{
    public interface IItemsRepeaterPresenter : IItemsPresenter
    {
        /// <summary>
        /// Retrieves the index of the item from the data source that corresponds to the specified
        /// <see cref="IControl"/>.
        /// </summary>
        /// <param name="element">
        /// The element that corresponds to the item to get the index of.
        /// </param>
        /// <returns>
        /// The index of the item from the data source that corresponds to the specified UIElement,
        /// or -1 if the element is not supported.
        /// </returns>
        int GetElementIndex(IControl element);

        /// <summary>
        /// Retrieves the realized <see cref="IControl"/> that corresponds to the item at the
        /// specified index in the data source.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        /// The <see cref="IControl"/> that corresponds to the item at the specified index if the
        /// item is realized, or null if the item is not realized.
        /// </returns>
        IControl TryGetElement(int index);

        /// <summary>
        /// Occurs each time an element is prepared for use.
        /// </summary>
        /// <remarks>
        /// The prepared element might be newly created or an existing element that is being re-
        /// used.
        /// </remarks>
        public event EventHandler<ElementPreparedEventArgs> ElementPrepared;

        /// <summary>
        /// Occurs each time an element is cleared and made available to be re-used.
        /// </summary>
        /// <remarks>
        /// This event is raised immediately each time an element is cleared, such as when it falls
        /// outside the range of realized items. Elements are cleared when they become available
        /// for re-use.
        /// </remarks>
        public event EventHandler<ElementClearingEventArgs> ElementClearing;
    }
}
