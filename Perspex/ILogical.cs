// -----------------------------------------------------------------------
// <copyright file="ILogical.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface ILogical
    {
        ILogical LogicalParent { get; set; }

        IEnumerable<ILogical> LogicalChildren { get; }
    }
}
