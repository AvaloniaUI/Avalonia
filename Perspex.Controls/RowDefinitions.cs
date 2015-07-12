// -----------------------------------------------------------------------
// <copyright file="RowDefinitions.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Parsers;

    public class RowDefinitions : PerspexList<RowDefinition>
    {
        public RowDefinitions()
        {
        }

        public RowDefinitions(string s)
        {
            this.AddRange(GridLengthsParser.Parse(s).Select(x => new RowDefinition(x)));
        }
    }
}
