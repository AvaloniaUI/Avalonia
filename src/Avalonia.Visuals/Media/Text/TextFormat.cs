// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Avalonia.Media.Text
{
    [DebuggerDisplay("Typeface = {Typeface.FontFamily.Name}, FontSize = {FontSize}")]
    public readonly struct TextFormat : IEquatable<TextFormat>
    {
        public TextFormat(Typeface typeface, double fontSize)
        {
            Typeface = typeface;
            FontSize = fontSize;
            FontMetrics = new FontMetrics(typeface, fontSize);
        }

        /// <summary>
        ///     Gets the typeface.
        /// </summary>
        /// <value>
        ///     The typeface.
        /// </value>
        public Typeface Typeface { get; }

        /// <summary>
        ///     Gets the font size.
        /// </summary>
        /// <value>
        ///     The size of the font.
        /// </value>
        public double FontSize { get; }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        /// <value>
        ///     The metrics of the font.
        /// </value> 
        public FontMetrics FontMetrics { get; }

        public static bool operator ==(TextFormat self, TextFormat other)
        {
            return self.Equals(other);
        }

        public static bool operator !=(TextFormat self, TextFormat other)
        {
            return !(self == other);
        }

        public bool Equals(TextFormat other)
        {
            return Typeface.Equals(other.Typeface) && FontSize.Equals(other.FontSize);
        }

        public override bool Equals(object obj)
        {
            return obj is TextFormat other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Typeface != null ? Typeface.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ FontSize.GetHashCode();
                return hashCode;
            }
        }
    }
}
