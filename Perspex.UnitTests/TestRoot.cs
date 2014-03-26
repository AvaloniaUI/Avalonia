// -----------------------------------------------------------------------
// <copyright file="TestRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using Moq;
    using Perspex.Controls;
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
