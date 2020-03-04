// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Fonts;
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

        public bool TryMatchCharacter(int codepoint, FontWeight fontWeight, FontStyle fontStyle,
            FontFamily fontFamily, CultureInfo culture, out FontKey fontKey)
        {
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

            if (fontFamily != null)
            {
                foreach (var familyName in fontFamily.FamilyNames)
                {
                    var skTypeface = _skFontManager.MatchCharacter(familyName, (SKFontStyleWeight)fontWeight,
                        SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle, t_languageTagBuffer, codepoint);

                    if (skTypeface == null)
                    {
                        continue;
                    }

                    fontKey = new FontKey(new FontFamily(familyName), fontWeight, fontStyle);

                    return true;
                }
            }
            else
            {
                var skTypeface = _skFontManager.MatchCharacter(null, (SKFontStyleWeight)fontWeight,
                    SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle, t_languageTagBuffer, codepoint);

                if (skTypeface != null)
                {
                    fontKey = new FontKey(new FontFamily(skTypeface.FamilyName), fontWeight, fontStyle);

                    return true;
                }
            }

            fontKey = default;

            return false;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            var skTypeface = SKTypeface.Default;

            if (typeface.FontFamily.Key == null)
            {
                var defaultName = SKTypeface.Default.FamilyName;

                foreach (var familyName in typeface.FontFamily.FamilyNames)
                {
                    skTypeface = SKTypeface.FromFamilyName(familyName, (SKFontStyleWeight)typeface.Weight,
                        SKFontStyleWidth.Normal, (SKFontStyleSlant)typeface.Style);

                    if (!skTypeface.FamilyName.Equals(familyName, StringComparison.Ordinal) &&
                        defaultName.Equals(skTypeface.FamilyName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    break;
                }
            }
            else
            {
                var fontCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(typeface.FontFamily);

                skTypeface = fontCollection.Get(typeface);
            }

            return new GlyphTypefaceImpl(skTypeface);
        }
    }
}
