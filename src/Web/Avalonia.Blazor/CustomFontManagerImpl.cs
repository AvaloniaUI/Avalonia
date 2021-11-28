using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Blazor
{
    public class CustomFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;
        private readonly string _defaultFamilyName;

        private readonly Typeface _defaultTypeface =
            new Typeface("avares://Avalonia.Blazor/Assets#Noto Mono");
        private readonly Typeface _italicTypeface =
            new Typeface("avares://Avalonia.Blazor/Assets#Noto Sans");
        private readonly Typeface _emojiTypeface =
            new Typeface("avares://Avalonia.Blazor/Assets#Twitter Color Emoji");

        public CustomFontManagerImpl()
        {
            _customTypefaces = new[] { _emojiTypeface, _italicTypeface, _defaultTypeface };
            _defaultFamilyName = _defaultTypeface.FontFamily.FamilyNames.PrimaryFamilyName;
        }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return _customTypefaces.Select(x => x.FontFamily.Name);
        }

        private readonly string[] _bcp47 = { CultureInfo.CurrentCulture.ThreeLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontFamily fontFamily,
            CultureInfo culture, out Typeface typeface)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                if (customTypeface.GlyphTypeface.GetGlyph((uint)codepoint) == 0)
                {
                    continue;
                }

                typeface = new Typeface(customTypeface.FontFamily, fontStyle, fontWeight);

                return true;
            }

            typeface = _defaultTypeface;

            return true;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            SKTypeface skTypeface;

            switch (typeface.FontFamily.Name)
            {
                case "Twitter Color Emoji":
                    {
                        var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(_emojiTypeface.FontFamily);
                        skTypeface = typefaceCollection.Get(typeface);
                        break;
                    }
                case "Noto Sans":
                    {
                        var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(_italicTypeface.FontFamily);
                        skTypeface = typefaceCollection.Get(typeface);
                        break;
                    }
                default:
                    {
                        var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(_defaultTypeface.FontFamily);
                        skTypeface = typefaceCollection.Get(_defaultTypeface);
                        break;
                    }
            }

            return new GlyphTypefaceImpl(skTypeface);
        }
    }
}
