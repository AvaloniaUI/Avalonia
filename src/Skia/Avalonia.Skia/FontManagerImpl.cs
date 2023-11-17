using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
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

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (checkForUpdates)
            {
                _skFontManager = SKFontManager.CreateDefault();
            }

            return _skFontManager.GetFontFamilies();
        }

        [ThreadStatic] private static string[]? t_languageTagBuffer;

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch, CultureInfo? culture, out Typeface fontKey)
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

            var skTypeface = _skFontManager.MatchCharacter(null, skFontStyle, t_languageTagBuffer, codepoint);

            if (skTypeface != null)
            {
                fontKey = new Typeface(skTypeface.FamilyName, fontStyle, fontWeight, fontStretch);

                return true;
            }

            fontKey = default;

            return false;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var fontStyle = new SKFontStyle((SKFontStyleWeight)weight, (SKFontStyleWidth)stretch,
                (SKFontStyleSlant)style);

            var skTypeface = _skFontManager.MatchFamily(familyName, fontStyle);

            if (skTypeface is null)
            {
                return false;
            }

            var fontSimulations = FontSimulations.None;

            if ((int)weight >= 600 && !skTypeface.IsBold)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if (style == FontStyle.Italic && !skTypeface.IsItalic)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            glyphTypeface = new GlyphTypefaceImpl(skTypeface, fontSimulations);

            return true;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            var skTypeface = SKTypeface.FromStream(stream);

            if (skTypeface != null)
            {
                glyphTypeface = new GlyphTypefaceImpl(skTypeface, fontSimulations);

                return true;
            }

            glyphTypeface = null;

            return false;
        }
    }
}
