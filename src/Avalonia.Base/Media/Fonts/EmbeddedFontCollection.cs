using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public class EmbeddedFontCollection : FontCollectionBase
    {
        private readonly List<FontFamily> _fontFamilies = new List<FontFamily>(1);

        private readonly Uri _key;

        private readonly Uri _source;

        private IFontManagerImpl? _fontManager;

        public EmbeddedFontCollection(Uri key, Uri source)
        {
            _key = key;

            _source = source;
        }

        public override Uri Key => _key;

        public override FontFamily this[int index] => _fontFamilies[index];

        public override int Count => _fontFamilies.Count;

        public override void Initialize(IFontManagerImpl fontManager)
        {
            _fontManager = fontManager;

            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = FontFamilyLoader.LoadFontAssets(_source);

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
                            _fontFamilies.Add(new FontFamily(_key, glyphTypeface.FamilyName));
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


        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            var key = new FontCollectionKey(style, weight, stretch);

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null)
                {
                    return true;
                }

                if (TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                {
                    if (glyphTypeface is IGlyphTypeface2 glyphTypeface2)
                    {
                        var fontSimulations = FontSimulations.None;

                        if (style != FontStyle.Normal && glyphTypeface2.Style != style)
                        {
                            fontSimulations |= FontSimulations.Oblique;
                        }

                        if ((int)weight >= 600 && glyphTypeface2.Weight != weight)
                        {
                            fontSimulations |= FontSimulations.Bold;
                        }

                        if (fontSimulations != FontSimulations.None && glyphTypeface2.TryGetStream(out var stream))
                        {
                            using (stream)
                            {
                                if(_fontManager is not null && _fontManager.TryCreateGlyphTypeface(stream, fontSimulations, out glyphTypeface) && 
                                    glyphTypefaces.TryAdd(key, glyphTypeface))
                                {
                                    return true;
                                }

                                return false;
                            }
                        }
                    }

                    return true;
                }
            }

            //Try to find a partially matching font
            for (var i = 0; i < Count; i++)
            {
                var fontFamily = _fontFamilies[i];

                if (fontFamily.Name.ToLower(CultureInfo.InvariantCulture).StartsWith(familyName.ToLower(CultureInfo.InvariantCulture)))
                {
                    if (_glyphTypefaceCache.TryGetValue(fontFamily.Name, out glyphTypefaces) &&
                        TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }

            glyphTypeface = null;

            return false;
        }

        public override IEnumerator<FontFamily> GetEnumerator() => _fontFamilies.GetEnumerator();
    }
}
