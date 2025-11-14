using System;
using System.Collections.Generic;
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
        private readonly string[] _bcp47 = { CultureInfo.CurrentCulture.ThreeLetterISOLanguageName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName };
        private IFontCollection? _systemFonts;

        public CustomFontManagerImpl()
        {
            _defaultFamilyName = FontManager.SystemFontsKey + "#Noto Mono";
        }

        public IFontCollection SystemFonts
        {
            get
            {
                if(_systemFonts is null)
                {
                    var source = new Uri("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests");

                    _systemFonts = new EmbeddedFontCollection(FontManager.SystemFontsKey, source);
                }

                return _systemFonts;
            }
        }
        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            // Directly load from assets to avoid creating the full font collection
            try
            {
                var key = new Uri("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests");

                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                var fontAssets = FontFamilyLoader.LoadFontAssets(key);
                var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (var fontAsset in fontAssets)
                {
                    try
                    {
                        using var stream = assetLoader.Open(fontAsset);
                        using var sk = SKTypeface.FromStream(stream);

                        if (sk != null && !string.IsNullOrEmpty(sk.FamilyName))
                        {
                            names.Add(sk.FamilyName);
                        }
                    }
                    catch
                    {
                        // Ignore faulty assets
                    }
                }

                return names.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch,
            string? familyName, CultureInfo? culture, out Typeface typeface)
        {
            if(SystemFonts.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out typeface))
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
            _systemFonts?.Dispose();
        }
    }
}
