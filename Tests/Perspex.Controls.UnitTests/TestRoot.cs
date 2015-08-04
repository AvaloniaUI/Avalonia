// -----------------------------------------------------------------------
// <copyright file="TestRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using Moq;
    using Perspex.Layout;
    using Perspex.Platform;
    using Perspex.Rendering;

    internal class TestRoot : Decorator, ILayoutRoot, IRenderRoot
    {
        public Size ClientSize
        {
            get { return new Size(100, 100); }
        }

        public ILayoutManager LayoutManager
        {
            get { return new Mock<ILayoutManager>().Object; }
        }

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
