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
    public abstract class FontCollectionBase : IFontCollection
    {
        private static readonly Comparer<FontFamily> FontFamilyNameComparer =
            Comparer<FontFamily>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        // Make this internal for testing purposes
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>> _glyphTypefaceCache = new();

        private readonly object _fontFamiliesLock = new();
        private volatile FontFamily[] _fontFamilies = Array.Empty<FontFamily>();
        private readonly IFontManagerImpl _fontManagerImpl;
        private readonly IAssetLoader _assetLoader;

        protected FontCollectionBase()
        {
            _fontManagerImpl = AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>();
            _assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
        }

        public abstract Uri Key { get; }

        public int Count => _fontFamilies.Length;

        public FontFamily this[int index] => _fontFamilies[index];

        public virtual bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch,
            string? familyName, CultureInfo? culture, out Typeface match)
        {
            match = default;

            var key = new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch };

            //If a font family is defined we try to find a match inside that family first
            if (familyName != null && _glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (TryGetNearestMatch(glyphTypefaces, key, out var glyphTypeface))
                {
                    if (glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _))
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

                if (TryGetNearestMatch(glyphTypefaces, key, out var glyphTypeface))
                {
                    if (glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _))
                    {
                        var platformTypeface = glyphTypeface.PlatformTypeface;

                        // Found a match
                        match = new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName),
                            platformTypeface.Style,
                            platformTypeface.Weight,
                            platformTypeface.Stretch);

                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool TryCreateSyntheticGlyphTypeface(
            GlyphTypeface glyphTypeface,
            FontStyle style,
            FontWeight weight,
            FontStretch stretch,
            [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface)
        {
            syntheticGlyphTypeface = null;

            //Source family should be present in the cache.
            if (!_glyphTypefaceCache.TryGetValue(glyphTypeface.FamilyName, out var glyphTypefaces))
            {
                return false;
            }

            var key = new FontCollectionKey(style, weight, stretch);

            var currentKey = glyphTypeface.ToFontCollectionKey();
                
            if (currentKey == key)
            {
                return false;
            }

            var fontSimulations = FontSimulations.None;

            if (style != FontStyle.Normal && glyphTypeface.Style != style)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            if ((int)weight >= 600 && glyphTypeface.Weight < weight)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if (fontSimulations != FontSimulations.None && glyphTypeface.PlatformTypeface.TryGetStream(out var stream))
            {
                using (stream)
                {
                    if (_fontManagerImpl.TryCreateGlyphTypeface(stream, fontSimulations, out var platformTypeface))
                    {
                        syntheticGlyphTypeface = new GlyphTypeface(platformTypeface, fontSimulations);

                        //Add the TypographicFamilyName to the cache
                        if (!string.IsNullOrEmpty(glyphTypeface.TypographicFamilyName))
                        {
                            TryAddGlyphTypeface(glyphTypeface.TypographicFamilyName, key, syntheticGlyphTypeface);
                        }

                        foreach (var kvp in glyphTypeface.FamilyNames)
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
                    FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);

            var key = typeface.ToFontCollectionKey();

            return TryGetGlyphTypeface(familyName, key, out glyphTypeface);
        }

        public virtual bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                // Take a snapshot of the entries to avoid issues with concurrent modifications
                var entries = glyphTypefaces.ToArray();

                var typefaces = new Typeface[entries.Length];

                for (var i = 0; i < entries.Length; i++)
                {
                    var key = entries[i].Key;

                    typefaces[i] = new Typeface(new FontFamily(Key + "#" + familyName), key.Style, key.Weight, key.Stretch);
                }

                familyTypefaces = typefaces;

                return true;
            }

            return false;
        }

        public bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
        /// Attempts to add the specified <see cref="GlyphTypeface"/> to the font collection.
        /// </summary>
        /// <remarks>This method checks the <see cref="GlyphTypeface.FamilyName"/> and, if applicable,
        /// the typographic family name and other family names provided by the <see cref="GlyphTypeface"/> interface.
        /// If any of these names can be associated with the glyph typeface, the typeface is added to the collection.
        /// The method ensures that duplicate entries are not added.</remarks>
        /// <param name="glyphTypeface">The glyph typeface to add. Must not be <see langword="null"/> and must have a non-empty <see
        /// cref="GlyphTypeface.FamilyName"/>.</param>
        /// <returns><see langword="true"/> if the glyph typeface was successfully added to the collection; otherwise, <see
        /// langword="false"/>.</returns>
        public bool TryAddGlyphTypeface(GlyphTypeface glyphTypeface)
        {
            var key = glyphTypeface.ToFontCollectionKey();

            return TryAddGlyphTypeface(glyphTypeface, key);
        }

        /// <summary>
        /// Attempts to add the specified glyph typeface to the collection using the provided key.
        /// </summary>
        /// <remarks>The method adds the glyph typeface using both its typographic family name and all
        /// available family names. If the glyph typeface or its family name is invalid, the method returns false and
        /// does not add the typeface.</remarks>
        /// <param name="glyphTypeface">The glyph typeface to add. Cannot be null, and its FamilyName property must not be null or empty.</param>
        /// <param name="key">The key that identifies the font collection to which the glyph typeface will be added.</param>
        /// <returns>true if the glyph typeface was successfully added to the collection; otherwise, false.</returns>
        public bool TryAddGlyphTypeface(GlyphTypeface glyphTypeface, FontCollectionKey key)
        {
            if (glyphTypeface == null || string.IsNullOrEmpty(glyphTypeface.FamilyName))
            {
                return false;
            }

            var result = false;

            //Add the TypographicFamilyName to the cache
            if (!string.IsNullOrEmpty(glyphTypeface.TypographicFamilyName))
            {
                if (TryAddGlyphTypeface(glyphTypeface.TypographicFamilyName, key, glyphTypeface))
                {
                    result = true;
                }
            }

            foreach (var kvp in glyphTypeface.FamilyNames)
            {
                if (TryAddGlyphTypeface(kvp.Value, key, glyphTypeface))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to add a glyph typeface from the specified font stream.
        /// </summary>
        /// <remarks>The method first attempts to create a glyph typeface from the provided font stream.
        /// If successful, it adds the created glyph typeface to the collection.</remarks>
        /// <param name="stream">The font stream containing the font data. The stream must be readable and positioned at the beginning of the
        /// font data.</param>
        /// <param name="glyphTypeface">When this method returns, contains the created <see cref="GlyphTypeface"/> instance if the operation
        /// succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the glyph typeface was successfully created and added; otherwise, <see
        /// langword="false"/>.</returns>
        public bool TryAddGlyphTypeface(Stream stream, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            if (!_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface))
            {
                return false;
            }

            glyphTypeface = new GlyphTypeface(platformTypeface);

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
                        var fontAssets = FontFamilyLoader.LoadFontAssets(source);

                        foreach (var fontAsset in fontAssets)
                        {
                            var stream = _assetLoader.Open(fontAsset);

                            if (!_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface))
                            {
                                continue;
                            }

                            var glyphTypeface = new GlyphTypeface(platformTypeface);

                            var key = glyphTypeface.ToFontCollectionKey();

                            //Add TypographicFamilyName to the cache
                            if (!string.IsNullOrEmpty(glyphTypeface.TypographicFamilyName))
                            {
                                if (TryAddGlyphTypeface(glyphTypeface.TypographicFamilyName, key, glyphTypeface))
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

                            if (_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface))
                            {
                                var glyphTypeface = new GlyphTypeface(platformTypeface);

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

                                    if (_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface))
                                    {
                                        var glyphTypeface = new GlyphTypeface(platformTypeface);

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
            if (fontFamily == null)
            {
                throw new ArgumentNullException(nameof(fontFamily));
            }

            lock (_fontFamiliesLock)
            {
                var current = _fontFamilies;
                int index = Array.BinarySearch(current, fontFamily, FontFamilyNameComparer);

                // If an existing family with the same name is present, do nothing
                if (index >= 0)
                {
                    // BinarySearch found an equal entry, so avoid
                    // allocating a new array and inserting a duplicate.
                    return;
                }

                index = ~index;

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
        /// <param name="glyphTypeface">When this method returns, contains the matching <see cref="GlyphTypeface"/> if a match is found; otherwise,
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching glyph typeface is found; otherwise, <see langword="false"/>.</returns>
        protected bool TryGetGlyphTypeface(string familyName, FontCollectionKey key, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
                    var matchedKey = glyphTypeface.ToFontCollectionKey();

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

                var compare = string.Compare(snapshot[mid].Name, familyName, StringComparison.OrdinalIgnoreCase);

                // If the current name is lexicographically less than the search name, move right
                if (compare < 0)
                {
                    left = mid + 1;
                }
                else if (compare == 0)
                {
                    // Exact match found in snapshot. Use the exact family name for lookup
                    if (_glyphTypefaceCache.TryGetValue(snapshot[mid].Name, out var exactGlyphTypefaces) &&
                        TryGetNearestMatch(exactGlyphTypefaces, key, out glyphTypeface))
                    {
                        return true;
                    }

                    // Exact family present but no matching typeface found.
                    return false;
                }
                else
                {
                    // Only check for prefix when snapshot[mid].Name is > familyName. This
                    // avoids the more expensive StartsWith call for names that are definitely
                    // ordered before the search term.
                    if (snapshot[mid].Name.StartsWith(familyName, StringComparison.OrdinalIgnoreCase))
                    {
                        firstMatch = mid;
                        right = mid - 1; // Continue searching to the left for the first match
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }
            }

            if (firstMatch != -1)
            {
                // Iterate over all consecutive prefix matches
                for (int i = firstMatch; i < snapshot.Length; i++)
                {
                    var fontFamily = snapshot[i];

                    if (!fontFamily.Name.StartsWith(familyName, StringComparison.OrdinalIgnoreCase))
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
        /// Attempts to retrieve the nearest matching <see cref="GlyphTypeface"/> for the specified font key from the
        /// provided collection of glyph typefaces.
        /// </summary>
        /// <remarks>This method attempts to find the best match for the specified font key by considering
        /// various fallback strategies, such as normalizing the font style, stretch, and weight. 
        /// If no suitable match is found, the method will return the first available non-null <see cref="GlyphTypeface"/> from the
        /// collection, if any.</remarks>
        /// <param name="glyphTypefaces">A collection of glyph typefaces, indexed by <see cref="FontCollectionKey"/>.</param>
        /// <param name="key">The <see cref="FontCollectionKey"/> representing the desired font attributes.</param>
        /// <param name="glyphTypeface">When this method returns, contains the <see cref="GlyphTypeface"/> that most closely matches the specified
        /// key, if a match is found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching <see cref="GlyphTypeface"/> is found; otherwise, <see
        /// langword="false"/>.</returns>
        protected bool TryGetNearestMatch(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces, 
            FontCollectionKey key, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
        protected bool TryAddGlyphTypeface(string familyName, FontCollectionKey key, GlyphTypeface? glyphTypeface)
        {
            if (string.IsNullOrEmpty(familyName))
            {
                return false;
            }

            // Check if the family already exists
            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
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

            // Family doesn't exist yet. Create a new dictionary instance and try to install it.
            var newDict = new ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>();

            // GetOrAdd will return the instance that ended up in the dictionary. If it's our
            // newDict instance then we won the race to add the family and should publish it.
            var dict = _glyphTypefaceCache.GetOrAdd(familyName, newDict);

            if (ReferenceEquals(dict, newDict))
            {
                // We successfully installed the dictionary; publish the FontFamily once.
                var fontFamily = new FontFamily(Key + "#" + familyName);

                // Add the font family to the sorted array
                AddFontFamily(fontFamily);
            }

            // Add or compare the glyphTypeface in the resulting dictionary.
            if (dict.TryGetValue(key, out var existingAfter))
            {
                if (ReferenceEquals(existingAfter, glyphTypeface) || (existingAfter is null && glyphTypeface is null))
                {
                    return true;
                }

                return false;
            }

            return dict.TryAdd(key, glyphTypeface);
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
           IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
           FontCollectionKey key,
           [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
            IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
