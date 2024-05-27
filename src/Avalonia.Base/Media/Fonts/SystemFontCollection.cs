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
            _familyNames = fontManager.PlatformImpl.GetInstalledFontFamilyNames().Where(x=> !string.IsNullOrEmpty(x)).ToList();
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

            var typeface = GetImplicitTypeface(new Typeface(familyName, style, weight, stretch), out familyName);

            style = typeface.Style;

            weight = typeface.Weight;

            stretch = typeface.Stretch;

            var key = new FontCollectionKey(style, weight, stretch);

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (glyphTypefaces.TryGetValue(key, out glyphTypeface))
                {
                    return glyphTypeface != null;
                }
            }

            glyphTypefaces ??= _glyphTypefaceCache.GetOrAdd(familyName,
                (_) => new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>());

            //Try to create the glyph typeface via system font manager
            if (!_fontManager.PlatformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch,
                    out glyphTypeface))
            {
                glyphTypefaces.TryAdd(key, null);

                return false;
            }

            var createdKey =
                new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

            //No exact match
            if (createdKey != key)
            {
                //Try to find nearest match if possible
                if (!TryGetNearestMatch(glyphTypefaces, key, out var nearestMatch))
                {
                    glyphTypeface = nearestMatch;
                }
                else
                {
                    //Try to create a synthetic glyph typeface
                    if (TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, out var syntheticGlyphTypeface))
                    {
                        glyphTypeface = syntheticGlyphTypeface;
                    }
                }
            }

            glyphTypefaces.TryAdd(key, glyphTypeface);

            return glyphTypeface != null;
        }

        private bool TryCreateSyntheticGlyphTypeface(IGlyphTypeface glyphTypeface, FontStyle style, FontWeight weight,
            [NotNullWhen(true)] out IGlyphTypeface? syntheticGlyphTypeface)
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
                        _fontManager.PlatformImpl.TryCreateGlyphTypeface(stream, fontSimulations,
                            out syntheticGlyphTypeface);

                        return syntheticGlyphTypeface != null;
                    }
                }
            }

            syntheticGlyphTypeface = null;

            return false;
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
