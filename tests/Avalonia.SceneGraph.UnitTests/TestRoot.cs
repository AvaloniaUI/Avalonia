// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.SceneGraph.UnitTests
{
    public class TestRoot : TestVisual, IRenderRoot
    {
        public IRenderTarget RenderTarget
        {
            get { throw new NotImplementedException(); }
        }

        public IRenderQueueManager RenderQueueManager
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
