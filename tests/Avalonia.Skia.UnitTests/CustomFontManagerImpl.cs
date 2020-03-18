using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia.UnitTests
{
    public class CustomFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;

        private readonly Typeface _defaultTypeface =
            new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");
        private readonly Typeface _emojiTypeface =
            new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Twitter Color Emoji");

        public CustomFontManagerImpl()
        {
            _customTypefaces = new[] { _emojiTypeface, _defaultTypeface };
        }

        public string GetDefaultFontFamilyName()
        {
            return _defaultTypeface.FontFamily.ToString();
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return _customTypefaces.Select(x => x.FontFamily.Name);
        }

        public bool TryMatchCharacter(int codepoint, FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily,
            CultureInfo culture, out FontKey fontKey)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                if (customTypeface.GlyphTypeface.GetGlyph((uint)codepoint) == 0)
                    continue;
                fontKey = new FontKey(customTypeface.FontFamily, fontWeight, fontStyle);

                return true;
            }

            var fallback = SKFontManager.Default.MatchCharacter(codepoint);

            fontKey = new FontKey(fallback?.FamilyName ?? SKTypeface.Default.FamilyName, fontWeight, fontStyle);

            return true;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            switch (typeface.FontFamily.Name)
            {
                case "Twitter Color Emoji":
                case "Noto Mono":
                    var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(typeface.FontFamily);
                    var skTypeface = typefaceCollection.Get(typeface);
                    return new GlyphTypefaceImpl(skTypeface);
                default:
                    return new GlyphTypefaceImpl(SKTypeface.FromFamilyName(typeface.FontFamily.Name,
                        (SKFontStyleWeight)typeface.Weight, SKFontStyleWidth.Normal, (SKFontStyleSlant)typeface.Style));
            }
        }
    }
}
