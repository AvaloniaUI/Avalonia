// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface.
    /// </summary>
    public class Typeface
    {
        public static readonly Typeface Default = new Typeface(FontFamily.Default);

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface(FontFamily fontFamily,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal)
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
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal)
            : this(new FontFamily(fontFamilyName), style, weight)
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
    }
}
