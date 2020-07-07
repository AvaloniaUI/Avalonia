﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia.UnitTests.Media
{
    public class CustomFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;
        private readonly string _defaultFamilyName;

        private readonly Typeface _defaultTypeface =
            new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");
        private readonly Typeface _italicTypeface =
            new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Sans");
        private readonly Typeface _emojiTypeface =
            new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Twitter Color Emoji");

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

        public bool TryMatchCharacter(int codepoint, FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily,
            CultureInfo culture, out FontKey fontKey)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                if (customTypeface.GlyphTypeface.GetGlyph((uint)codepoint) == 0)
                {
                    continue;
                }

                fontKey = new FontKey(customTypeface.FontFamily.Name, fontWeight, fontStyle);

                return true;
            }

            var fallback = SKFontManager.Default.MatchCharacter(fontFamily?.Name, (SKFontStyleWeight)fontWeight,
                SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle, _bcp47, codepoint);

            fontKey = new FontKey(fallback?.FamilyName ?? _defaultFamilyName, fontWeight, fontStyle);

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
                case "Noto Mono":
                    {
                        var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(_defaultTypeface.FontFamily);
                        skTypeface = typefaceCollection.Get(typeface);
                        break;
                    }
                default:
                    {
                        skTypeface = SKTypeface.FromFamilyName(typeface.FontFamily.Name,
                            (SKFontStyleWeight)typeface.Weight, SKFontStyleWidth.Normal, (SKFontStyleSlant)typeface.Style);
                        break;
                    }
            }

            return new GlyphTypefaceImpl(skTypeface);
        }
    }
}
