using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public abstract class FontCollectionBase : IFontCollection2
    {
        private static readonly Comparer<FontFamily> FontFamilyNameComparer =
            Comparer<FontFamily>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));

        protected readonly ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>> _glyphTypefaceCache = new();

        private readonly object _fontFamiliesLock = new();
        private volatile FontFamily[] _fontFamilies = Array.Empty<FontFamily>();

        public abstract Uri Key { get; }

        public int Count => _fontFamilies.Length;

        public FontFamily this[int index] => _fontFamilies[index];

        public virtual bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch,
            string? familyName, CultureInfo? culture, out Typeface match)
        {
            match = default;

            //If a font family is defined we try to find a match inside that family first
            if (familyName != null && _glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (TryGetNearestMatch(glyphTypefaces, new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch }, out var glyphTypeface))
                {
                    if (glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                    {
                        match = new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName), style, weight, stretch);

                        return true;
                    }
                }
            }

            //Try to find a match in any font family
            foreach (var pair in _glyphTypefaceCache)
            {
                if (pair.Key == familyName)
                {
                    //We already tried this before
                    continue;
                }

                glyphTypefaces = pair.Value;

                if (TryGetNearestMatch(glyphTypefaces, new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch }, out var glyphTypeface))
                {
                    if (glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                    {
                        // Found a match
                        match = new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName), 
                            glyphTypeface.Style, 
                            glyphTypeface.Weight, 
                            glyphTypeface.Stretch);

                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool TryCreateSyntheticGlyphTypeface(
            IGlyphTypeface glyphTypeface,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch,
            [NotNullWhen(true)] out IGlyphTypeface? syntheticGlyphTypeface)
        {
            syntheticGlyphTypeface = null;

            //Source family should be present in the cache.
            if (!_glyphTypefaceCache.TryGetValue(glyphTypeface.FamilyName, out var glyphTypefaces))
            {
                return false;
            }

            var fontManager = FontManager.Current.PlatformImpl;

            var key = new FontCollectionKey(style, weight, stretch);

            var currentKey =
                new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

            if (currentKey == key)
            {
                return false;
            }

            if (glyphTypeface is not IGlyphTypeface2 glyphTypeface2)
            {
                return false;
            }

            var fontSimulations = FontSimulations.None;

            if (style != FontStyle.Normal && glyphTypeface2.Style != style)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            if ((int)weight >= 600 && glyphTypeface2.Weight < weight)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if (fontSimulations != FontSimulations.None && glyphTypeface2.TryGetStream(out var stream))
            {
                using (stream)
                {
                    if (fontManager.TryCreateGlyphTypeface(stream, fontSimulations, out syntheticGlyphTypeface))
                    {
                        //Add the TypographicFamilyName to the cache
                        if (!string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                        {
                            TryAddGlyphTypeface(glyphTypeface2.TypographicFamilyName, key, syntheticGlyphTypeface);
                        }

                        foreach (var kvp in glyphTypeface2.FamilyNames)
                        {
                            TryAddGlyphTypeface(kvp.Value, key, syntheticGlyphTypeface);
                        }

                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        public IEnumerator<FontFamily> GetEnumerator() => ((IEnumerable<FontFamily>)_fontFamilies).GetEnumerator();

        public virtual bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                    FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);

            style = typeface.Style;

            weight = typeface.Weight;

            stretch = typeface.Stretch;

            var key = new FontCollectionKey(style, weight, stretch);

            return TryGetGlyphTypeface(familyName, key, out glyphTypeface);
        }

        public virtual bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                var typefaces = new Typeface[glyphTypefaces.Count];

                var i = 0;

                foreach (var key in glyphTypefaces.Keys)
                {
                    typefaces[i++] = new Typeface(new FontFamily(Key + "#" + familyName), key.Style, key.Weight, key.Stretch);
                }

                familyTypefaces = typefaces;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the glyph typeface that most closely matches the specified font family name, style,
        /// weight, and stretch.
        /// </summary>
        /// <remarks>This method searches for a glyph typeface in the font collection cache that matches
        /// the specified parameters. If an exact match is not found, fallback mechanisms are applied to find the
        /// closest available match based on the specified style, weight, and stretch. If no suitable match is found,
        /// the method returns <see langword="false"/> and <paramref name="glyphTypeface"/> is set to <see
        /// langword="null"/>.</remarks>
        /// <param name="familyName">The name of the font family to search for. This parameter cannot be <see langword="null"/> or empty.</param>
        /// <param name="style">The desired font style.</param>
        /// <param name="weight">The desired font weight.</param>
        /// <param name="stretch">The desired font stretch.</param>
        /// <param name="glyphTypeface">When this method returns, contains the <see cref="IGlyphTypeface"/> that most closely matches the specified
        /// parameters, if a match is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a matching glyph typeface is found; otherwise, <see langword="false"/>.</returns>
        public bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (!_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                glyphTypeface = null;

                return false;
            }

            var key = new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch };

            return TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface);
        }

        /// <summary>
        /// Attempts to add the specified <see cref="IGlyphTypeface"/> to the font collection.
        /// </summary>
        /// <remarks>This method checks the <see cref="IGlyphTypeface.FamilyName"/> and, if applicable,
        /// the typographic family name and other family names provided by the <see cref="IGlyphTypeface2"/> interface.
        /// If any of these names can be associated with the glyph typeface, the typeface is added to the collection.
        /// The method ensures that duplicate entries are not added.</remarks>
        /// <param name="glyphTypeface">The glyph typeface to add. Must not be <see langword="null"/> and must have a non-empty <see
        /// cref="IGlyphTypeface.FamilyName"/>.</param>
        /// <returns><see langword="true"/> if the glyph typeface was successfully added to the collection; otherwise, <see
        /// langword="false"/>.</returns>
        public bool TryAddGlyphTypeface(IGlyphTypeface glyphTypeface)
        {
            if (glyphTypeface == null || string.IsNullOrEmpty(glyphTypeface.FamilyName))
            {
                return false;
            }

            var key = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

            if (glyphTypeface is IGlyphTypeface2 glyphTypeface2)
            {
                var result = false;

                //Add the TypographicFamilyName to the cache
                if (!string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                {
                    if (TryAddGlyphTypeface(glyphTypeface2.TypographicFamilyName, key, glyphTypeface))
                    {
                        result = true;
                    }
                }

                foreach (var kvp in glyphTypeface2.FamilyNames)
                {
                    if (TryAddGlyphTypeface(kvp.Value, key, glyphTypeface))
                    {
                        result = true;
                    }
                }

                return result;
            }
            else
            {
                return TryAddGlyphTypeface(glyphTypeface.FamilyName, key, glyphTypeface);
            }
        }

        /// <summary>
        /// Attempts to add a glyph typeface from the specified font stream.
        /// </summary>
        /// <remarks>The method first attempts to create a glyph typeface from the provided font stream.
        /// If successful, it adds the created glyph typeface to the collection.</remarks>
        /// <param name="stream">The font stream containing the font data. The stream must be readable and positioned at the beginning of the
        /// font data.</param>
        /// <param name="glyphTypeface">When this method returns, contains the created <see cref="IGlyphTypeface"/> instance if the operation
        /// succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the glyph typeface was successfully created and added; otherwise, <see
        /// langword="false"/>.</returns>
        public bool TryAddGlyphTypeface(Stream stream, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (!FontManager.Current.PlatformImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out glyphTypeface))
            {
                return false;
            }

            return TryAddGlyphTypeface(glyphTypeface);
        }

        /// <summary>
        /// Attempts to add a font source to the font collection.
        /// </summary>
        /// <remarks>This method processes the specified font source and attempts to load all available
        /// fonts from it.  Fonts are added to the collection based on their family name and typographic family name (if
        /// available). If the <paramref name="source"/> is <see langword="null"/>, the method returns <see
        /// langword="false"/>.</remarks>
        /// <param name="source">The URI of the font source to add. This can be a file path, a resource URI, or another valid font source
        /// URI.</param>
        /// <returns><see langword="true"/> if at least one font from the specified source was successfully added to the font
        /// collection;  otherwise, <see langword="false"/>.</returns>
        public bool TryAddFontSource(Uri source)
        {
            if (source is null)
            {
                return false;
            }

            var result = false;

            switch (source.Scheme)
            {
                case "avares":
                case "resm":
                    {
                        var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                        var fontAssets = FontFamilyLoader.LoadFontAssets(source);

                        foreach (var fontAsset in fontAssets)
                        {
                            var stream = assetLoader.Open(fontAsset);

                            if (!FontManager.Current.PlatformImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                            {
                                continue;
                            }

                            var key = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

                            //Add TypographicFamilyName to the cache
                            if (glyphTypeface is IGlyphTypeface2 glyphTypeface2 && !string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                            {
                                if (TryAddGlyphTypeface(glyphTypeface2.TypographicFamilyName, key, glyphTypeface))
                                {
                                    result = true;
                                }
                            }

                            if (TryAddGlyphTypeface(glyphTypeface.FamilyName, key, glyphTypeface))
                            {
                                result = true;
                            }
                        }

                        break;
                    }
                case "file":
                    {
                        // If the path is a file, load the font file directly
                        if (FontFamilyLoader.IsFontSource(source))
                        {
                            if (!File.Exists(source.LocalPath))
                            {
                                return false;
                            }

                            using var stream = File.OpenRead(source.LocalPath);

                            if (FontManager.Current.PlatformImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                            {
                                if (TryAddGlyphTypeface(glyphTypeface))
                                {
                                    result = true;
                                }
                            }
                        }
                        // If the path is a directory, load all font files from that directory
                        else
                        {
                            if (!Directory.Exists(source.LocalPath))
                            {
                                return false;
                            }

                            foreach (var file in Directory.EnumerateFiles(source.LocalPath))
                            {
                                if (FontFamilyLoader.IsFontFile(file))
                                {
                                    using var stream = File.OpenRead(file);

                                    if (FontManager.Current.PlatformImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                                    {
                                        if (TryAddGlyphTypeface(glyphTypeface))
                                        {
                                            result = true;
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    }
                default:
                    //Unsupported scheme
                    return false;
            }

            return result;
        }

        /// <summary>
        /// Inserts the specified font family into the internal collection, maintaining the collection in sorted order
        /// by font family name.
        /// </summary>
        /// <remarks>If a font family with the same name already exists in the collection, the new
        /// instance will be inserted alongside it. The collection remains sorted after insertion.</remarks>
        /// <param name="fontFamily">The font family to add to the collection. Cannot be null.</param>
        protected void AddFontFamily(FontFamily fontFamily)
        {
            if (fontFamily == null) throw new ArgumentNullException(nameof(fontFamily));

            lock (_fontFamiliesLock)
            {
                var current = _fontFamilies;
                int index = Array.BinarySearch(current, fontFamily, FontFamilyNameComparer);

                if (index < 0)
                {
                    index = ~index;
                }

                var copy = new FontFamily[current.Length + 1];

                if (index > 0)
                {
                    Array.Copy(current, 0, copy, 0, index);
                }

                copy[index] = fontFamily;

                if (index < current.Length)
                {
                    Array.Copy(current, index, copy, index + 1, current.Length - index);
                }

                // Publish new array for readers
                _fontFamilies = copy;
            }
        }

        /// <summary>
        /// Attempts to retrieve a glyph typeface that matches the specified font family name and font collection key.
        /// </summary>
        /// <remarks>This method performs a binary search to locate font families with names that match
        /// the specified <paramref name="familyName"/>. If multiple matches are found, the method iterates over them to
        /// find the best match based on the provided <paramref name="key"/>.</remarks>
        /// <param name="familyName">The name of the font family to search for. This parameter is case-insensitive.</param>
        /// <param name="key">The key representing the desired font collection attributes.</param>
        /// <param name="glyphTypeface">When this method returns, contains the matching <see cref="IGlyphTypeface"/> if a match is found; otherwise,
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching glyph typeface is found; otherwise, <see langword="false"/>.</returns>
        protected bool TryGetGlyphTypeface(string familyName, FontCollectionKey key, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null)
                {
                    return true;
                }

                if (TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                {
                    var matchedKey = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

                    if (matchedKey != key)
                    {
                        if (TryCreateSyntheticGlyphTypeface(glyphTypeface, key.Style, key.Weight, key.Stretch, out var syntheticGlyphTypeface))
                        {
                            glyphTypeface = syntheticGlyphTypeface;
                        }
                        else
                        {
                            // Cache the nearest match for future lookups
                            TryAddGlyphTypeface(familyName, key, glyphTypeface);
                        }
                    }

                    return true;
                }
            }

            // Binary search for the first possible prefix match using the snapshot array
            var snapshot = _fontFamilies;
            int left = 0;
            int right = snapshot.Length - 1;
            int firstMatch = -1;

            while (left <= right)
            {
                int mid = (left + right) / 2;

                var compare = string.Compare(snapshot[mid].Name, familyName, StringComparison.InvariantCultureIgnoreCase);

                if (snapshot[mid].Name.StartsWith(familyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    firstMatch = mid;
                    right = mid - 1; // Continue searching to the left for the first match
                }
                else if (compare < 0)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (firstMatch != -1)
            {
                // Iterate over all consecutive prefix matches
                for (int i = firstMatch; i < snapshot.Length; i++)
                {
                    var fontFamily = snapshot[i];

                    if (!fontFamily.Name.StartsWith(familyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }

                    if (_glyphTypefaceCache.TryGetValue(fontFamily.Name, out glyphTypefaces) &&
                        TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the nearest matching <see cref="IGlyphTypeface"/> for the specified font key from the
        /// provided collection of glyph typefaces.
        /// </summary>
        /// <remarks>This method attempts to find the best match for the specified font key by considering
        /// various fallback strategies, such as normalizing the font style, stretch, and weight. If no suitable match is found, the method will return the first available non-null <see cref="IGlyphTypeface"/> from the
        /// collection, if any.</remarks>
        /// <param name="glyphTypefaces">A collection of glyph typefaces, indexed by <see cref="FontCollectionKey"/>.</param>
        /// <param name="key">The <see cref="FontCollectionKey"/> representing the desired font attributes.</param>
        /// <param name="glyphTypeface">When this method returns, contains the <see cref="IGlyphTypeface"/> that most closely matches the specified
        /// key, if a match is found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching <see cref="IGlyphTypeface"/> is found; otherwise, <see
        /// langword="false"/>.</returns>
        protected bool TryGetNearestMatch(IDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces, FontCollectionKey key, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null)
            {
                return true;
            }

            if (key.Style != FontStyle.Normal)
            {
                key = key with { Style = FontStyle.Normal };
            }

            if (key.Stretch != FontStretch.Normal)
            {
                if (TryFindStretchFallback(glyphTypefaces, key, out glyphTypeface))
                {
                    return true;
                }

                if (key.Weight != FontWeight.Normal)
                {
                    if (TryFindStretchFallback(glyphTypefaces, key with { Weight = FontWeight.Normal }, out glyphTypeface))
                    {
                        return true;
                    }
                }

                key = key with { Stretch = FontStretch.Normal };
            }

            if (TryFindWeightFallback(glyphTypefaces, key, out glyphTypeface))
            {
                return true;
            }

            if (TryFindStretchFallback(glyphTypefaces, key, out glyphTypeface))
            {
                return true;
            }

            //Take the first glyph typeface we can find.
            foreach (var typeface in glyphTypefaces.Values)
            {
                if (typeface != null)
                {
                    glyphTypeface = typeface;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to add a glyph typeface to the cache for the specified font family and key.
        /// </summary>
        /// <remarks>If the specified font family does not exist in the cache, it is added along with the
        /// glyph typeface. The method ensures that the font family is inserted in a sorted order within the internal
        /// collection.</remarks>
        /// <param name="familyName">The name of the font family to which the glyph typeface belongs. Cannot be null or empty.</param>
        /// <param name="key">The key associated with the glyph typeface in the cache.</param>
        /// <param name="glyphTypeface">The glyph typeface to add to the cache. Can be null.</param>
        /// <returns><see langword="true"/> if the glyph typeface was successfully added to the cache; otherwise, <see
        /// langword="false"/>.</returns>
        protected bool TryAddGlyphTypeface(string familyName, FontCollectionKey key, IGlyphTypeface? glyphTypeface)
        {
            if (string.IsNullOrEmpty(familyName))
            {
                return false;
            }

            // Get or create the dictionary for the specified family name
            var glyphTypefaces = _glyphTypefaceCache.GetOrAdd(familyName, _ =>
            {
                var dict = new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>();

                // Add the font family to the collection
                var fontFamily = new FontFamily(Key + "#" + familyName);

                AddFontFamily(fontFamily);

                return dict;
            });

            // Try to add the glyph typeface to the family's dictionary
            if (glyphTypefaces.TryGetValue(key, out var existing))
            {
                if (ReferenceEquals(existing, glyphTypeface) || (existing is null && glyphTypeface is null))
                {
                    return true;
                }

                return false;
            }

            return glyphTypefaces.TryAdd(key, glyphTypeface);
        }

        /// <summary>
        /// Attempts to locate a fallback glyph typeface with a similar font stretch to the specified key within the
        /// provided collection.
        /// </summary>
        /// <remarks>The search prioritizes font stretches closest to the requested value, expanding
        /// outward until a match is found or all options are exhausted.</remarks>
        /// <param name="glyphTypefaces">A dictionary mapping font collection keys to their corresponding glyph typefaces. Used as the source for
        /// searching fallback typefaces.</param>
        /// <param name="key">The font collection key specifying the desired font stretch and other font attributes to match.</param>
        /// <param name="glyphTypeface">When this method returns, contains the found glyph typeface with a similar stretch if one exists; otherwise,
        /// null.</param>
        /// <returns>true if a suitable fallback glyph typeface is found; otherwise, false.</returns>
        private static bool TryFindStretchFallback(
           IDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces,
           FontCollectionKey key,
           [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var stretch = (int)key.Stretch;

            if (stretch < 5)
            {
                for (var i = 0; stretch + i < 9; i++)
                {
                    if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; stretch - i > 1; i++)
                {
                    if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to locate a fallback glyph typeface in the specified collection that closely matches the weight of
        /// the provided key.
        /// </summary>
        /// <remarks>The method searches for the closest available weight to the requested value,
        /// considering both lighter and heavier alternatives within the collection. If no exact match is found, it
        /// progressively searches for the nearest available weight in both directions.</remarks>
        /// <param name="glyphTypefaces">A dictionary mapping font collection keys to glyph typeface instances. The method searches this collection
        /// for a suitable fallback.</param>
        /// <param name="key">The font collection key specifying the desired font attributes, including weight, for which a fallback glyph
        /// typeface is sought.</param>
        /// <param name="glyphTypeface">When this method returns, contains the matching glyph typeface if a suitable fallback is found; otherwise,
        /// null.</param>
        /// <returns>true if a fallback glyph typeface matching the requested weight is found; otherwise, false.</returns>
        private static bool TryFindWeightFallback(
            IDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;
            var weight = (int)key.Weight;

            //If the target weight given is between 400 and 500 inclusive
            if (weight >= 400 && weight <= 500)
            {
                //Look for available weights between the target and 500, in ascending order.
                for (var i = 0; weight + i <= 500; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights greater than 500, in ascending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            //If a weight less than 400 is given, look for available weights less than the target, in descending order.
            if (weight < 400)
            {
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            //If a weight greater than 500 is given, look for available weights greater than the target, in ascending order.
            if (weight > 500)
            {
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void IDisposable.Dispose()
        {
            foreach (var glyphTypefaces in _glyphTypefaceCache.Values)
            {
                foreach (var pair in glyphTypefaces)
                {
                    pair.Value?.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
