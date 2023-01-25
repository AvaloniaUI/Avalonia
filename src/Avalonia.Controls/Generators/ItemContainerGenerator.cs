using System;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Generates containers for an <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// When creating a container for an item from a <see cref="VirtualizingPanel"/>, the following
    /// method order should be followed:
    /// 
    /// - <see cref="IsItemItsOwnContainer(Control)"/> should first be called if the item is
    ///   derived from the <see cref="Control"/> class. If this method returns true then the
    ///   item itself should be used as the container.
    /// - If <see cref="IsItemItsOwnContainer(Control)"/> returns false then
    ///   <see cref="CreateContainer"/> should be called to create a new container.
    /// - <see cref="PrepareItemContainer(Control, object?, int)"/> method should be called for the
    ///   container.
    /// - The container should then be added to the panel using 
    ///   <see cref="VirtualizingPanel.AddInternalChild(Control)"/>
    /// - Finally, <see cref="ItemContainerPrepared(Control, object?, int)"/> should be called.
    /// - When the item is ready to be recycled, <see cref="ClearItemContainer(Control)"/> should
    ///   be called if <see cref="IsItemItsOwnContainer(Control)"/> returned false.
    /// 
    /// NOTE: Although this class is similar to that found in WPF/UWP, in Avalonia this class only
    /// concerns itself with generating and clearing item containers; it does not maintain a
    /// record of the currently realized containers, that responsibility is delegated to the
    /// items panel.
    /// </remarks>
    public class ItemContainerGenerator
    {
        private readonly ItemsControl _owner;

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
        /// Notifies the <see cref="ItemsControl"/> that a container has been fully prepared to
        /// display an item.
        /// </summary>
        /// <param name="container">The container control.</param>
        /// <param name="item">The item being displayed.</param>
        /// <param name="index">The index of the item being displayed.</param>
        /// <remarks>
        /// This method should be called when a container has been fully prepared and added
        /// to the logical and visual trees, but may be called before a layout pass has completed.
        /// It should be called regardless of the result of
        /// <see cref="IsItemItsOwnContainer(Control)"/>.
        /// </remarks>
        public void ItemContainerPrepared(Control container, object? item, int index) =>
            _owner.ItemContainerPrepared(container, item, index);

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
        /// <param name="container">The container control.</param>
        public void ClearItemContainer(Control container) => _owner.ClearItemContainer(container);

        [Obsolete("Use ItemsControl.ContainerFromIndex")]
        public Control? ContainerFromIndex(int index) => _owner.ContainerFromIndex(index);

        [Obsolete("Use ItemsControl.IndexFromContainer")]
        public int IndexFromContainer(Control container) => _owner.IndexFromContainer(container);
    }
}
