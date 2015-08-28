// -----------------------------------------------------------------------
// <copyright file="RowDefinitions.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Parsers;

    /// <summary>
    /// A collection of <see cref="RowDefinition"/>s.
    /// </summary>
    public class RowDefinitions : PerspexList<RowDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
        /// </summary>
        public RowDefinitions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
        /// </summary>
        /// <param name="s">A string representation of the row definitions.</param>
        public RowDefinitions(string s)
        {
            this.AddRange(GridLengthsParser.Parse(s).Select(x => new RowDefinition(x)));
        }
    }
}