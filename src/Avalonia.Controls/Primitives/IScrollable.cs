// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Interface implemented by controls that handle their own scrolling when placed inside a 
    /// <see cref="ScrollViewer"/>.
    /// </summary>
    /// <remarks>
    /// Controls that implement this interface, when placed inside a <see cref="ScrollViewer"/>
    /// can override the physical scrolling behavior of the scroll viewer with logical scrolling.
    /// Physical scrolling means that the scroll viewer is a simple viewport onto a larger canvas
    /// whereas logical scrolling means that the scrolling is handled by the child control itself
    /// and it can choose to do handle the scroll information as it sees fit.
    /// </remarks>
    public interface IScrollable
    {
        /// <summary>
        /// Gets a value indicating whether logical scrolling is enabled on the control.
        /// </summary>
        bool IsLogicalScrollEnabled { get; }

        /// <summary>
        /// Gets or sets the scroll invalidation method.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method notifies the attached <see cref="ScrollViewer"/> of a change in 
        /// the <see cref="Extent"/>, <see cref="Offset"/> or <see cref="Viewport"/> properties.
        /// </para>
        /// <para>
        /// This property is set by the parent <see cref="ScrollViewer"/> when the 
        /// <see cref="IScrollable"/> is placed inside it.
        /// </para>
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

        /// <summary>
        /// Gets the size to scroll by, in logical units.
        /// </summary>
        Size ScrollSize { get; }

        /// <summary>
        /// Gets the size to page by, in logical units.
        /// </summary>
        Size PageScrollSize { get; }
    }
}
