using System;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Generates containers for an <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// Although this class is similar to that found in WPF/UWP, in Avalonia this class only
    /// concerns itself with generating and clearing item containers; it does not maintain a
    /// record of the currently realized containers, that responsibility is delegated to the
    /// items panel.
    /// </remarks>
    public class ItemContainerGenerator
    {
        private ItemsControl _owner;

        internal ItemContainerGenerator(ItemsControl owner) => _owner = owner;

        /// <summary>
        /// Creates a new container control.
        /// </summary>
        /// <returns>The newly created container control.</returns>
        /// <remarks>
        /// Before calling this method, <see cref="IsItemItsOwnContainer(Control)"/> should be
        /// called to determine whether the item itself should be used as a container. After
        /// calling this method, <see cref="PrepareItemContainer(Control, object, int)"/> should
        /// be called to prepare the container to display the specified item.
        /// </remarks>
        public Control CreateContainer() => _owner.CreateContainerForItemOverride();

        /// <summary>
        /// Determines whether the specified item is (or is eligible to be) its own container.
        /// </summary>
        /// <param name="container">The item.</param>
        /// <returns>true if the item is its own container, otherwise false.</returns>
        /// <remarks>
        /// Whereas in WPF/UWP, non-control items can be their own container, in Avalonia only
        /// control items may be; the caller is responsible for checking if each item is a control
        /// and calling this method before creating a new container.
        /// </remarks>
        public bool IsItemItsOwnContainer(Control container) => _owner.IsItemItsOwnContainerOverride(container);

        /// <summary>
        /// Prepares the specified element as the container for the corresponding item.
        /// </summary>
        /// <param name="container">The element that's used to display the specified item.</param>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item to display.</param>
        /// <remarks>
        /// If <see cref="IsItemItsOwnContainer(Control)"/> is true for an item, then this method
        /// only needs to be called a single time, otherwise this method should be called after the
        /// container is created, and each subsequent time the container is recycled to display a
        /// new item.
        /// </remarks>
        public void PrepareItemContainer(Control container, object? item, int index) => 
            _owner.PrepareItemContainer(container, item, index);

        /// <summary>
        /// Called when the index for a container changes due to an insertion or removal in the
        /// items collection.
        /// </summary>
        /// <param name="container">The container whose index changed.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        public void ItemContainerIndexChanged(Control container, int oldIndex, int newIndex) =>
            _owner.ItemContainerIndexChanged(container, oldIndex, newIndex);

        /// <summary>
        /// Undoes the effects of the <see cref="PrepareItemContainer(Control, object, int)"/> method.
        /// </summary>
        /// <param name="container">The element that's used to display the specified item.</param>
        public void ClearItemContainer(Control container) => _owner.ClearContainerForItemOverride(container);

        public Control? ContainerFromIndex(int index) => _owner.ContainerFromIndex(index);
        public int IndexFromContainer(Control container) => _owner.IndexFromContainer(container);
    }
}
