using System;

namespace Avalonia.Media.Fonts
{
    public readonly struct FontKey : IEquatable<FontKey>
    {
        public readonly FontFamily FontFamily;
        public readonly FontStyle Style;
        public readonly FontWeight Weight;

        public FontKey(FontFamily fontFamily, FontWeight weight, FontStyle style)
        {
            FontFamily = fontFamily;
            Style = style;
            Weight = weight;
        }

        public override int GetHashCode()
        {
            var hash = FontFamily.GetHashCode();

            hash = hash * 31 + (int)Style;
            hash = hash * 31 + (int)Weight;

            return hash;
        }

        public override bool Equals(object other)
        {
            return other is FontKey key && Equals(key);
        }

        public bool Equals(FontKey other)
        {
            return FontFamily == other.FontFamily &&
                Style == other.Style &&
                   Weight == other.Weight;
        }
    }
}
