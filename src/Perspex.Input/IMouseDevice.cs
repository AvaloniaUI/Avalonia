// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Input
{
    /// <summary>
    /// Represents a mouse device.
    /// </summary>
    public interface IMouseDevice : IPointerDevice
    {
        /// <summary>
        /// Gets the mouse position, in screen coordinates.
        /// </summary>
        Point Position { get; }
    }
}
