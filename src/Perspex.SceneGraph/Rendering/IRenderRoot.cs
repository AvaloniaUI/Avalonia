// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;

namespace Perspex.Rendering
{
    /// <summary>
    /// Represents the root of a renderable tree.
    /// </summary>
    public interface IRenderRoot
    {
        /// <summary>
        /// Gets the render manager which schedules renders.
        /// </summary>
        IRenderQueueManager RenderQueueManager { get; }

        /// <summary>
        /// Converts a point from screen to client coordinates.
        /// </summary>
        /// <param name="point">The point in screen coordinates.</param>
        /// <returns>The point in client coordinates.</returns>
        Point PointToClient(Point point);

        /// <summary>
        /// Converts a point from client to screen coordinates.
        /// </summary>
        /// <param name="point">The point in client coordinates.</param>
        /// <returns>The point in screen coordinates.</returns>
        Point PointToScreen(Point point);
    }
}
