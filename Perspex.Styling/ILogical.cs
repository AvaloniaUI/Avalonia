// -----------------------------------------------------------------------
// <copyright file="ILogical.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System.Collections.Generic;

    public interface ILogical
    {
        ILogical LogicalParent { get; set; }

        IEnumerable<ILogical> LogicalChildren { get; }
    }
}
