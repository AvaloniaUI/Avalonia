// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : DrawOperation
    {
        public BrushDrawOperation(Rect bounds, Matrix transform, IPen pen)
            : base(bounds, transform, pen)
        {
        }

        /// <summary>
        /// Gets a collection of child scenes that are needed to draw visual brushes.
        /// </summary>
        public abstract IDictionary<IVisual, Scene> ChildScenes { get; }
    }
}
