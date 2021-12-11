using System.Globalization;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Web.Blazor
{
    public class CustomFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;
        private readonly string _defaultFamilyName;

        private readonly Typeface _defaultTypeface =
            new Typeface("avares://Avalonia.Web.Blazor/Assets#Noto Mono");
        private readonly Typeface _italicTypeface =
            new Typeface("avares://Avalonia.Web.Blazor/Assets#Noto Sans");

        public CustomFontManagerImpl()
        {
            _customTypefaces = new[] { _italicTypeface, _defaultTypeface };
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
