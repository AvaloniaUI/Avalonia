using System;
using System.ComponentModel;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Generates containers for an <see cref="ItemsControl"/>.
    /// </summary>
    /// <remarks>
    /// When creating a container for an item from a <see cref="VirtualizingPanel"/>, the following
    /// process should be followed:
    ///
    /// - <see cref="NeedsContainer(object, int, out object?)"/> should first be called to
    ///   determine whether the item needs a container. This method will return true if the item
    ///   should be wrapped in a container control, or false if the item itself can be used as a
    ///   container.
    /// - If <see cref="NeedsContainer(object, int, out object?)"/> returns true then the
    ///   <see cref="CreateContainer"/> method should be called to create a new container, passing
    ///   the recycle key returned from <see cref="NeedsContainer(object, int, out object?)"/>.
    /// - If the panel supports recycling and the recycle key is non-null then the recycle key
    ///   should be recorded for the container (e.g. in an attached property or the realized
    ///   container list).
    /// - <see cref="PrepareItemContainer(Control, object?, int)"/> method should be called for the
    ///   container.
    /// - The container should then be added to the panel using 
    ///   <see cref="VirtualizingPanel.AddInternalChild(Control)"/>
    /// - Finally, <see cref="ItemContainerPrepared(Control, object?, int)"/> should be called.
    /// 
    /// NOTE: If <see cref="NeedsContainer(object, int, out object?)"/> in the first step above
    /// returns false then the above steps should be carried out a single time: the first time the
    /// item is displayed. Otherwise the steps should be carried out each time a new container is
    /// realized for an item.
    ///
    /// When unrealizing a container, the following process should be followed:
    /// 
    /// - If <see cref="NeedsContainer(object, int, out object?)"/> for the item returned false
    ///   then the item cannot be unrealized or recycled.
    /// - Otherwise, <see cref="ClearItemContainer(Control)"/> should be called for the container
    /// - If recycling is supported by the panel and the container then the container should be
    ///   added to a recycle pool keyed on the recycle key returned from 
    ///   <see cref="NeedsContainer(object, int, out object?)"/>. It is assumed that recycled
    ///   containers will not be removed from the panel but instead hidden from view using
    ///   e.g. `container.IsVisible = false`.
    /// - If recycling is not supported then the container should be removed from the panel.
    ///
    /// When recycling an unrealized container, the following process should be followed:
    /// 
    /// - <see cref="NeedsContainer(object, int, out object?)"/> should be called to determine
    ///   whether the item needs a container, and if so, the recycle key.
    /// - A container should be taken from the recycle pool keyed on the returned recycle key.
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
        /// Determines whether the specified item needs to be wrapped in a container control.
        /// </summary>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="recycleKey">
        /// When the method returns, contains a key that can be used to locate a previously
        /// recycled container of the correct type, or null if the item cannot be recycled.
        /// </param>
        /// <returns>
        /// true if the item needs a container; otherwise false if the item can itself be used
        /// as a container.
        /// </returns>
        public bool NeedsContainer(object? item, int index, out object? recycleKey) =>
            _owner.NeedsContainerOverride(item, index, out recycleKey);

        /// <summary>
        /// Creates a new container control.
        /// </summary>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="recycleKey">
        /// The recycle key returned from <see cref="NeedsContainer(object, int, out object?)"/>
        /// </param>
        /// <returns>The newly created container control.</returns>
        /// <remarks>
        /// Before calling this method, <see cref="NeedsContainer(object, int, out object?)"/>
        /// should be called to determine whether the item itself should be used as a container.
        /// After calling this method, <see cref="PrepareItemContainer(Control, object, int)"/>
        /// must be called to prepare the container to display the specified item.
        /// 
        /// If the panel supports recycling then the returned recycle key should be stored alongside
        /// the container and when container becomes eligible for recycling the container should
        /// be placed in a recycle pool using this key. If the returned recycle key is null then
        /// the container cannot be recycled.
        /// </remarks>
        public Control CreateContainer(object? item, int index, object? recycleKey) 
            => _owner.CreateContainerForItemOverride(item, index, recycleKey);

        /// <summary>
        /// Prepares the specified element as the container for the corresponding item.
        /// </summary>
        /// <param name="container">The element that's used to display the specified item.</param>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item to display.</param>
        /// <remarks>
        /// If <see cref="NeedsContainer(object, int, out object?)"/> is false for an
        /// item, then this method must only be called a single time; otherwise this method must
        /// be called after the container is created, and each subsequent time the container is
        /// recycled to display a new item.
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
        /// This method must be called when a container has been fully prepared and added
        /// to the logical and visual trees, but may be called before a layout pass has completed.
        /// It must be called regardless of the result of
        /// <see cref="NeedsContainer(object, int, out object?)"/> but if that method returned
        /// false then must be called only a single time.
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
        /// <remarks>
        /// This method must be called when a container is unrealized. The container must have
        /// already have been removed from the virtualizing panel's list of realized containers before
        /// this method is called. This method must not be called if
        /// <see cref="NeedsContainer(object, int, out object?)"/> returned false for the item.
        /// </remarks>
        public void ClearItemContainer(Control container) => _owner.ClearItemContainer(container);

        [Obsolete("Use ItemsControl.ContainerFromIndex"), EditorBrowsable(EditorBrowsableState.Never)]
        public Control? ContainerFromIndex(int index) => _owner.ContainerFromIndex(index);

        [Obsolete("Use ItemsControl.IndexFromContainer"), EditorBrowsable(EditorBrowsableState.Never)]
        public int IndexFromContainer(Control container) => _owner.IndexFromContainer(container);
    }
}
