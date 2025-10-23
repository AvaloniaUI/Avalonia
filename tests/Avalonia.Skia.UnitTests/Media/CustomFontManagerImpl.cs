using System;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Avalonia.Skia.UnitTests.Media
{
    public class CustomFontManagerImpl : IFontManagerImpl, IDisposable
    {
        private readonly string _defaultFamilyName;
        private bool _isInitialized;

        public CustomFontManagerImpl()
        {
            _defaultFamilyName = FontManager.SystemFontsKey + "#Noto Mono";

            SystemFonts = new EmbeddedFontCollection(FontManager.SystemFontsKey, new Uri("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests"));
        }

        public IFontCollection SystemFonts { get; }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (!_isInitialized)
            {
                SystemFonts.Initialize(this);

                _isInitialized = true;
            }

            return SystemFonts.Select(x=> x.Name).ToArray();
        }

        private readonly string[] _bcp47 = { CultureInfo.CurrentCulture.ThreeLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch,
            CultureInfo culture, out Typeface typeface)
        {
            if (!_isInitialized)
            {
                SystemFonts.Initialize(this);
            }

            if(SystemFonts.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, null, culture, out typeface))
            {
                return true;
            }

            var fallback = SKFontManager.Default.MatchCharacter(null, (SKFontStyleWeight)fontWeight,
                (SKFontStyleWidth)fontStretch, (SKFontStyleSlant)fontStyle, _bcp47, codepoint);

            typeface = new Typeface(fallback?.FamilyName ?? _defaultFamilyName, fontStyle, fontWeight);

            return true;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface glyphTypeface)
        {
            if (!_isInitialized)
            {
                SystemFonts.Initialize(this);
            }

            if (SystemFonts.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface))
            {
                return true;
            }

            var skTypeface = SKTypeface.FromFamilyName(familyName,
                        (SKFontStyleWeight)weight, SKFontStyleWidth.Normal, (SKFontStyleSlant)style);

            glyphTypeface = new GlyphTypefaceImpl(skTypeface, FontSimulations.None);

            return true;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IGlyphTypeface glyphTypeface)
        {
            var skTypeface = SKTypeface.FromStream(stream);

            glyphTypeface = new GlyphTypefaceImpl(skTypeface, fontSimulations);

            return true;
        }

        public void Dispose()
        {
            SystemFonts.Dispose();
        }
    }
}
