using System.Collections.Concurrent;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SKTypefaceCollection
    {
        private readonly ConcurrentDictionary<FontKey, SKTypeface> _cachedTypefaces =
            new ConcurrentDictionary<FontKey, SKTypeface>();

        public void AddTypeFace(SKTypeface typeface)
        {
            var key = new FontKey(typeface.FamilyName, (SKFontStyleWeight)typeface.FontWeight, typeface.FontSlant);

            _cachedTypefaces.TryAdd(key, typeface);
        }

        public SKTypeface GetTypeFace(Typeface typeface)
        {
            SKFontStyleSlant skStyle = SKFontStyleSlant.Upright;

            switch (typeface.Style)
            {
                case FontStyle.Italic:
                    skStyle = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    skStyle = SKFontStyleSlant.Oblique;
                    break;
            }

            var key = new FontKey(typeface.FontFamily.Name, (SKFontStyleWeight)typeface.Weight, skStyle);

            return _cachedTypefaces.TryGetValue(key, out var skTypeface) ? skTypeface : TypefaceCache.Default;
        }

        private struct FontKey
        {
            public readonly string Name;
            public readonly SKFontStyleSlant Slant;
            public readonly SKFontStyleWeight Weight;

            public FontKey(string name, SKFontStyleWeight weight, SKFontStyleSlant slant)
            {
                Name = name;
                Slant = slant;
                Weight = weight;
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + (int)Slant;
                hash = hash * 31 + (int)Weight;

                return hash;
            }

            public override bool Equals(object other)
            {
                return other is FontKey ? Equals((FontKey)other) : false;
            }

            private bool Equals(FontKey other)
            {
                return Name == other.Name && Slant == other.Slant &&
                       Weight == other.Weight;
            }
        }
    }
}
