// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Interface implemented by scrollable controls.
    /// </summary>
    public interface IScrollable
    {
        /// <summary>
        /// Gets the extent of the scrollable content, in logical units
        /// </summary>
        Size Extent { get; }

        /// <summary>
        /// Gets or sets the current scroll offset, in logical units.
        /// </summary>
        Vector Offset { get; set; }

        /// <summary>
        /// Gets the size of the viewport, in logical units.
        /// </summary>
        Size Viewport { get; }
    }
}
