using System;

namespace Avalonia.Media.Fonts
{
    public readonly struct FontKey : IEquatable<FontKey>
    {
        public readonly string FamilyName;
        public readonly FontStyle Style;
        public readonly FontWeight Weight;

        public FontKey(string familyName, FontWeight weight, FontStyle style)
        {
            FamilyName = familyName;
            Style = style;
            Weight = weight;
        }

        public override int GetHashCode()
        {
            var hash = FamilyName.GetHashCode();

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
            return FamilyName == other.FamilyName &&
                Style == other.Style &&
                   Weight == other.Weight;
        }
    }
}
