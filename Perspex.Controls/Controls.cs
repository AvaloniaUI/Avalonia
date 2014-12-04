// -----------------------------------------------------------------------
// <copyright file="Controls.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Collections.Generic;

    public class Controls : PerspexList<Control>
    {
        public Controls()
        {
        }

        public Controls(IEnumerable<Control> items)
            : base(items)
        {
        }
    }
}
