// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Provides details for the <see cref="IItemContainerGenerator.Materialized"/>
    /// and <see cref="IItemContainerGenerator.Dematerialized"/> events.
    /// </summary>
    public class ItemContainerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerEventArgs"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ItemContainerEventArgs(ItemContainerInfo container)
        {
            StartingIndex = container.Index;
            Containers = new[] { container };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerEventArgs"/> class.
        /// </summary>
        /// <param name="startingIndex">The index of the first container in the source items.</param>
        /// <param name="containers">The containers.</param>
        /// <remarks>
        /// TODO: Do we really need to pass in StartingIndex here? The ItemContainerInfo objects
        /// have an index, and what happens if the contains passed in aren't sequential?
        /// </remarks>
        public ItemContainerEventArgs(
            int startingIndex, 
            IList<ItemContainerInfo> containers)
        {
            StartingIndex = startingIndex;
            Containers = containers;
        }

        /// <summary>
        /// Gets the containers.
        /// </summary>
        public IList<ItemContainerInfo> Containers { get; }

        /// <summary>
        /// Gets the index of the first container in the source items.
        /// </summary>
        public int StartingIndex { get; }
    }
}
