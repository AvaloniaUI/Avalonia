// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines the valid modes for a <see cref="RepeatCount"/>.
    /// </summary>
    public enum RepeatType
    {
        None,
        Repeat,
        Loop
    }

    /// <summary>
    /// Determines the number of iterations of an animation.
    /// Also defines its repeat behavior. 
    /// </summary>
    [TypeConverter(typeof(RepeatCountTypeConverter))]
    public struct RepeatCount : IEquatable<RepeatCount>
    {
        private readonly RepeatType _type;
        private readonly ulong _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepeatCount"/> struct.
        /// </summary>
        /// <param name="value">The number of iterations of an animation.</param>
        public RepeatCount(ulong value)
            : this(value, RepeatType.Repeat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepeatCount"/> struct.
        /// </summary>
        /// <param name="value">The size of the RepeatCount.</param>
        /// <param name="type">The unit of the RepeatCount.</param>
        public RepeatCount(ulong value, RepeatType type)
        {
            if (type < RepeatType.None || type > RepeatType.Loop)
            {
                throw new ArgumentException("Invalid value", "type");
            }

            _type = type;
            _value = value;
        }

        /// <summary>
        /// Gets an instance of <see cref="RepeatCount"/> that indicates that an animation
        /// should repeat forever.
        /// </summary>
        public static RepeatCount Loop => new RepeatCount(0, RepeatType.Loop);

        /// <summary>
        /// Gets an instance of <see cref="RepeatCount"/> that indicates that an animation
        /// should not repeat.
        /// </summary>
        public static RepeatCount None => new RepeatCount(0, RepeatType.None);

        /// <summary>
        /// Gets the unit of the <see cref="RepeatCount"/>.
        /// </summary>
        public RepeatType RepeatType => _type;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="RepeatCount"/> is set to loop.
        /// </summary>
        public bool IsLoop => _type == RepeatType.Loop;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="RepeatCount"/> is set to not repeat.
        /// </summary>
        public bool IsNone => _type == RepeatType.None;

        /// <summary>
        /// Gets the number of repeat iterations.
        /// </summary>
        public ulong Value => _value;

        /// <summary>
        /// Compares two RepeatCount structures for equality.
        /// </summary>
        /// <param name="a">The first RepeatCount.</param>
        /// <param name="b">The second RepeatCount.</param>
        /// <returns>True if the structures are equal, otherwise false.</returns>
        public static bool operator ==(RepeatCount a, RepeatCount b)
        {
            return (a.IsNone && b.IsNone) && (a.IsLoop && b.IsLoop)
                || (a._value == b._value && a._type == b._type);
        }

        /// <summary>
        /// Compares two RepeatCount structures for inequality.
        /// </summary>
        /// <param name="rc1">The first RepeatCount.</param>
        /// <param name="rc2">The first RepeatCount.</param>
        /// <returns>True if the structures are unequal, otherwise false.</returns>
        public static bool operator !=(RepeatCount rc1, RepeatCount rc2)
        {
            return !(rc1 == rc2);
        }

        /// <summary>
        /// Determines whether the <see cref="RepeatCount"/> is equal to the specified object.
        /// </summary>
        /// <param name="o">The object with which to test equality.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            if (!(o is RepeatCount))
            {
                return false;
            }

            return this == (RepeatCount)o;
        }

        /// <summary>
        /// Compares two RepeatCount structures for equality.
        /// </summary>
        /// <param name="RepeatCount">The structure with which to test equality.</param>
        /// <returns>True if the structures are equal, otherwise false.</returns>
        public bool Equals(RepeatCount RepeatCount)
        {
            return this == RepeatCount;
        }

        /// <summary>
        /// Gets a hash code for the RepeatCount.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _value.GetHashCode() ^ _type.GetHashCode();
        }

        /// <summary>
        /// Gets a string representation of the <see cref="RepeatCount"/>.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            if (IsLoop)
            {
                return "Auto";
            }
            else if (IsNone)
            {
                return "None";
            }

            string s = _value.ToString();
            return s;
        }

        /// <summary>
        /// Parses a string to return a <see cref="RepeatCount"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="RepeatCount"/>.</returns>
        public static RepeatCount Parse(string s)
        {
            s = s.ToUpperInvariant().Trim();

            if (s == "NONE")
            {
                return None;
            }
            else if (s.EndsWith("LOOP"))
            {
                return Loop;
            }
            else
            {
                if(s.StartsWith("-"))
                    throw new InvalidCastException("RepeatCount can't be a negative number.");

                var value = ulong.Parse(s, CultureInfo.InvariantCulture);
             
                if (value == 1)
                    return None;

                return new RepeatCount(value);
            }
        }
    }
}
