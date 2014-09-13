// -----------------------------------------------------------------------
// <copyright file="Controls.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Perspex.Controls
{
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
