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

        [ThreadStatic] private static string[]? t_languageTagBuffer;

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch,
            FontFamily? fontFamily, CultureInfo? culture, out Typeface fontKey)
        {
            SKFontStyle skFontStyle;

            switch (fontWeight)
            {
                case FontWeight.Normal when fontStyle == FontStyle.Normal && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.Normal;
                    break;
                case FontWeight.Normal when fontStyle == FontStyle.Italic && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.Italic;
                    break;
                case FontWeight.Bold when fontStyle == FontStyle.Normal && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.Bold;
                    break;
                case FontWeight.Bold when fontStyle == FontStyle.Italic && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.BoldItalic;
                    break;
                default:
                    skFontStyle = new SKFontStyle((SKFontStyleWeight)fontWeight, (SKFontStyleWidth)fontStretch, (SKFontStyleSlant)fontStyle);
                    break;
            }

            culture ??= CultureInfo.CurrentUICulture;

            t_languageTagBuffer ??= new string[2];
            t_languageTagBuffer[0] = culture.TwoLetterISOLanguageName;
            t_languageTagBuffer[1] = culture.ThreeLetterISOLanguageName;

            if (fontFamily is not null && fontFamily.FamilyNames.HasFallbacks)
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

                    fontKey = new Typeface(skTypeface.FamilyName, fontStyle, fontWeight, fontStretch);

                    return true;
                }
            }
            else
            {
                var skTypeface = _skFontManager.MatchCharacter(null, skFontStyle, t_languageTagBuffer, codepoint);

                if (skTypeface != null)
                {
                    fontKey = new Typeface(skTypeface.FamilyName, fontStyle, fontWeight, fontStretch);

                    return true;
                }
            }

            fontKey = default;

            return false;
        }

        public IGlyphTypeface CreateGlyphTypeface(Typeface typeface)
        {
            SKTypeface? skTypeface = null;

            if (typeface.FontFamily.Key is null)
            {
                var defaultName = SKTypeface.Default.FamilyName;

                var fontStyle = new SKFontStyle((SKFontStyleWeight)typeface.Weight, (SKFontStyleWidth)typeface.Stretch,
                    (SKFontStyleSlant)typeface.Style);

                foreach (var familyName in typeface.FontFamily.FamilyNames)
                {
                    if(familyName == FontFamily.DefaultFontFamilyName)
                    {
                        continue;
                    }

                    skTypeface = _skFontManager.MatchFamily(familyName, fontStyle);

                    if (skTypeface is null || defaultName.Equals(skTypeface.FamilyName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    break;
                }

                // MatchTypeface can return "null" if matched typeface wasn't found for the style
                // Fallback to the default typeface and styles instead.
                skTypeface ??= _skFontManager.MatchTypeface(SKTypeface.Default, fontStyle)
                    ?? SKTypeface.Default;
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

            var fontSimulations = FontSimulations.None;

            if((int)typeface.Weight >= 600 && !skTypeface.IsBold)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if(typeface.Style == FontStyle.Italic && !skTypeface.IsItalic)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            return new GlyphTypefaceImpl(skTypeface, fontSimulations);
        }
    }
}
