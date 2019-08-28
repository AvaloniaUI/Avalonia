// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the placement for a <see cref="Popup"/> control.
    /// </summary>
    public enum PlacementMode
    {
        /// <summary>
        /// The popup is placed at the pointer position.
        /// </summary>
        Pointer,

        /// <summary>
        /// The popup is placed at the bottom left of its target.
        /// </summary>
        Bottom,

        /// <summary>
        /// The popup is placed at the top right of its target.
        /// </summary>
        Right,
        
        /// <summary>
        /// The popup is placed at the top left of its target.
        /// </summary>
        Left,
        
        /// <summary>
        /// The popup is placed at the top left of its target.
        /// </summary>
        Top,
        
        /// <summary>
        /// The popup is placed according to anchor and gravity rules
        /// </summary>
        AnchorAndGravity
    }
}
