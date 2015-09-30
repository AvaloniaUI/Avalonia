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
        /// Translates a point to screen co-ordinates.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>The point in screen co-ordinates.</returns>
        Point TranslatePointToScreen(Point p);
    }
}
