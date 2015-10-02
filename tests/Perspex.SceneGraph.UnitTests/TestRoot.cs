// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;
using Perspex.Rendering;

namespace Perspex.SceneGraph.UnitTests
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

        public Point TranslatePointToScreen(Point p)
        {
            throw new NotImplementedException();
        }
    }
}
