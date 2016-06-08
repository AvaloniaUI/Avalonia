// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel that can be used to virtualize items.
    /// </summary>
    public interface IVirtualizingPanel : IPanel
    {
        /// <summary>
        /// Gets or sets the controller for the virtualizing panel.
        /// </summary>
        /// <remarks>
        /// A virtualizing controller is responsible for maintaing the controls in the virtualizing
        /// panel. This property will be set by the controller when virtualization is initialized.
        /// Note that this property may remain null if the panel is added to a control that does
        /// not act as a virtualizing controller.
        /// </remarks>
        IVirtualizingController Controller { get; set; }

        /// <summary>
        /// Gets a value indicating whether the panel is full.
        /// </summary>
        /// <remarks>
        /// This property should return false until enough children are added to fill the space
        /// passed into the last measure in the direction of scroll. It should be updated
        /// immediately after a child is added or removed.
        /// </remarks>
        bool IsFull { get; }

        /// <summary>
        /// Gets the number of items that can be removed while keeping the panel full.
        /// </summary>
        /// <remarks>
        /// This property should return the number of children that are completely out of the
        /// panel's current bounds in the direction of scroll. It should be updated after an
        /// arrange.
        /// </remarks>
        int OverflowCount { get; }

        /// <summary>
        /// Gets the direction of scroll.
        /// </summary>
        Orientation ScrollDirection { get; }

        /// <summary>
        /// Gets the average size of the materialized items in the direction of scroll.
        /// </summary>
        double AverageItemSize { get; }

        /// <summary>
        /// Gets or sets a size in pixels by which the content is overflowing the panel, in the
        /// direction of scroll.
        /// </summary>
        /// <remarks>
        /// This may be non-zero even when <see cref="OverflowCount"/> is zero if the last item
        /// overflows the panel bounds.
        /// </remarks>
        double PixelOverflow { get; }

        /// <summary>
        /// Gets or sets the current pixel offset of the items in the direction of scroll.
        /// </summary>
        double PixelOffset { get; set; }
    }
}
