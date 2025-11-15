using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia.UnitTests.Media
{
    public class CustomFontManagerImpl : IFontManagerImpl, IDisposable
    {
        private readonly string _defaultFamilyName;
        private readonly IFontCollection _customFonts;
        private bool _isInitialized;

        public CustomFontManagerImpl()
        {
            var source = new Uri("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests");

            _defaultFamilyName = source.AbsoluteUri + "#Noto Mono";

            _customFonts = new EmbeddedFontCollection(source, source);
        }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (!_isInitialized)
            {
                _customFonts.Initialize(this);

                _isInitialized = true;
            }

            return _customFonts.Select(x => x.Name).ToArray();
        }

        private readonly string[] _bcp47 = { CultureInfo.CurrentCulture.ThreeLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch,
            CultureInfo culture, out IPlatformTypeface typeface)
        {
            if (!_isInitialized)
            {
                _customFonts.Initialize(this);
            }

            if (_customFonts.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, null, culture, out var match))
            {
                typeface = match.GlyphTypeface.PlatformTypeface;

                return true;
            }

            var fallback = SKFontManager.Default.MatchCharacter(null, (SKFontStyleWeight)fontWeight,
                (SKFontStyleWidth)fontStretch, (SKFontStyleSlant)fontStyle, _bcp47, codepoint);

            if (fallback == null)
            {
                typeface = null;

                return false;
            }

            typeface = new SkiaTypeface(fallback, FontSimulations.None);

            return true;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface platformTypeface)
        {
            if (!_isInitialized)
            {
                _customFonts.Initialize(this);
            }

            if (_customFonts.TryGetGlyphTypeface(familyName, style, weight, stretch, out var glyphTypeface))
            {
                platformTypeface = glyphTypeface.PlatformTypeface;

                return true;
            }

            var skTypeface = SKTypeface.FromFamilyName(familyName,
                        (SKFontStyleWeight)weight, SKFontStyleWidth.Normal, (SKFontStyleSlant)style);

            platformTypeface = new SkiaTypeface(skTypeface, FontSimulations.None);

            return true;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface platformTypeface)
        {
            var skTypeface = SKTypeface.FromStream(stream);

            platformTypeface = new SkiaTypeface(skTypeface, FontSimulations.None);

            return true;
        }

        public void Dispose()
        {
            _customFonts.Dispose();
        }

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
        {
            throw new NotImplementedException();
        }
    }
}
