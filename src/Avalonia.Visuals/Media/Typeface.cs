using System;
using Avalonia.Media;

namespace Avalonia.Media
{
    public class Typeface
    {
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

        public string FontFamilyName { get; }
        public double FontSize { get; }
        public FontStyle Style { get; }
        public FontWeight Weight { get; }
    }
}
