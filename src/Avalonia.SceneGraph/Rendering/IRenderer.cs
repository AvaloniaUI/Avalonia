// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public interface IRenderer : IDisposable
    {
        void AddDirty(IVisual visual);

        void Render(Rect rect);
    }
}