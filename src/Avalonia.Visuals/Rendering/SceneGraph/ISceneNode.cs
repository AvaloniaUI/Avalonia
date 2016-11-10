// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Represents a node in the low-level scene graph.
    /// </summary>
    public interface ISceneNode
    {
        /// <summary>
        /// Renders the node to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        void Render(IDrawingContextImpl context);
    }
}
