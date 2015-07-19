// -----------------------------------------------------------------------
// <copyright file="GridLengthsParser.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Parsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// Parses a string of <see cref="GridLength"/>s for <see cref="ColumnDefinitions"/> and
    /// <see cref="RowDefinitions"/>.
    /// </summary>
    public static class GridLengthsParser
    {
        /// <summary>
        /// Parses a string of <see cref="GridLength"/>s.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A collection of <see cref="GridLength"/>s.</returns>
        public static IEnumerable<GridLength> Parse(string s)
        {
            var parts = s.Split(',').Select(x => x.ToUpperInvariant().Trim());

            foreach (var part in parts)
            {
                if (part == "AUTO")
                {
                    yield return GridLength.Auto;
                }
                else if (part.EndsWith("*"))
                {
                    var valueString = part.Substring(0, part.Length - 1).Trim();
                    var value = valueString.Length > 0 ? double.Parse(valueString) : 1;
                    yield return new GridLength(value, GridUnitType.Star);
                }
                else if (part.EndsWith("PX"))
                {
                    var value = double.Parse(part.Substring(0, part.Length - 2));
                    yield return new GridLength(value, GridUnitType.Pixel);
                }
                else
                {
                    throw new FormatException("Invalid grid length: " + part);
                }
            }
        }
    }
}
