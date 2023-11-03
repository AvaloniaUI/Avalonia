using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Avalonia.Media
{
    /// <summary>
    /// The <see cref="UnicodeRange"/> descripes a set of Unicode characters.
    /// </summary>
    public readonly record struct UnicodeRange
    {
        public readonly static UnicodeRange Default = Parse("0-10FFFD");

        private readonly UnicodeRangeSegment _single;
        private readonly IReadOnlyList<UnicodeRangeSegment>? _segments = null;

        public UnicodeRange(int start, int end)
        {
            _single = new UnicodeRangeSegment(start, end);
        }

        public UnicodeRange(UnicodeRangeSegment single)
        {
            _single = single;
        }

        public UnicodeRange(IReadOnlyList<UnicodeRangeSegment> segments)
        {
            if(segments is null || segments.Count == 0)
            {
                throw new ArgumentException(nameof(segments));
            }

            _single = segments[0];
            _segments = segments;
        }

        internal UnicodeRangeSegment Single => _single;

        internal IReadOnlyList<UnicodeRangeSegment>? Segments => _segments;

        /// <summary>
        /// Determines if given value is inside the range.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>
        /// <c>true</c> If given value is inside the range, <c>false</c> otherwise.
        /// </returns>
        public bool IsInRange(int value)
        {
            if(_segments is null)
            {
                return _single.IsInRange(value);
            }

            foreach(var segment in _segments)
            {
                if (segment.IsInRange(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses a <see cref="UnicodeRange"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed <see cref="UnicodeRange"/>.</returns>
        /// <exception cref="FormatException"></exception>
        public static UnicodeRange Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new FormatException("Could not parse specified Unicode range.");
            }

            var parts = s.Split(',');

            var length = parts.Length;

            if(length == 0)
            {
                throw new FormatException("Could not parse specified Unicode range.");
            }

            if(length == 1)
            {
                return new UnicodeRange(UnicodeRangeSegment.Parse(parts[0]));
            }

            var segments = new UnicodeRangeSegment[length];

            for (int i = 0; i < length; i++)
            {
                segments[i] = UnicodeRangeSegment.Parse(parts[i].Trim());
            }

            return new UnicodeRange(segments);
        }
    }

    public readonly record struct UnicodeRangeSegment
    {
        private static readonly Regex s_regex = new Regex(@"^(?:[uU]\+)?(?:([0-9a-fA-F](?:[0-9a-fA-F?]{1,5})?))$", RegexOptions.Compiled);

        public UnicodeRangeSegment(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Get the start of the segment.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Get the end of the segment.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Determines if given value is inside the range segment.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>
        /// <c>true</c> If given value is inside the range segment, <c>false</c> otherwise.
        /// </returns>
        public bool IsInRange(int value)
        {
            return value - Start <= End - Start;
        }

        /// <summary>
        /// Parses a <see cref="UnicodeRangeSegment"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed <see cref="UnicodeRangeSegment"/>.</returns>
        /// <exception cref="FormatException"></exception>
        public static UnicodeRangeSegment Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new FormatException("Could not parse specified Unicode range segment.");
            }

            var parts = s.Split('-');

            int start, end;

            switch (parts.Length)
            {
                case 1:
                    {
                        //e.g. U+20, U+3F U+30??
                        var single = s_regex.Match(parts[0]);

                        if (!single.Success)
                        {
                            throw new FormatException("Could not parse specified Unicode range segment.");
                        }

                        if (!single.Value.Contains('?'))
                        {
                            start = int.Parse(single.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                            end = start;
                        }
                        else
                        {
                            start = int.Parse(single.Groups[1].Value.Replace('?', '0'), System.Globalization.NumberStyles.HexNumber);
                            end = int.Parse(single.Groups[1].Value.Replace('?', 'F'), System.Globalization.NumberStyles.HexNumber);
                        }
                        break;
                    }
                case 2:
                    {
                        var first = s_regex.Match(parts[0]);
                        var second = s_regex.Match(parts[1]);

                        if (!first.Success || !second.Success)
                        {
                            throw new FormatException("Could not parse specified Unicode range segment.");
                        }

                        start = int.Parse(first.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                        end = int.Parse(second.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                        break;
                    }
                default:
                    throw new FormatException("Could not parse specified Unicode range segment.");
            }

            return new UnicodeRangeSegment(start, end);
        }
    }
}
