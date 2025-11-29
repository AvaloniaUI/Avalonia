#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class FontManagerImpl : IFontManagerImpl, IFontManagerImpl2
    {
        private readonly ConcurrentDictionary<string, FontFamily> _fontFamilyMappings = new(StringComparer.OrdinalIgnoreCase);
        private SKFontManager _skFontManager = SKFontManager.Default;
        private string[]? _installedFontFamilyNames;
        private readonly object _installedFontLock = new();

        public string GetDefaultFontFamilyName()
        {
            return SKTypeface.Default.FamilyName;
        }

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (checkForUpdates)
            {
                lock (_installedFontLock)
                {
                    _installedFontFamilyNames = null;
                    _skFontManager = SKFontManager.CreateDefault();
                }
            }

            // Fast path without locking
            var result = _installedFontFamilyNames;

            if (result == null)
            {
                lock (_installedFontLock)
                {
                    _installedFontFamilyNames ??= _skFontManager.GetFontFamilies();

                    result = _installedFontFamilyNames;
                }
            }

            return result;
        }

        [ThreadStatic] private static string[]? t_languageTagBuffer;

        public IReadOnlyDictionary<string, FontFamily> FontFamilyMappings => _fontFamilyMappings;

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch, string? familyName, CultureInfo? culture, out Typeface fontKey)
        {
            if (!TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out SKTypeface? skTypeface))
            {
                fontKey = default;

                return false;
            }

            fontKey = new Typeface(
                skTypeface.FamilyName, 
                skTypeface.FontStyle.Slant.ToAvalonia(), 
                (FontWeight)skTypeface.FontStyle.Weight, 
                (FontStretch)skTypeface.FontStyle.Width);

            skTypeface.Dispose();

            return true;

        }

        public bool TryMatchCharacter(
            int codepoint,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            string? familyName,
            CultureInfo? culture,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (!TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out SKTypeface? skTypeface))
            {
                glyphTypeface = null;

                return false;
            }

            glyphTypeface = new GlyphTypefaceImpl(skTypeface, FontSimulations.None);

            return true;
        }

        private bool TryMatchCharacter(
            int codepoint,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            string? familyName,
            CultureInfo? culture,
            [NotNullWhen(true)] out SKTypeface? skTypeface)
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
                    skFontStyle = new SKFontStyle((SKFontStyleWeight)fontWeight, (SKFontStyleWidth)fontStretch, fontStyle.ToSkia());
                    break;
            }

            culture ??= CultureInfo.CurrentUICulture;

            t_languageTagBuffer ??= new string[1];
            t_languageTagBuffer[0] = culture.Name;

            skTypeface = _skFontManager.MatchCharacter(string.IsNullOrEmpty(familyName) ? null : familyName, skFontStyle, t_languageTagBuffer, codepoint);

            return skTypeface != null;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var fontStyle = new SKFontStyle((SKFontStyleWeight)weight, (SKFontStyleWidth)stretch, style.ToSkia());

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

            if (!string.Equals(familyName, glyphTypeface.FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                // The platform gave us a different font than we requested it might be an alias so we need to map it.
                _fontFamilyMappings.TryAdd(familyName, new FontFamily(glyphTypeface.FamilyName));
            }

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

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            var set = _skFontManager.GetFontStyles(familyName);

            if (set.Count == 0)
            {
                return false;
            }

            var typefaces = new List<Typeface>(set.Count);

            foreach (var fontStyle in set)
            {
                typefaces.Add(new Typeface(familyName, fontStyle.Slant.ToAvalonia(), (FontWeight)fontStyle.Weight, (FontStretch)fontStyle.Width));
            }

            familyTypefaces = typefaces;

            return true;
        }
    }
}
