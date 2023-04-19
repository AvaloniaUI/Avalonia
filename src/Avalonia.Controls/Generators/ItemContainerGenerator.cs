using System;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Generates containers for an <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// When creating a container for an item from a <see cref="VirtualizingPanel"/>, the following
    /// process should be followed:
    /// 
    /// - <see cref="GetContainerTypeForItem(object)"/> should first be called to get the container
    ///   type for the item. If the returned value is <see cref="ItemContainerType.ItemIsOwnContainer"/>
    ///   then the item itself should be used as a container.
    /// - Otherwise the <see cref="ItemContainerType"/> returned from
    ///   <see cref="GetContainerTypeForItem(object)"/> should be passed to 
    ///   <see cref="CreateContainer(ItemContainerType)"/> to create a new container.
    /// - <see cref="PrepareItemContainer(Control, object?, int)"/> method should be called for the
    ///   container.
    /// - The container should then be added to the panel using 
    ///   <see cref="VirtualizingPanel.AddInternalChild(Control)"/>
    /// - Finally, <see cref="ItemContainerPrepared(Control, object?, int)"/> should be called.
    /// 
    /// When unrealizing a container, the following process should be followed:
    /// 
    /// - If <see cref="GetContainerTypeForItem(object)"/> for the item returned
    ///   <see cref="ItemContainerType.ItemIsOwnContainer"/> then the item cannot be unrealized or
    ///   recycled.
    /// - Otherwise, <see cref="ClearItemContainer(Control)"/> should be called for the container
    /// - If recycling is supported then the container should be added to a recycle pool with
    ///   its <see cref="ItemContainerType"/> as the key.
    /// - It is assumed that recycled containers will not be removed from the panel but instead
    ///   hidden from view using e.g. `container.IsVisible = false`.
    /// 
    /// When recycling an unrealized container, the following process should be  
    /// 
    /// - <see cref="GetContainerTypeForItem(object)"/> should first be called to get the container
    ///   type for the item.
    /// - An element should be taken from the recycle pool using the <see cref="ItemContainerType"/>
    ///   as a key.
    /// - The container should be made visible.
    /// - <see cref="PrepareItemContainer(Control, object?, int)"/> method should be called for the
    ///   container.
    /// - <see cref="ItemContainerPrepared(Control, object?, int)"/> should be called.
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
        /// Determines the container type for the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// An <see cref="ItemContainerType"/>.
        /// </returns>
        /// <remarks>
        /// If the returned value is <see cref="ItemContainerType.ItemIsOwnContainer"/> then the
        /// item itself should be used as a container, otherwise the returned type should be passed
        /// to <see cref="CreateContainer(ItemContainerType)"/> to create a new container, or used
        /// as a recycle key.
        /// </remarks>
        public ItemContainerType GetContainerTypeForItem(object? item) => _owner.GetContainerTypeForItemOverride(item);

        /// <summary>
        /// Creates a new container control.
        /// </summary>
        /// <param name="type">
        /// The container type returned from <see cref="GetContainerTypeForItem(object)"/>.
        /// </param>
        /// <returns>The newly created container control.</returns>
        /// <remarks>
        /// Before calling this method, <see cref="GetContainerTypeForItem(object)"/> should be
        /// called to determine the container type. If that method returns
        /// <see cref="ItemContainerType.ItemIsOwnContainer"/> then this method should *not* be
        /// called.
        /// 
        /// After calling this method, <see cref="PrepareItemContainer(Control, object, int)"/>
        /// should be called to prepare the container to display the specified item.
        /// </remarks>
        public Control CreateContainer(ItemContainerType type) => _owner.CreateContainerForItem(type);

        /// <summary>
        /// Prepares the specified element as the container for the corresponding item.
        /// </summary>
        /// <param name="container">The element that's used to display the specified item.</param>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item to display.</param>
        /// <remarks>
        /// If <see cref="GetContainerTypeForItem(object)"/> returned
        /// <see cref="ItemContainerType.ItemIsOwnContainer"/> for an item, then this method
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
        /// <see cref="GetContainerTypeForItem(object)"/>.
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
