// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Perspex.Controls.Templates;

namespace Perspex.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public interface IItemContainerGenerator
    {
        /// <summary>
        /// Gets the currently realized containers.
        /// </summary>
        IEnumerable<ItemContainer> Containers { get; }

        /// <summary>
        /// Signalled whenever new containers are materialized.
        /// </summary>
        event EventHandler<ItemContainerEventArgs> Materialized;

        /// <summary>
        /// Event raised whenever containers are dematerialized.
        /// </summary>
        event EventHandler<ItemContainerEventArgs> Dematerialized;

        /// <summary>
        /// Creates container controls for a collection of items.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="items">The items.</param>
        /// <param name="selector">An optional member selector.</param>
        /// <returns>The created controls.</returns>
        IEnumerable<ItemContainer> Materialize(
            int startingIndex,
            IEnumerable items,
            IMemberSelector selector);

        /// <summary>
        /// Removes a set of created containers.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="count">The the number of items to remove.</param>
        /// <returns>The removed containers.</returns>
        IEnumerable<ItemContainer> Dematerialize(int startingIndex, int count);

        /// <summary>
        /// Removes a set of created containers and updates the index of later containers to fill
        /// the gap.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="count">The the number of items to remove.</param>
        /// <returns>The removed containers.</returns>
        IEnumerable<ItemContainer> RemoveRange(int startingIndex, int count);

        /// <summary>
        /// Clears all created containers and returns the removed controls.
        /// </summary>
        /// <returns>The removed controls.</returns>
        IEnumerable<ItemContainer> Clear();

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