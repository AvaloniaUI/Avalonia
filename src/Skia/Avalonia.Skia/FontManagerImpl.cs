// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        public FontManagerImpl()
        {
            DefaultFontFamilyName = SKTypeface.Default.FamilyName;
        }

        public string DefaultFontFamilyName { get; }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (checkForUpdates)
            {
                _skFontManager = SKFontManager.CreateDefault();
            }

            return _skFontManager.FontFamilies;
        }

        public Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle)
        {
            return TypefaceCache.Get(fontFamily.Name, fontWeight, fontStyle).Typeface;
        }

        public Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null)
        {
            var fontFamilyName = FontFamily.Default.Name;

            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            if (fontFamily != null)
            {
                foreach (var familyName in fontFamily.FamilyNames)
                {
                    var skTypeface = _skFontManager.MatchCharacter(familyName, (SKFontStyleWeight)fontWeight,
                        SKFontStyleWidth.Normal,
                        (SKFontStyleSlant)fontStyle,
                        new[] { culture.TwoLetterISOLanguageName, culture.ThreeLetterISOLanguageName }, codepoint);

                    if (skTypeface == null)
                    {
                        continue;
                    }

                    fontFamilyName = familyName;

                    break;
                }
            }
            else
            {
                var skTypeface = _skFontManager.MatchCharacter(null, (SKFontStyleWeight)fontWeight, SKFontStyleWidth.Normal,
                    (SKFontStyleSlant)fontStyle,
                    new[] { culture.TwoLetterISOLanguageName, culture.ThreeLetterISOLanguageName }, codepoint);

                if (skTypeface != null)
                {
                    fontFamilyName = skTypeface.FamilyName;
                }
            }

            return GetTypeface(fontFamilyName, fontWeight, fontStyle);
        }
    }
}
