using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Native.Interop;
using Avalonia.Native.Interop.Impl;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class FontManagerImpl : IFontManagerImpl
    {
        private readonly IAvgFactory _factory;
        private IAvgFontManager _native;

        public FontManagerImpl(IAvgFactory factory)
        {
            _factory = factory;
            _native = _factory.CreateAvgFontManager();
        }

        public IAvgFontManager Native => _native; 
        
        public string GetDefaultFontFamilyName()
        {
            var mystring = _native.DefaultFamilyName;

            return mystring.ToString();
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            var count = _native.FontFamilyCount;

            for (var i = 0; i < count; i++)
            {
                yield return _native.GetFamilyName(i).ToString();
            }
        }
        
        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            AvgTypeface t = new AvgTypeface();
            t.FontWeight = (int)typeface.Weight;
            switch (typeface.Style)
            {
                case FontStyle.Normal:
                    t.FontStyle = AvgFontStyle.Normal;
                    break;
                case FontStyle.Italic:
                    t.FontStyle = AvgFontStyle.Italic;
                    break;
                
                case FontStyle.Oblique:
                    t.FontStyle = AvgFontStyle.Oblique;
                    break;
            }
            
            return new GlyphTypefaceImpl(_native.CreateGlyphTypeface(typeface.FontFamily.Name, t));
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, FontFamily fontFamily, CultureInfo culture, out Typeface typeface)
        {
            typeface = Typeface.Default;

            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            if (fontFamily != null && fontFamily.FamilyNames.HasFallbacks)
            {
                var familyNames = fontFamily.FamilyNames;

                foreach (var name in familyNames)
                {
                    Console.WriteLine($"Matching: {name}");
                }
            }

            return true;
        }
    }
}
