using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class HarfBuzzFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;
        private readonly string _defaultFamilyName;

        private static readonly Typeface _defaultTypeface =
            new Typeface("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Mono");
        private  static readonly Typeface _italicTypeface =
            new Typeface("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Sans");
        private  static readonly Typeface _emojiTypeface =
            new Typeface("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Twitter Color Emoji");

        public HarfBuzzFontManagerImpl(string defaultFamilyName = "resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Mono")
        {
            _customTypefaces = new[] { _emojiTypeface, _italicTypeface, _defaultTypeface };
            _defaultFamilyName = defaultFamilyName;
        }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return _customTypefaces.Select(x => x.FontFamily!.Name);
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch,
            FontFamily fontFamily, CultureInfo culture, out Typeface fontKey)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                var glyphTypeface = customTypeface.GlyphTypeface;

                if (!glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                {
                    continue;
                }
                
                fontKey = customTypeface;
                    
                return true;
            }

            fontKey = default;

            return false;
        }

        public IGlyphTypeface CreateGlyphTypeface(Typeface typeface)
        {
            var fontFamily = typeface.FontFamily;

            if (fontFamily.IsDefault)
            {
                fontFamily = _defaultTypeface.FontFamily;
            }
            
            if (fontFamily!.Key == null)
            {
                return null;
            }
            
            var fontAssets = FontFamilyLoader.LoadFontAssets(fontFamily.Key);

            var asset = fontAssets.First();
            
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var stream = assetLoader.Open(asset);
            
            return new HarfBuzzGlyphTypefaceImpl(stream);
        }
    }
}
