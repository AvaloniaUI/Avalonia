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
        /// Signalled whenever new containers are initialized.
        /// </summary>
        IObservable<ItemContainers> ContainersInitialized { get; }

        /// <summary>
        /// Creates container controls for a collection of items.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="items">The items.</param>
        /// <param name="itemTemplate">An optional item template.</param>
        /// <returns>The created controls.</returns>
        IList<IControl> CreateContainers(
            int startingIndex,
            IEnumerable items,
            IDataTemplate itemTemplate);

        /// <summary>
        /// Removes a set of created containers from the index and returns the removed controls.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="items">The items.</param>
        /// <returns>The removed controls.</returns>
        IList<IControl> RemoveContainers(int startingIndex, IEnumerable items);

        /// <summary>
        /// Clears the created containers from the index and returns the removed controls.
        /// </summary>
        /// <returns>The removed controls.</returns>
        IList<IControl> ClearContainers();

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