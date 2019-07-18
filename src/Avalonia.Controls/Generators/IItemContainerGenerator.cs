// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public interface IItemContainerGenerator
    {
        /// <summary>
        /// Gets the currently realized containers.
        /// </summary>
        IEnumerable<ItemContainerInfo> Containers { get; }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        IDataTemplate ItemTemplate { get; set; }

        /// <summary>
        /// Gets the ContainerType, or null if its an untyped ContainerGenerator.
        /// </summary>
        Type ContainerType { get; }

        /// <summary>
        /// Signaled whenever new containers are materialized.
        /// </summary>
        event EventHandler<ItemContainerEventArgs> Materialized;

        /// <summary>
        /// Event raised whenever containers are dematerialized.
        /// </summary>
        event EventHandler<ItemContainerEventArgs> Dematerialized;

        /// <summary>
        /// Event raised whenever containers are recycled.
        /// </summary>
        event EventHandler<ItemContainerEventArgs> Recycled;

        /// <summary>
        /// Creates a container control for an item.
        /// </summary>
        /// <param name="index">
        /// The index of the item of data in the control's items.
        /// </param>
        /// <param name="item">The item.</param>
        /// <returns>The created controls.</returns>
        ItemContainerInfo Materialize(int index, object item);

        /// <summary>
        /// Removes a set of created containers.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item in the control's items.
        /// </param>
        /// <param name="count">The the number of items to remove.</param>
        /// <returns>The removed containers.</returns>
        IEnumerable<ItemContainerInfo> Dematerialize(int startingIndex, int count);

        /// <summary>
        /// Inserts space for newly inserted containers in the index.
        /// </summary>
        /// <param name="index">The index at which space should be inserted.</param>
        /// <param name="count">The number of blank spaces to create.</param>
        void InsertSpace(int index, int count);

        /// <summary>
        /// Removes a set of created containers and updates the index of later containers to fill
        /// the gap.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item in the control's items.
        /// </param>
        /// <param name="count">The the number of items to remove.</param>
        /// <returns>The removed containers.</returns>
        IEnumerable<ItemContainerInfo> RemoveRange(int startingIndex, int count);

        bool TryRecycle(int oldIndex, int newIndex, object item);

        /// <summary>
        /// Clears all created containers and returns the removed controls.
        /// </summary>
        /// <returns>The removed controls.</returns>
        IEnumerable<ItemContainerInfo> Clear();

        /// <summary>
        /// Gets the container control representing the item with the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The container, or null if no container created.</returns>
        IControl ContainerFromIndex(int index);

        /// <summary>
        /// Gets the index of the specified container control.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The index of the container, or -1 if not found.</returns>
        int IndexFromContainer(IControl container);
    }
}
