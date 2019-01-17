// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the valid units for a <see cref="GridLength"/>.
    /// </summary>
    public enum GridUnitType
    {
        /// <summary>
        /// The row or column is auto-sized to fit its content.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The row or column is sized in device independent pixels.
        /// </summary>
        Pixel = 1,

        /// <summary>
        /// The row or column is sized as a weighted proportion of available space.
        /// </summary>
        Star = 2,
    }

    /// <summary>
    /// Holds the width or height of a <see cref="Grid"/>'s column and row definitions.
    /// </summary>
    public struct GridLength : IEquatable<GridLength>
    {
        private readonly GridUnitType _type;

        private readonly double _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLength"/> struct.
        /// </summary>
        /// <param name="value">The size of the GridLength in device independent pixels.</param>
        public GridLength(double value)
            : this(value, GridUnitType.Pixel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLength"/> struct.
        /// </summary>
        /// <param name="value">The size of the GridLength.</param>
        /// <param name="type">The unit of the GridLength.</param>
        public GridLength(double value, GridUnitType type)
        {
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentException("Invalid value", nameof(value));
            }

            if (type < GridUnitType.Auto || type > GridUnitType.Star)
            {
                throw new ArgumentException("Invalid value", nameof(type));
            }

            _type = type;
            _value = value;
        }

        /// <summary>
        /// Gets an instance of <see cref="GridLength"/> that indicates that a row or column should
        /// auto-size to fit its content.
        /// </summary>
        public static GridLength Auto => new GridLength(0, GridUnitType.Auto);

        /// <summary>
        /// Gets the unit of the <see cref="GridLength"/>.
        /// </summary>
        public GridUnitType GridUnitType => _type;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="GridLength"/> has a <see cref="GridUnitType"/> of Pixel.
        /// </summary>
        public bool IsAbsolute => _type == GridUnitType.Pixel;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="GridLength"/> has a <see cref="GridUnitType"/> of Auto.
        /// </summary>
        public bool IsAuto => _type == GridUnitType.Auto;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="GridLength"/> has a <see cref="GridUnitType"/> of Star.
        /// </summary>
        public bool IsStar => _type == GridUnitType.Star;

        /// <summary>
        /// Gets the length.
        /// </summary>
        public double Value => _value;

        /// <summary>
        /// Compares two GridLength structures for equality.
        /// </summary>
        /// <param name="a">The first GridLength.</param>
        /// <param name="b">The second GridLength.</param>
        /// <returns>True if the structures are equal, otherwise false.</returns>
        public static bool operator ==(GridLength a, GridLength b)
        {
            return (a.IsAuto && b.IsAuto) || (a._value == b._value && a._type == b._type);
        }

        /// <summary>
        /// Compares two GridLength structures for inequality.
        /// </summary>
        /// <param name="gl1">The first GridLength.</param>
        /// <param name="gl2">The first GridLength.</param>
        /// <returns>True if the structures are unequal, otherwise false.</returns>
        public static bool operator !=(GridLength gl1, GridLength gl2)
        {
            return !(gl1 == gl2);
        }

        /// <summary>
        /// Determines whether the <see cref="GridLength"/> is equal to the specified object.
        /// </summary>
        /// <param name="o">The object with which to test equality.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            if (!(o is GridLength))
            {
                return false;
            }

            return this == (GridLength)o;
        }

        /// <summary>
        /// Compares two GridLength structures for equality.
        /// </summary>
        /// <param name="gridLength">The structure with which to test equality.</param>
        /// <returns>True if the structures are equal, otherwise false.</returns>
        public bool Equals(GridLength gridLength)
        {
            return this == gridLength;
        }

        /// <summary>
        /// Gets a hash code for the GridLength.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _value.GetHashCode() ^ _type.GetHashCode();
        }

        /// <summary>
        /// Gets a string representation of the <see cref="GridLength"/>.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            if (IsAuto)
            {
                return "Auto";
            }

            string s = _value.ToString();
            return IsStar ? s + "*" : s;
        }

        /// <summary>
        /// Parses a string to return a <see cref="GridLength"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="GridLength"/>.</returns>
        public static GridLength Parse(string s)
        {
            s = s.ToUpperInvariant();

            if (s == "AUTO")
            {
                return Auto;
            }
            else if (s.EndsWith("*"))
            {
                var valueString = s.Substring(0, s.Length - 1).Trim();
                var value = valueString.Length > 0 ? double.Parse(valueString, CultureInfo.InvariantCulture) : 1;
                return new GridLength(value, GridUnitType.Star);
            }
            else
            {
                var value = double.Parse(s, CultureInfo.InvariantCulture);
                return new GridLength(value, GridUnitType.Pixel);
            }
        }

        /// <summary>
        /// Parses a string to return a collection of <see cref="GridLength"/>s.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="GridLength"/>.</returns>
        public static IEnumerable<GridLength> ParseLengths(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture))
            {
                while (tokenizer.TryReadString(out var item))
                {
                    yield return Parse(item);
                }
            }
        }
    }
}
