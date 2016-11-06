// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public interface IVisualNode : ISceneNode
    {
        IVisual Visual { get; }
        Rect ClipBounds { get; set; }
        bool ClipToBounds { get; set; }
        IReadOnlyList<ISceneNode> Children { get; }
    }
}
