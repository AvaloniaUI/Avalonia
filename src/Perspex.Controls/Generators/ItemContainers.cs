// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Perspex.Controls.Generators
{
    /// <summary>
    /// Holds details about a set of item containers in an <see cref="IItemContainerGenerator"/>.
    /// </summary>
    public class ItemContainers
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainers"/> class.
        /// </summary>
        /// <param name="startingIndex">The index of the first container in the source items.</param>
        /// <param name="containers">The containers.</param>
        public ItemContainers(int startingIndex, IList<IControl> containers)
        {
            StartingIndex = startingIndex;
            Items = containers;
        }

        /// <summary>
        /// Gets the index of the first container in the source items.
        /// </summary>
        public int StartingIndex { get; }

        /// <summary>
        /// Gets the containers. May contain null entries.
        /// </summary>
        public IList<IControl> Items { get; }
    }
}
