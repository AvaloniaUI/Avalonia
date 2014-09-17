// -----------------------------------------------------------------------
// <copyright file="TestRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using Moq;
    using Perspex.Layout;

    internal class TestRoot : Decorator, ILayoutRoot
    {
        public Size ClientSize
        {
            get { return new Size(100, 100); }
        }

        public ILayoutManager LayoutManager
        {
            get { return new Mock<ILayoutManager>().Object; }
        }
    }
}
