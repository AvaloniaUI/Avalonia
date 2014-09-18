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

        public IRenderManager RenderManager
        {
            get { throw new NotImplementedException(); }
        }
    }
}
