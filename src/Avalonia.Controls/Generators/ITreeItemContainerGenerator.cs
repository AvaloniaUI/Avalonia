// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    public interface ITreeItemContainerGenerator : IItemContainerGenerator
    {
        /// <summary>
        /// Gets the container index for the tree.
        /// </summary>
        TreeContainerIndex Index { get; }
    }
}