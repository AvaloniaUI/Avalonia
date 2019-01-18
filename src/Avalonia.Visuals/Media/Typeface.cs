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
        /// <param name="fontSize">The font size, in DIPs.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface(
            FontFamily fontFamily, 
            double fontSize = 12, 
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

            FontFamily = fontFamily;
            FontSize = fontSize;
            Style = style;
            Weight = weight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="fontSize">The font size, in DIPs.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        public Typeface(
            string fontFamilyName,
            double fontSize = 12,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal)
            : this(new FontFamily(fontFamilyName), fontSize, style, weight)
        {
        }

        /// <summary>
        /// Gets the font family.
        /// </summary>
        public FontFamily FontFamily { get; }

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
