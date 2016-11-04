// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace Avalonia.Rendering.SceneGraph
{
    public interface ISceneNode
    {
        void Render(IDrawingContextImpl context);
    }
}
