// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Rendering;

namespace Perspex.Controls.UnitTests
{
    internal class TestRoot : Decorator, ILayoutRoot, IRenderRoot
    {
        public Size ClientSize => new Size(100, 100);

        public ILayoutManager LayoutManager => new Mock<ILayoutManager>().Object;

        public IRenderer Renderer
        {
            get { throw new NotImplementedException(); }
        }

        public IRenderManager RenderManager
        {
            get { throw new NotImplementedException(); }
        }

        public Point TranslatePointToScreen(Point p)
        {
            return new Point();
        }
    }
}
