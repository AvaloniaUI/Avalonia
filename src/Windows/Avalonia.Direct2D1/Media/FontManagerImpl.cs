﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;
using FontFamily = Avalonia.Media.FontFamily;
using FontStretch = Avalonia.Media.FontStretch;
using FontStyle = Avalonia.Media.FontStyle;
using FontWeight = Avalonia.Media.FontWeight;

namespace Avalonia.Direct2D1.Media
{
    internal class FontManagerImpl : IFontManagerImpl
    {
        public string GetDefaultFontFamilyName()
        {
            //ToDo: Implement a real lookup of the system's default font.
            return "Segoe UI";
        }

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            var familyCount = Direct2D1FontCollectionCache.InstalledFontCollection.FontFamilyCount;

            var fontFamilies = new string[familyCount];

            for (var i = 0; i < familyCount; i++)
            {
                fontFamilies[i] = Direct2D1FontCollectionCache.InstalledFontCollection.GetFontFamily(i).FamilyNames.GetString(0);
            }

            return fontFamilies;
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch,
            FontFamily fontFamily, CultureInfo culture, out Typeface typeface)
        {
            var familyCount = Direct2D1FontCollectionCache.InstalledFontCollection.FontFamilyCount;

            for (var i = 0; i < familyCount; i++)
            {
                var font = Direct2D1FontCollectionCache.InstalledFontCollection.GetFontFamily(i)
                    .GetMatchingFonts((SharpDX.DirectWrite.FontWeight)fontWeight,
                        (SharpDX.DirectWrite.FontStretch)fontStretch,
                        (SharpDX.DirectWrite.FontStyle)fontStyle).GetFont(0);

                if (!font.HasCharacter(codepoint))
                {
                    continue;
                }

                var fontFamilyName = font.FontFamily.FamilyNames.GetString(0);

                typeface = new Typeface(fontFamilyName, fontStyle, fontWeight, fontStretch);

                return true;
            }

            typeface = default;

            return false;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface glyphTypeface)
        {
            var systemFonts = Direct2D1FontCollectionCache.InstalledFontCollection;

            if (familyName == FontFamily.DefaultFontFamilyName)
            {
                familyName = "Segoe UI";
            }

            if (systemFonts.FindFamilyName(familyName, out var index))
            {
                var font = systemFonts.GetFontFamily(index).GetFirstMatchingFont(
                    (SharpDX.DirectWrite.FontWeight)weight,
                    (SharpDX.DirectWrite.FontStretch)stretch,
                    (SharpDX.DirectWrite.FontStyle)style);

                glyphTypeface = new GlyphTypefaceImpl(font);

                return true;
            }

            glyphTypeface = null;

            return false;
        }

        public bool TryCreateGlyphTypeface(Stream stream, out IGlyphTypeface glyphTypeface)
        {
            var fontLoader = new DWriteResourceFontLoader(Direct2D1Platform.DirectWriteFactory, new[] { stream });

            var fontCollection = new SharpDX.DirectWrite.FontCollection(Direct2D1Platform.DirectWriteFactory, fontLoader, fontLoader.Key);

            if (fontCollection.FontFamilyCount > 0)
            {
                var fontFamily = fontCollection.GetFontFamily(0);

                if (fontFamily.FontCount > 0)
                {
                    var font = fontFamily.GetFont(0);

                    glyphTypeface = new GlyphTypefaceImpl(font);

                    return true;
                }
            }

            glyphTypeface = null;

            return false;
        }
    }
}
