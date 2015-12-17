// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// Interface implemented by controls that handle their own scrolling when placed inside a 
    /// <see cref="ScrollViewer"/>.
    /// </summary>
    public interface IScrollable
    {
        /// <summary>
        /// Gets or sets the invalidation method which notifies the attached 
        /// <see cref="ScrollViewer"/> of a change in <see cref="Extent"/> or <see cref="Offset"/>.
        /// </summary>
        /// <remarks>
        /// This property is set by the parent <see cref="ScrollViewer"/> when the 
        /// <see cref="IScrollable"/> is placed inside it.
        /// </remarks>
        Action InvalidateScroll { get; set; }

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
