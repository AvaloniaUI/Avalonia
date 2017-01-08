// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Visuals.UnitTests
{
    public class TestRoot : TestVisual, IRenderRoot
    {
        public Size ClientSize { get; }
        
        public IRenderTarget CreateRenderTarget()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public IRenderer Renderer
        {
            get { throw new NotImplementedException(); }
        }

        public Point PointToClient(Point p)
        {
            throw new NotImplementedException();
        }

        public Point PointToScreen(Point p)
        {
            throw new NotImplementedException();
        }
    }
}
