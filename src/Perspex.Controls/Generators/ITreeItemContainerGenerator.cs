// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Perspex.Controls.Generators
{
    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    public interface ITreeItemContainerGenerator : IItemContainerGenerator
    {
        /// <summary>
        /// Gets the item container for the root of the tree, or null if this generator is itself 
        /// the root of the tree.
        /// </summary>
        ITreeItemContainerGenerator RootGenerator { get; }
    }
}