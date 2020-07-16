using System;

namespace Avalonia.Media.Fonts
{
    public readonly struct FontKey : IEquatable<FontKey>
    {
        public FontKey(string familyName, FontStyle style, FontWeight weight)
        {
            FamilyName = familyName;
            Style = style;
            Weight = weight;
        }

        public string FamilyName { get; }
        public FontStyle Style { get; }
        public FontWeight Weight { get; }

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
