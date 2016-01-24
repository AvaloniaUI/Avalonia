// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Perspex.Controls.Generators
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
        /// <param name="startingIndex">The index of the first container in the source items.</param>
        /// <param name="containers">The containers.</param>
        public ItemContainerEventArgs(
            int startingIndex, 
            IList<ItemContainer> containers)
        {
            StartingIndex = startingIndex;
            Containers = containers;
        }

        /// <summary>
        /// Gets the containers.
        /// </summary>
        public IList<ItemContainer> Containers { get; }

        /// <summary>
        /// Gets the index of the first container in the source items.
        /// </summary>
        public int StartingIndex { get; }
    }
}
