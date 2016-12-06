// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.VisualTree;
using System.Collections.Generic;

namespace Avalonia.Rendering
{
    public interface IRenderer : IDisposable
    {
        bool DrawFps { get; set; }
        bool DrawDirtyRects { get; set; }

        void AddDirty(IVisual visual);
        IEnumerable<IVisual> HitTest(Point p, Func<IVisual, bool> filter);
        void Render(Rect rect);
    }
}