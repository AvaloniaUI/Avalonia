using System;
using System.Diagnostics;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface.
    /// </summary>
    [DebuggerDisplay("Name = {FontFamily.Name}, Weight = {Weight}, Style = {Style}")]
    public readonly struct Typeface : IEquatable<Typeface>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        public Typeface(FontFamily fontFamily,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal,
            FontStretch stretch = FontStretch.Normal)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Font weight must be > 0.");
            }
            
            if ((int)stretch < 1)
            {
                throw new ArgumentException("Font stretch must be > 1.");
            }

            FontFamily = fontFamily;
            Style = style;
            Weight = weight;
            Stretch = stretch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Typeface"/> class.
        /// </summary>
        /// <param name="fontFamilyName">The name of the font family.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        public Typeface(string fontFamilyName,
            FontStyle style = FontStyle.Normal,
            FontWeight weight = FontWeight.Normal,
            FontStretch stretch = FontStretch.Normal)
            : this(new FontFamily(fontFamilyName), style, weight, stretch)
        {
        }

        public static Typeface Default { get; } = new Typeface(FontFamily.Default);

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
        /// Gets the font stretch.
        /// </summary>
        public FontStretch Stretch { get; }

        /// <summary>
        /// Gets the glyph typeface.
        /// </summary>
        /// <value>
        /// The glyph typeface.
        /// </value>
        public IGlyphTypeface GlyphTypeface
        {
            get
            {
                if(FontManager.Current.TryGetGlyphTypeface(this, out var glyphTypeface))
                {
                    return glyphTypeface;
                }

                throw new InvalidOperationException("Could not create glyphTypeface.");
            }
        }

        public static bool operator !=(Typeface a, Typeface b)
        {
            return !(a == b);
        }

        public static bool operator ==(Typeface a, Typeface b)
        {
            return  a.Equals(b);
        }

        public override bool Equals(object? obj)
        {
            return obj is Typeface typeface && Equals(typeface);
        }

        public bool Equals(Typeface other)
        {
            return FontFamily == other.FontFamily && Style == other.Style && 
                   Weight == other.Weight && Stretch == other.Stretch;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FontFamily != null ? FontFamily.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Style;
                hashCode = (hashCode * 397) ^ (int)Weight;
                hashCode = (hashCode * 397) ^ (int)Stretch;
                return hashCode;
            }
        }
    }
}
