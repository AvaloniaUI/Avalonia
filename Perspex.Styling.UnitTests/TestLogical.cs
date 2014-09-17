// -----------------------------------------------------------------------
// <copyright file="SubscribeCheck.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System.Collections.Generic;
    using Perspex.Styling;

    public abstract class TestLogical : TestControlBase, ILogical
    {
        public TestLogical()
        {
        }

        public abstract ILogical LogicalParent
        {
            get;
            set;
        }

        public abstract IEnumerable<ILogical> LogicalChildren
        {
            get;
        }
    }
}
