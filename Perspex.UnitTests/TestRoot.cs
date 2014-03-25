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
    using Perspex.Controls;
    using Perspex.Layout;

    internal class TestRoot : Decorator, ILayoutRoot
    {
        public Size ClientSize
        {
            get { throw new NotImplementedException(); }
        }

        public ILayoutManager LayoutManager
        {
            get { throw new NotImplementedException(); }
        }
    }
}
