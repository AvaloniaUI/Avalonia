using System;
using System.Diagnostics;
using System.Text;
using Avalonia.Utilities;

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

            FontFamily = fontFamily ?? FontFamily.Default;
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
            : this(string.IsNullOrEmpty(fontFamilyName) ? FontFamily.Default : new FontFamily(fontFamilyName),
                  style, weight, stretch)
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
        public GlyphTypeface GlyphTypeface
        {
            get
            {
                if(FontManager.Current.TryGetGlyphTypeface(this, out var glyphTypeface))
                {
                    return glyphTypeface;
                }

                throw new InvalidOperationException(
                    $"Could not create glyphTypeface. Font family: {FontFamily?.Name} (key: {FontFamily?.Key}). Style: {Style}. Weight: {Weight}. Stretch: {Stretch}");
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

        /// <summary>
        /// Normalizes the typeface by extracting and removing style, weight, and stretch information from the font
        /// family name, and returns a new <see cref="Typeface"/> instance with the updated properties.
        /// </summary>
        /// <remarks>This method analyzes the font family name to identify and extract any style, weight,
        /// or stretch information embedded within it. If such information is found, it is removed from the family name,
        /// and the corresponding properties of the returned <see cref="Typeface"/> are updated accordingly. If no such
        /// information is found, the method returns the current instance without modification.</remarks>
        /// <param name="normalizedFamilyName">When this method returns, contains the normalized font family name with style, weight, and stretch
        /// information removed. This parameter is passed uninitialized.</param>
        /// <returns>A new <see cref="Typeface"/> instance with the updated <see cref="FontStyle"/>, <see cref="FontWeight"/>,
        /// and <see cref="FontStretch"/> properties, or the current instance if no normalization was performed.</returns>
        public Typeface Normalize(out string normalizedFamilyName)
        {
            normalizedFamilyName = FontFamily.FamilyNames.PrimaryFamilyName;

            //Return early if no separator is present.
            if (!normalizedFamilyName.Contains(' '))
            {
                return this;
            }

            var style = Style;
            var weight = Weight;
            var stretch = Stretch;

            StringBuilder? normalizedFamilyNameBuilder = null;
            var totalCharsRemoved = 0;

            var tokenizer = new SpanStringTokenizer(normalizedFamilyName, ' ');

            // Skip initial family name.
            tokenizer.ReadSpan();

            while (tokenizer.TryReadSpan(out var token))
            {
                // Don't try to match numbers.
                if (new SpanStringTokenizer(token).TryReadInt32(out _))
                {
                    continue;
                }

                // Try match with font style, weight or stretch and update accordingly.
                var match = false;
                if (EnumHelper.TryParse<FontStyle>(token, true, out var newStyle))
                {
                    style = newStyle;
                    match = true;
                }
                else if (EnumHelper.TryParse<FontWeight>(token, true, out var newWeight))
                {
                    weight = newWeight;
                    match = true;
                }
                else if (EnumHelper.TryParse<FontStretch>(token, true, out var newStretch))
                {
                    stretch = newStretch;
                    match = true;
                }

                if (match)
                {
                    // Carve out matched word from the normalized name.
                    normalizedFamilyNameBuilder ??= new StringBuilder(normalizedFamilyName);
                    normalizedFamilyNameBuilder.Remove(tokenizer.CurrentTokenIndex - totalCharsRemoved, token.Length);
                    totalCharsRemoved += token.Length;
                }
            }

            // Get rid of any trailing spaces.
            normalizedFamilyName = (normalizedFamilyNameBuilder?.ToString() ?? normalizedFamilyName).TrimEnd();

            //Preserve old font source
            return new Typeface(FontFamily, style, weight, stretch);
        }
    }
}
