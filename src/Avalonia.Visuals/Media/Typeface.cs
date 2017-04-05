using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface.
    /// </summary>
    public class Typeface
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="fontSize">The font size, in DIPs.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface(
            string fontFamilyName,
            double fontSize,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal)
        {
            if (fontSize <= 0)
            {
                throw new ArgumentException("Font size must be > 0.");
            }

            if (weight <= 0)
            {
                throw new ArgumentException("Font weight must be > 0.");
            }

            FontFamilyName = fontFamilyName;
            FontSize = fontSize;
            Style = style;
            Weight = weight;
        }

        /// <summary>
        /// Gets the name of the font family.
        /// </summary>
        public string FontFamilyName { get; }

        /// <summary>
        /// Gets the size of the font in DIPs.
        /// </summary>
        public double FontSize { get; }

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
