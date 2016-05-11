// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Interface for controls that host their own separate visual tree, such as popups.
    /// </summary>
    public interface IVisualTreeHost
    {
        /// <summary>
        /// Gets the root of the hosted visual tree.
        /// </summary>
        /// <value>
        /// The root of the hosted visual tree.
        /// </value>
        IVisual Root { get; }
    }
}
