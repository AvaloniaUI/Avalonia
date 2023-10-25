using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : FontCollectionBase
    {
        private readonly FontManager _fontManager;
        private readonly List<string> _familyNames;

        public SystemFontCollection(FontManager fontManager)
        {
            _fontManager = fontManager;
            _familyNames = fontManager.PlatformImpl.GetInstalledFontFamilyNames().ToList();
        }

        public override Uri Key => FontManager.SystemFontsKey;

        public override FontFamily this[int index]
        {
            get
            {
                var familyName = _familyNames[index];

                return new FontFamily(familyName);
            }
        }

        public override int Count => _familyNames.Count;

        public override IEnumerator<FontFamily> GetEnumerator()
        {
            foreach (var familyName in _familyNames)
            {
                yield return new FontFamily(familyName);
            }
        }

        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var key = new FontCollectionKey(style, weight, stretch);

            var glyphTypefaces = _glyphTypefaceCache.GetOrAdd(familyName,
                (_) => new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>());

            if (glyphTypefaces.TryGetValue(key, out glyphTypeface))
            {
                return glyphTypeface != null;
            }

            if(!_fontManager.PlatformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface) || 
                !glyphTypeface.FamilyName.Contains(familyName))
            {
                //Try to find nearest match if possible
                TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface);
            }

            if(glyphTypeface is IGlyphTypeface2 glyphTypeface2)
            {
                var fontSimulations = FontSimulations.None;

                if(style != FontStyle.Normal && glyphTypeface2.Style != style)
                {
                    fontSimulations |= FontSimulations.Oblique;
                }

                if((int)weight >= 600 && glyphTypeface2.Weight != weight)
                {
                    fontSimulations |= FontSimulations.Bold;
                }

                if(fontSimulations != FontSimulations.None && glyphTypeface2.TryGetStream(out var stream))
                {
                    using (stream)
                    {
                        _fontManager.PlatformImpl.TryCreateGlyphTypeface(stream, fontSimulations, out glyphTypeface);
                    }
                }
            }

            glyphTypefaces.TryAdd(key, glyphTypeface);

            return glyphTypeface != null;
        }

        public override void Initialize(IFontManagerImpl fontManager)
        {
            //We initialize the system font collection during construction.
        }

        public void AddCustomFontSource(Uri source)
        {
            if (source is null)
            {
                return;
            }

            LoadGlyphTypefaces(_fontManager.PlatformImpl, source);
        }

        private void LoadGlyphTypefaces(IFontManagerImpl fontManager, Uri source)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = FontFamilyLoader.LoadFontAssets(source);

            foreach (var fontAsset in fontAssets)
            {
                var stream = assetLoader.Open(fontAsset);

                if (fontManager.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                {
                    if (!_glyphTypefaceCache.TryGetValue(glyphTypeface.FamilyName, out var glyphTypefaces))
                    {
                        glyphTypefaces = new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>();

                        if (_glyphTypefaceCache.TryAdd(glyphTypeface.FamilyName, glyphTypefaces))
                        {
                            //Move the user defined system font to the start of the collection
                            _familyNames.Insert(0, glyphTypeface.FamilyName);
                        }
                    }

                    var key = new FontCollectionKey(
                        glyphTypeface.Style,
                        glyphTypeface.Weight,
                        glyphTypeface.Stretch);

                    glyphTypefaces.TryAdd(key, glyphTypeface);
                }
            }
        }
    }
}
