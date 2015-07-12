// -----------------------------------------------------------------------
// <copyright file="ColumnDefinitions.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Parsers;

    public class ColumnDefinitions : PerspexList<ColumnDefinition>
    {
        public ColumnDefinitions()
        {
        }

        public ColumnDefinitions(string s)
        {
            this.AddRange(GridLengthsParser.Parse(s).Select(x => new ColumnDefinition(x)));
        }
    }
}
