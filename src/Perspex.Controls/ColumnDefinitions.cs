﻿// -----------------------------------------------------------------------
// <copyright file="ColumnDefinitions.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Parsers;

    /// <summary>
    /// A collection of <see cref="ColumnDefinition"/>s.
    /// </summary>
    public class ColumnDefinitions : PerspexList<ColumnDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinitions"/> class.
        /// </summary>
        public ColumnDefinitions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinitions"/> class.
        /// </summary>
        /// <param name="s">A string representation of the column definitions.</param>
        public ColumnDefinitions(string s)
        {
            this.AddRange(GridLengthsParser.Parse(s).Select(x => new ColumnDefinition(x)));
        }
    }
}