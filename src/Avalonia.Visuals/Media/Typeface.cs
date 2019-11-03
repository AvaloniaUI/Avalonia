// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface.
    /// </summary>
    [DebuggerDisplay("Name = {FontFamily.Name}, Weight = {Weight}, Style = {Style}")]
    public class Typeface : IEquatable<Typeface>
    {
        public static readonly Typeface Default = new Typeface(FontFamily.Default);

        private GlyphTypeface _glyphTypeface;

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="style">The font style.</param>
        public Typeface([NotNull]FontFamily fontFamily,
            FontWeight weight = FontWeight.Normal,
            FontStyle style = FontStyle.Normal)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Font weight must be > 0.");
            }

            FontFamily = fontFamily;
            Style = style;
            Weight = weight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface(string fontFamilyName,
            FontWeight weight = FontWeight.Normal,
            FontStyle style = FontStyle.Normal)
            : this(new FontFamily(fontFamilyName), weight, style)
        {
        }

        /// <summary>
        /// Gets the font family.
        /// </summary>
        public FontFamily FontFamily { get; }

        /// <summary>
        /// Gets the font style.
        /// </summary>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the font weight.
        /// </summary>
        public FontWeight Weight { get; }

        /// <summary>
        /// Gets the glyph typeface.
        /// </summary>
        /// <value>
        /// The glyph typeface.
        /// </value>
        public GlyphTypeface GlyphTypeface => _glyphTypeface ?? (_glyphTypeface = new GlyphTypeface(this));

        public static bool operator !=(Typeface a, Typeface b)
        {
            return !(a == b);
        }

        public static bool operator ==(Typeface a, Typeface b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return !(a is null) && a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            if (obj is Typeface typeface)
            {
                return Equals(typeface);
            }

            return false;
        }

        public bool Equals(Typeface other)
        {
            if (other is null)
            {
                return false;
            }

            return FontFamily.Equals(other.FontFamily) && Style == other.Style && Weight == other.Weight;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FontFamily != null ? FontFamily.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Style;
                hashCode = (hashCode * 397) ^ (int)Weight;
                return hashCode;
            }
        }
    }
}
