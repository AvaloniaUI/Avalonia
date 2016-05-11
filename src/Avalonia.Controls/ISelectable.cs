// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for objects that are selectable.
    /// </summary>
    /// <remarks>
    /// Controls such as <see cref="SelectingItemsControl"/> use this interface to indicate the
    /// selected control in a list. If changing the control's <see cref="IsSelected"/> property
    /// should update the selection in a <see cref="SelectingItemsControl"/> or equivalent, then
    /// the control should raise the <see cref="SelectingItemsControl.IsSelectedChangedEvent"/>.
    /// </remarks>
    public interface ISelectable
    {
        /// <summary>
        /// Gets or sets the selected state of the object.
        /// </summary>
        bool IsSelected { get; set; }
    }
}