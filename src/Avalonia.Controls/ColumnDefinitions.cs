// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// A collection of <see cref="ColumnDefinition"/>s.
    /// </summary>
    public class ColumnDefinitions : DefinitionList<ColumnDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinitions"/> class.
        /// </summary>
        public ColumnDefinitions() : base ()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinitions"/> class.
        /// </summary>
        /// <param name="s">A string representation of the column definitions.</param>
        public ColumnDefinitions(string s)
            : this()
        {
            AddRange(GridLength.ParseLengths(s).Select(x => new ColumnDefinition(x)));
        }

        /// <summary>
        /// Parses a string representation of column definitions collection.
        /// </summary>
        /// <param name="s">The column definitions string.</param>
        /// <returns>The <see cref="ColumnDefinitions"/>.</returns>
        public static ColumnDefinitions Parse(string s) => new ColumnDefinitions(s);
    }
}