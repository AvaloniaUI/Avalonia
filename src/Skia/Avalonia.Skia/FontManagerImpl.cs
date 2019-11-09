// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        public FontKey MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null)
        {
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

                    return new FontKey(new FontFamily(familyName), fontWeight, fontStyle);
                }
            }
            else
            {
                var skTypeface = _skFontManager.MatchCharacter(null, (SKFontStyleWeight)fontWeight, SKFontStyleWidth.Normal,
                    (SKFontStyleSlant)fontStyle,
                    new[] { culture.TwoLetterISOLanguageName, culture.ThreeLetterISOLanguageName }, codepoint);

                if (skTypeface != null)
                {
                    return new FontKey(new FontFamily(skTypeface.FamilyName), fontWeight, fontStyle);
                }
            }

            return new FontKey(FontFamily.Default, fontWeight, fontStyle);
        }
    }
}
