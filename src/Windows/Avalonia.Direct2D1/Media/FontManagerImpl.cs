using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Platform;
using Vortice.DirectWrite;
using FontFamily = Avalonia.Media.FontFamily;
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

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
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
            FontWeight fontWeight,
            FontFamily fontFamily, CultureInfo culture, out Typeface typeface)
        {
            var familyCount = Direct2D1FontCollectionCache.InstalledFontCollection.FontFamilyCount;

            for (var i = 0; i < familyCount; i++)
            {
                var font = Direct2D1FontCollectionCache.InstalledFontCollection.GetFontFamily(i)
                    .GetMatchingFonts((Vortice.DirectWrite.FontWeight)fontWeight, FontStretch.Normal,
                        (Vortice.DirectWrite.FontStyle)fontStyle).GetFont(0);

                if (!font.HasCharacter(codepoint))
                {
                    continue;
                }

                var fontFamilyName = font.FontFamily.FamilyNames.GetString(0);

                typeface = new Typeface(fontFamilyName, fontStyle, fontWeight);

                return true;
            }

            typeface = default;

            return false;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            return new GlyphTypefaceImpl(typeface);
        }
    }
}
