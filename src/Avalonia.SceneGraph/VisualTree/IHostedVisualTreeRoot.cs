// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Interface for controls that are at the root of a hosted visual tree, such as popups.
    /// </summary>
    public interface IHostedVisualTreeRoot
    {
        /// <summary>
        /// Gets the visual tree host.
        /// </summary>
        /// <value>
        /// The visual tree host.
        /// </value>
        IVisual Host { get; }
    }
}
