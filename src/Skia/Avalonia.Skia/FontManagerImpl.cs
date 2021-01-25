using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class FontManagerImpl : IFontManagerImpl
    {
        private SKFontManager _skFontManager = SKFontManager.Default;

        public string GetDefaultFontFamilyName()
        {
            return SKTypeface.Default.FamilyName;
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (checkForUpdates)
            {
                _skFontManager = SKFontManager.CreateDefault();
            }

            return _skFontManager.FontFamilies;
        }

        [ThreadStatic] private static string[] t_languageTagBuffer;

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight,
            FontFamily fontFamily, CultureInfo culture, out Typeface fontKey)
        {
            SKFontStyle skFontStyle;

            switch (fontWeight)
            {
                case FontWeight.Normal when fontStyle == FontStyle.Normal:
                    skFontStyle = SKFontStyle.Normal;
                    break;
                case FontWeight.Normal when fontStyle == FontStyle.Italic:
                    skFontStyle = SKFontStyle.Italic;
                    break;
                case FontWeight.Bold when fontStyle == FontStyle.Normal:
                    skFontStyle = SKFontStyle.Bold;
                    break;
                case FontWeight.Bold when fontStyle == FontStyle.Italic:
                    skFontStyle = SKFontStyle.BoldItalic;
                    break;
                default:
                    skFontStyle = new SKFontStyle((SKFontStyleWeight)fontWeight, SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle);
                    break;
            }

            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            if (t_languageTagBuffer == null)
            {
                t_languageTagBuffer = new string[2];
            }

            t_languageTagBuffer[0] = culture.TwoLetterISOLanguageName;
            t_languageTagBuffer[1] = culture.ThreeLetterISOLanguageName;

            if (fontFamily != null && fontFamily.FamilyNames.HasFallbacks)
            {
                var familyNames = fontFamily.FamilyNames;

                for (var i = 1; i < familyNames.Count; i++)
                {
                    var skTypeface =
                        _skFontManager.MatchCharacter(familyNames[i], skFontStyle, t_languageTagBuffer, codepoint);

                    if (skTypeface == null)
                    {
                        continue;
                    }

                    fontKey = new Typeface(skTypeface.FamilyName, fontStyle, fontWeight);

                    return true;
                }
            }
            else
            {
                var skTypeface = _skFontManager.MatchCharacter(null, skFontStyle, t_languageTagBuffer, codepoint);

                if (skTypeface != null)
                {
                    fontKey = new Typeface(skTypeface.FamilyName, fontStyle, fontWeight);

                    return true;
                }
            }

            fontKey = default;

            return false;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            SKTypeface skTypeface = null;

            if (typeface.FontFamily.Key == null)
            {
                var defaultName = SKTypeface.Default.FamilyName;
                var fontStyle = new SKFontStyle((SKFontStyleWeight)typeface.Weight, SKFontStyleWidth.Normal, (SKFontStyleSlant)typeface.Style);

                foreach (var familyName in typeface.FontFamily.FamilyNames)
                {
                    skTypeface = _skFontManager.MatchFamily(familyName, fontStyle);

                    if (skTypeface is null
                        || (!skTypeface.FamilyName.Equals(familyName, StringComparison.Ordinal)
                            && defaultName.Equals(skTypeface.FamilyName, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    break;
                }

                skTypeface ??= _skFontManager.MatchTypeface(SKTypeface.Default, fontStyle);
            }
            else
            {
                var fontCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(typeface.FontFamily);

                skTypeface = fontCollection.Get(typeface);
            }

            if (skTypeface == null)
            {
                throw new InvalidOperationException(
                    $"Could not create glyph typeface for: {typeface.FontFamily.Name}.");
            }

            return new GlyphTypefaceImpl(skTypeface);
        }
    }
}
