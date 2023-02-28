using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : IFontCollection
    {
        private readonly Dictionary<string, Dictionary<FontCollectionKey, IGlyphTypeface>> _glyphTypefaceCache =
           new Dictionary<string, Dictionary<FontCollectionKey, IGlyphTypeface>>();

        private readonly FontManager _fontManager;
        private readonly string[] _familyNames;

        public SystemFontCollection(FontManager fontManager)
        {
            _fontManager = fontManager;
            _familyNames = fontManager.PlatformImpl.GetInstalledFontFamilyNames();
        }

        public Uri Key => new Uri("fontCollection:SystemFonts");

        public FontFamily this[int index]
        {
            get
            {
                var familyName = _familyNames[index];

                return new FontFamily(familyName);
            }
        }

        public int Count => _familyNames.Length;

        public bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (familyName == FontFamily.DefaultFontFamilyName)
            {
                familyName = _fontManager.DefaultFontFamilyName;
            }

            var key = new FontCollectionKey(style, weight, stretch);

            if (!_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                glyphTypefaces = new Dictionary<FontCollectionKey, IGlyphTypeface>();

                _glyphTypefaceCache.Add(familyName, glyphTypefaces);
            }

            if (glyphTypefaces.TryGetValue(key, out glyphTypeface))
            {
                return true;
            }

            if (_fontManager.PlatformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface))
            {
                glyphTypefaces.Add(key, glyphTypeface);

                return true;
            }

            return false;
        }

        public void Initialize(IFontManagerImpl fontManager)
        {
            //We initialize the system font collection during construction.
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<FontFamily> GetEnumerator()
        {
            foreach (var familyName in _familyNames)
            {
                yield return new FontFamily(familyName);
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var glyphTypefaces in _glyphTypefaceCache.Values)
            {
                foreach (var pair in glyphTypefaces)
                {
                    pair.Value.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
