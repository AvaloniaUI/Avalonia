using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public abstract class FontCollectionBase : IFontCollection
    {
        private static readonly FontCollectionKey s_regularKey =
            new(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);

        private static readonly Comparer<FontFamily> FontFamilyNameComparer =
            Comparer<FontFamily>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        // Make this internal for testing purposes
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>> _glyphTypefaceCache =
            new(StringComparer.OrdinalIgnoreCase);

        // Cache of resolved script/culture fallback family names. A non-null value is the preferred
        // fallback family for that script bucket: a Tier B hint that is still re-checked for coverage
        // per codepoint, and which does NOT by itself suppress a platform call. A null value is a
        // *negative* entry that prevents repeated platform-fallback calls for the same script bucket.
        private readonly ConcurrentDictionary<ScriptFallbackKey, string?> _scriptFallbackCache = new();

        private readonly object _fontFamiliesLock = new();
        private volatile FontFamily[] _fontFamilies = Array.Empty<FontFamily>();
        private readonly IFontManagerImpl _fontManagerImpl;
        private readonly IAssetLoader _assetLoader;

        // Font matching data of each variable face, built on the first request the face's
        // registered keys cannot answer.
        private readonly ConditionalWeakTable<GlyphTypeface, VariableFace> _variableFaces = new();

        // Simulated default instances created to vary a simulated face. Only their varied clones
        // are registered, so the collection keeps the sources to release them on disposal.
        private readonly ConcurrentBag<GlyphTypeface> _variedSyntheticSources = new();

        // Instance families of registered variable faces, keyed by the family name the font's
        // STAT table composes (e.g. "Inter Variable Display"). Their faces are created on
        // the first lookup of the family.
        private readonly ConcurrentDictionary<string, InstanceFamily> _instanceFamilies =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly record struct ScriptFallbackKey(Script Script, string? CultureName);

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
            => TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, Script.Unknown, out match);

        /// <summary>
        /// Character-to-typeface match with an optional shaping-capability constraint. When
        /// <paramref name="shapingScript"/> is a complex script, only candidates that can shape it
        /// (<see cref="GlyphTypeface.CanShapeScript"/>) are considered; <see cref="Script.Unknown"/>
        /// imposes no constraint and is identical to the public overload.
        /// </summary>
        internal bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch,
            string? familyName, CultureInfo? culture, Script shapingScript, out Typeface match)
        {
            match = default;

            var key = new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch };
            var cp = new Codepoint((uint)codepoint);
            var script = cp.Script;
            var refinedScript = FontFallbackScriptHints.RefineWithCulture(cp, culture);
            var scriptKey = new ScriptFallbackKey(refinedScript, culture?.Name);

            // --- Tier A: requested family, coverage-checked, culture-compatible ---
            if (familyName != null &&
                _glyphTypefaceCache.TryGetValue(familyName, out var requestedFamily) &&
                TryGetCoveringMatchForFamily(requestedFamily, key, codepoint, culture, shapingScript, out var requestedGlyphTypeface) &&
                IsCultureCompatible(requestedGlyphTypeface, culture, script))
            {
                match = BuildTypefaceWithSynthesis(requestedGlyphTypeface, familyName, key);
                return true;
            }

            // --- Tier B: cached script/culture resolution ---
            if (FontFallbackScriptHints.IsLocaleSensitive(refinedScript) || culture != null)
            {
                if (_scriptFallbackCache.TryGetValue(scriptKey, out var cachedFamily) &&
                    cachedFamily != null &&
                    !string.Equals(cachedFamily, familyName, StringComparison.OrdinalIgnoreCase) &&
                    _glyphTypefaceCache.TryGetValue(cachedFamily, out var cachedTypefaces) &&
                    TryGetCoveringMatchForFamily(cachedTypefaces, key, codepoint, culture, shapingScript, out var cachedGlyphTypeface))
                {
                    match = BuildTypefaceWithSynthesis(cachedGlyphTypeface, cachedFamily, key);
                    return true;
                }
            }

            // --- Tier C: deterministic cache sweep (non last-resort), culture-scored ---
            if (TryMatchInCache(codepoint, key, familyName, culture, script, refinedScript, shapingScript, isLastResort: false, out var bestGt, out var bestFamilyName))
            {
                // The sweep returns the nearest cached key for the winning family. A codepoint the
                // bucket family can't cover (e.g. the Simplified-only 华 when the bucket is a JP font)
                // lands here, so the exact-key upgrade is needed here too: otherwise a Bold face cached
                // for an earlier run is rendered for a Normal request.
                bestGt = PreferExactKey(bestGt, key, codepoint, culture, shapingScript);

                if (FontFallbackScriptHints.IsLocaleSensitive(refinedScript) || culture != null)
                {
                    _scriptFallbackCache.TryAdd(scriptKey, bestFamilyName);
                }

                match = BuildTypefaceWithSynthesis(bestGt, bestFamilyName, key);
                return true;
            }

            // --- Tier D: platform fallback ---
            // Only a *negative* cache entry (the platform had no font for this script bucket)
            // suppresses a retry. A *positive* entry must not: it records a preferred family for the
            // bucket, but that family may not cover THIS codepoint - e.g. 中 (U+4E2D) resolves to a CJK
            // font that lacks the Simplified-only 华 (U+534E) - and the platform resolves per codepoint,
            // so it may still place a codepoint the bucket's font cannot. Reaching Tier D already means
            // no cached family covered this codepoint.
            // Skipped for shaping-constrained queries: the platform match is not capability-checked,
            // and the caller's unconstrained pass already covers the platform path (so the negative
            // cache stays consistent with unconstrained lookups).
            if (shapingScript == Script.Unknown)
            {
                var hasCacheEntry = _scriptFallbackCache.TryGetValue(scriptKey, out var cachedFamily);
                var platformAlreadyAttempted = hasCacheEntry && cachedFamily is null;

                if (!platformAlreadyAttempted &&
                    TryMatchCharacterFromPlatform(codepoint, key, familyName, culture, out var platformGt))
                {
                    // Keep any existing positive hint (TryAdd won't overwrite); registering the match
                    // lets later lookups for this codepoint be served by Tier C without the platform.
                    _scriptFallbackCache.TryAdd(scriptKey, platformGt.FamilyName);
                    match = BuildTypefaceWithSynthesis(platformGt, null, key);
                    return true;
                }

                // Record a negative only when nothing was cached for the bucket yet, so a positive hint
                // for one codepoint isn't downgraded to a negative by another the platform can't place.
                if (!hasCacheEntry)
                {
                    _scriptFallbackCache.TryAdd(scriptKey, null);
                }
            }

            // --- Tier E: last-resort cache sweep ---
            if (TryMatchInCache(codepoint, key, familyName, culture, script, refinedScript, shapingScript, isLastResort: true, out var lrGt, out var lrFamilyName))
            {
                match = BuildTypefaceWithSynthesis(lrGt, lrFamilyName, key);
                return true;
            }

            return false;
        }

        private bool TryMatchInCache(
            int codepoint,
            FontCollectionKey key,
            string? skipFamilyName,
            CultureInfo? culture,
            Script script,
            Script refinedScript,
            Script shapingScript,
            bool isLastResort,
            [NotNullWhen(true)] out GlyphTypeface? bestGlyphTypeface,
            [NotNullWhen(true)] out string? bestFamilyName)
        {
            bestGlyphTypeface = null;
            bestFamilyName = null;

            // Iterate the sorted family snapshot for deterministic order.
            var snapshot = _fontFamilies;
            var bestScore = int.MinValue;

            for (var i = 0; i < snapshot.Length; i++)
            {
                var familyName = snapshot[i].Name;

                if (skipFamilyName != null && string.Equals(familyName, skipFamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
                {
                    continue;
                }

                if (!TryGetCoveringMatch(glyphTypefaces, key, codepoint, isLastResort, shapingScript, out var candidate))
                {
                    continue;
                }

                var score = ScoreCandidate(candidate, key, culture, script, refinedScript);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestGlyphTypeface = candidate;
                    bestFamilyName = familyName;
                }
            }

            return bestGlyphTypeface != null;
        }

        private static int ScoreCandidate(
            GlyphTypeface candidate,
            FontCollectionKey requestedKey,
            CultureInfo? culture,
            Script script,
            Script refinedScript)
        {
            var score = 0;

            // Exact culture match in the font's name table.
            if (culture != null && candidate.FamilyNames.ContainsKey(culture))
            {
                score += 8;
            }
            else if (culture != null)
            {
                var parent = culture.Parent;

                if (parent != null && parent != CultureInfo.InvariantCulture && candidate.FamilyNames.ContainsKey(parent))
                {
                    score += 4;
                }
            }

            // Self-declared culture coverage via OS/2 codepage bits or meta dlng/slng.
            if (culture != null && FontFallbackScriptHints.IsFontCompatibleWithCulture(candidate, culture))
            {
                score += 4;
            }

            // Font's primary script aligns with the requested script — ask the font directly.
            if (FontFallbackScriptHints.IsLocaleSensitive(refinedScript))
            {
                if (candidate.SupportsScript(refinedScript))
                {
                    score += 2;
                }
                else if (refinedScript != script && candidate.SupportsScript(script))
                {
                    score += 1;
                }
            }

            if (candidate.ToFontCollectionKey().StyleEquals(requestedKey))
            {
                score += 1;
            }

            return score;
        }

        private static bool IsCultureCompatible(GlyphTypeface candidate, CultureInfo? culture, Script script)
        {
            // If no culture or codepoint is locale-insensitive, the candidate is fine.
            if (culture == null || !FontFallbackScriptHints.IsLocaleSensitive(script))
            {
                return true;
            }

            // Positive signal from the font's own self-declaration (OS/2 codepage bits or meta dlng/slng).
            // When the font declares coverage we accept it as compatible regardless of localized names.
            if (FontFallbackScriptHints.IsFontCompatibleWithCulture(candidate, culture))
            {
                return true;
            }

            // If the font has no localized family names at all, treat as compatible (no negative signal).
            if (candidate.FamilyNames.Count == 0)
            {
                return true;
            }

            if (candidate.FamilyNames.ContainsKey(culture))
            {
                return true;
            }

            var parent = culture.Parent;

            if (parent != null && parent != CultureInfo.InvariantCulture && candidate.FamilyNames.ContainsKey(parent))
            {
                return true;
            }

            // The font advertises localized names but not for this culture — reject so Tier C can score.
            return false;
        }

        /// <summary>
        /// Builds the fallback typeface for a face that maps the codepoint. When the face does not
        /// match <paramref name="requestedKey"/>, the key is resolved from that face the way a
        /// family lookup resolves it (named instance, axis value, nearest position, simulation) and
        /// the result is cached under the requested key, which the returned typeface resolves
        /// through. Varied clones and simulated faces share the face's character map, so the result
        /// still maps the codepoint. <paramref name="matchedFamilyName"/> is the family the face was
        /// found in, or <see langword="null"/> when it came from the platform.
        /// </summary>
        private Typeface BuildTypefaceWithSynthesis(GlyphTypeface glyphTypeface, string? matchedFamilyName,
            FontCollectionKey requestedKey)
        {
            // The face of an instance family is named by the root family, which resolves the
            // requested key at the root's own optical size and custom axis values. Staying in the
            // instance family keeps the typeface resolving to the position it was matched at.
            var familyName = matchedFamilyName is not null && _instanceFamilies.ContainsKey(matchedFamilyName) ?
                matchedFamilyName :
                glyphTypeface.FamilyName;

            // An entry already cached under the requested key is what the returned typeface
            // resolves to, so resolving again would only repeat the work.
            if (!glyphTypeface.ToFontCollectionKey().StyleEquals(requestedKey) &&
                !(_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) &&
                  glyphTypefaces.TryGetValue(requestedKey, out var cached) && cached is not null))
            {
                TryAddGlyphTypeface(familyName, requestedKey, ResolveFromFace(glyphTypeface, requestedKey));
            }

            return new Typeface(
                new FontFamily(null, Key.AbsoluteUri + "#" + familyName),
                requestedKey.Style,
                requestedKey.Weight,
                requestedKey.Stretch);
        }

        /// <summary>
        /// Resolves <paramref name="key"/> from <paramref name="glyphTypeface"/> alone: a named
        /// instance or axis position of a variable face first, then the nearest of those
        /// positions, then a simulation of the nearest face. Returns <paramref name="glyphTypeface"/>
        /// when nothing gets closer.
        /// </summary>
        private GlyphTypeface ResolveFromFace(GlyphTypeface glyphTypeface, FontCollectionKey key)
        {
            var nearest = glyphTypeface;

            if (glyphTypeface.VariationAxes.Count > 0 && glyphTypeface.FontSimulations == FontSimulations.None)
            {
                var variableFaces = new List<VariableFace>(1)
                {
                    _variableFaces.GetValue(glyphTypeface, static gt => new VariableFace(gt))
                };

                if (TryGetVariedMatch(variableFaces, key, out var varied))
                {
                    return varied;
                }

                var faces = new Dictionary<FontCollectionKey, GlyphTypeface?>
                {
                    [new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch)] =
                        glyphTypeface
                };

                if (TryGetNearestMatch(CreateVariedCandidates(faces, variableFaces, key), key, out var candidate))
                {
                    nearest = candidate;
                }
            }

            if (!nearest.ToFontCollectionKey().StyleEquals(key) &&
                TryCreateSyntheticGlyphTypeface(nearest, key.Style, key.Weight, key.Stretch, out var synthetic))
            {
                return synthetic;
            }

            return nearest;
        }

        /// <summary>
        /// Hook for platform-backed collections (e.g. <see cref="SystemFontCollection"/>) to consult
        /// the underlying font manager for a fallback typeface. Invoked at most once per
        /// (script-bucket, culture) pair from <see cref="FontCollectionBase.TryMatchCharacter(int, FontStyle, FontWeight, FontStretch, string?, CultureInfo?, out Typeface)"/>.
        /// </summary>
        protected virtual bool TryMatchCharacterFromPlatform(
            int codepoint,
            FontCollectionKey key,
            string? familyName,
            CultureInfo? culture,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;
            return false;
        }

        /// <summary>
        /// Resolves a covering face for a single family at the requested key. Takes the cheap cached
        /// covering match first (an exact-key hit needs nothing more) and only escalates to the exact
        /// key when that match differs in any axis, so a Bold (or Italic, or Condensed) face cached for
        /// one run is not reused for a differently-keyed request of the same family.
        /// </summary>
        private bool TryGetCoveringMatchForFamily(
            ConcurrentDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            int codepoint,
            CultureInfo? culture,
            Script shapingScript,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            if (!TryGetCoveringMatch(glyphTypefaces, key, codepoint, isLastResort: false, shapingScript, out glyphTypeface))
            {
                return false;
            }

            glyphTypeface = PreferExactKey(glyphTypeface, key, codepoint, culture, shapingScript);
            return true;
        }

        /// <summary>
        /// When <paramref name="glyphTypeface"/> differs from the requested <paramref name="key"/> in
        /// any axis (style, weight or stretch), asks the platform for the exact-key face of the same
        /// family - the only source of a key the cache lacks - via <see cref="TryMatchCharacterFromPlatform"/>,
        /// and returns it when it is an exact, shapeable match; otherwise returns the input unchanged.
        /// The platform is consulted only on a mismatch, and a collection without one keeps the
        /// neighbouring match. This guards every key axis, not just weight.
        /// </summary>
        private GlyphTypeface PreferExactKey(
            GlyphTypeface glyphTypeface,
            FontCollectionKey key,
            int codepoint,
            CultureInfo? culture,
            Script shapingScript)
        {
            // The platform's character match, biased by the family already resolved, yields that family
            // at the requested key when it has that face (MatchCharacter covers the codepoint, so no
            // extra coverage check is needed). Accept it only when it is the exact key and can shape.
            if (!glyphTypeface.ToFontCollectionKey().StyleEquals(key) &&
                TryMatchCharacterFromPlatform(codepoint, key, glyphTypeface.FamilyName, culture, out var exact) &&
                exact.ToFontCollectionKey().StyleEquals(key) &&
                CanShape(exact, shapingScript))
            {
                return exact;
            }

            return glyphTypeface;
        }

        /// <summary>
        /// Picks a variant of the family that both is close to the requested key and actually maps
        /// the requested codepoint. Falls back through the existing weight/stretch search but
        /// filters every candidate through the font's character-to-glyph map.
        /// </summary>
        // A candidate satisfies a shaping-capability constraint when it can shape the requested
        // script; Script.Unknown means no constraint (the historical, unconstrained behaviour).
        private static bool CanShape(GlyphTypeface glyphTypeface, Script shapingScript)
            => shapingScript == Script.Unknown || glyphTypeface.CanShapeScript(shapingScript);

        private static bool TryGetCoveringMatch(
            ConcurrentDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            int codepoint,
            bool isLastResort,
            Script shapingScript,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            // Exact key first.
            if (glyphTypefaces.TryGetValue(key, out glyphTypeface) &&
                glyphTypeface != null &&
                glyphTypeface.IsLastResort == isLastResort &&
                glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _) &&
                CanShape(glyphTypeface, shapingScript))
            {
                return true;
            }

            GlyphTypeface? coveringNearest = null;
            int coveringDistance = int.MaxValue;

            var keys = glyphTypefaces.Keys.ToArray();

            Array.Sort(keys);

            foreach (var candidateKey in keys)
            {
                if (!glyphTypefaces.TryGetValue(candidateKey, out var candidate) ||
                    candidate == null ||
                    candidate.IsLastResort != isLastResort)
                {
                    continue;
                }

                if (!candidate.CharacterToGlyphMap.TryGetGlyph(codepoint, out _) ||
                    !CanShape(candidate, shapingScript))
                {
                    continue;
                }

                var distance = KeyDistance(candidateKey, key);

                if (distance < coveringDistance)
                {
                    coveringDistance = distance;
                    coveringNearest = candidate;
                }
            }

            glyphTypeface = coveringNearest;

            return glyphTypeface != null;
        }

        private static int KeyDistance(FontCollectionKey a, FontCollectionKey b)
        {
            var weightDelta = (int)a.Weight - (int)b.Weight;

            if (weightDelta < 0)
            {
                weightDelta = -weightDelta;
            }

            var stretchDelta = (int)a.Stretch - (int)b.Stretch;

            if (stretchDelta < 0)
            {
                stretchDelta = -stretchDelta;
            }

            var styleDelta = a.Style == b.Style ? 0 : 1;

            return weightDelta + stretchDelta * 100 + styleDelta * 10_000;
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
                
            if (currentKey.StyleEquals(key))
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

            if (fontSimulations == FontSimulations.None)
            {
                return false;
            }

            // A synthetic for this key may already be cached under the source family, reached
            // through another of its names or by another thread. Building a second one copies the
            // whole font file through TryGetStream, then loses the slot below to the instance
            // already there, so nothing caches it, nothing disposes it, and its native typeface is
            // never released.
            if (glyphTypefaces.TryGetValue(key, out var cachedGlyphTypeface) &&
                cachedGlyphTypeface is not null &&
                cachedGlyphTypeface.FontSimulations == fontSimulations)
            {
                syntheticGlyphTypeface = cachedGlyphTypeface;

                return true;
            }

            if (glyphTypeface.PlatformTypeface.TryGetStream(out var stream))
            {
                using (stream)
                {
                    if (_fontManagerImpl.TryCreateGlyphTypeface(stream, fontSimulations, out var platformTypeface))
                    {
                        syntheticGlyphTypeface = GlyphTypeface.TryCreate(platformTypeface, fontSimulations);
                        if (syntheticGlyphTypeface is null)
                            return false;

                        // The stream of a face inside a font collection (.ttc) loads the
                        // collection's first face, which may be another family or style (an
                        // oblique "Yu Gothic UI" would come back as "Yu Gothic Medium"). Keep the
                        // unsimulated face rather than cache a different one under its name.
                        if (!IsSameFace(glyphTypeface, syntheticGlyphTypeface))
                        {
                            syntheticGlyphTypeface.Dispose();
                            syntheticGlyphTypeface = null;
                            return false;
                        }

                        // The stream holds the default instance, so a varied face is simulated by
                        // moving the simulated default instance to the same position.
                        if (!glyphTypeface.VariationPosition.IsDefault)
                        {
                            _variedSyntheticSources.Add(syntheticGlyphTypeface);
                            syntheticGlyphTypeface =
                                syntheticGlyphTypeface.WithVariation(glyphTypeface.VariationPosition);
                        }

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

        private static bool IsSameFace(GlyphTypeface expected, GlyphTypeface actual)
        {
            if (expected.GlyphCount != actual.GlyphCount ||
                !string.Equals(expected.FamilyName, actual.FamilyName, StringComparison.Ordinal) ||
                expected.FaceNames.Count != actual.FaceNames.Count)
            {
                return false;
            }

            foreach (var faceName in expected.FaceNames)
            {
                if (!actual.FaceNames.TryGetValue(faceName.Key, out var name) ||
                    !string.Equals(faceName.Value, name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerator<FontFamily> GetEnumerator() => ((IEnumerable<FontFamily>)_fontFamilies).GetEnumerator();

        public virtual bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                    FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);

            var key = typeface.ToFontCollectionKey();

            return TryGetGlyphTypeface(familyName, key, allowNearestMatch: true, out glyphTypeface);
        }

        public virtual bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (TryGetFamilyFaces(familyName, out var glyphTypefaces))
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
            if (!TryGetFamilyFaces(familyName, out var glyphTypefaces))
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

            // FamilyNames holds only the Windows-platform names, so a font that names its family on
            // another platform alone would otherwise not be found under its own family name.
            if (TryAddGlyphTypeface(glyphTypeface.FamilyName, key, glyphTypeface))
            {
                result = true;
            }

            foreach (var kvp in glyphTypeface.FamilyNames)
            {
                if (TryAddGlyphTypeface(kvp.Value, key, glyphTypeface))
                {
                    result = true;
                }
            }

            if (result && RegistersInstanceFamilies)
            {
                AddInstanceFamilies(glyphTypeface);
            }

            return result;
        }

        /// <summary>
        /// Whether registering a variable face also makes the families of its instances
        /// addressable. A collection whose platform enumerates those families itself opts out.
        /// </summary>
        internal virtual bool RegistersInstanceFamilies => true;

        /// <summary>
        /// Registers the instance families of an unsimulated default-instance variable face
        /// without creating their faces; <see cref="MaterializeInstanceFamily"/> does that on the
        /// first lookup of a family.
        /// </summary>
        private void AddInstanceFamilies(GlyphTypeface glyphTypeface)
        {
            if (glyphTypeface.FontSimulations != FontSimulations.None ||
                !glyphTypeface.VariationPosition.IsDefault ||
                glyphTypeface.VariationAxes.Count == 0)
            {
                return;
            }

            foreach (var candidate in GetInstanceFamilies(glyphTypeface))
            {
                var family = _instanceFamilies.GetOrAdd(candidate.FamilyName, static name => new InstanceFamily(name));

                if (family.TryAdd(glyphTypeface, candidate.Position))
                {
                    AddFontFamily(new FontFamily(Key + "#" + family.Name));
                }
            }
        }

        /// <summary>
        /// Returns the families the instances of <paramref name="root"/> belong to under the STAT
        /// naming model, each with one position in it: the default instance when the family holds
        /// it, else a Regular position, else the first. The family sits at that position's optical
        /// size and custom axis values, and font matching reaches the rest of it along the weight,
        /// width and slope axes. The families come from the fvar named instances and from the
        /// values STAT names on the other axes, which may have no named instance (Inter Variable's
        /// "Display" optical size). Families named like <paramref name="root"/> itself are left
        /// out: the root already represents them.
        /// </summary>
        internal static List<(string FamilyName, NormalizedVariationPosition Position)> GetInstanceFamilies(
            GlyphTypeface root)
        {
            var result = new List<(string FamilyName, NormalizedVariationPosition Position)>();
            var ranks = new List<int>();
            var names = new List<string>(1);
            var instances = root.NamedInstances;

            for (var i = 0; i < instances.Count; i++)
            {
                Add(instances[i].Coordinates, root.CreateNormalizedPosition(null, instances[i].Index));
            }

            foreach (var coordinates in VariableFontNaming.GetFamilyPositions(root.StatTable, root.VariationAxes))
            {
                var settings = new List<FontVariation>(coordinates.Count);

                foreach (var coordinate in coordinates)
                {
                    settings.Add(new FontVariation(coordinate.Key, coordinate.Value));
                }

                Add(coordinates, root.CreateNormalizedPosition(new FontVariationSettings(settings)));
            }

            return result;

            void Add(IReadOnlyDictionary<OpenTypeTag, float> coordinates, NormalizedVariationPosition position)
            {
                names.Clear();
                root.GetInstanceFamilyNames(coordinates, names);

                if (names.Count == 0)
                {
                    return;
                }

                var rank = position.IsDefault ? 2 : root.GetProjectedKey(position).StyleEquals(s_regularKey) ? 1 : 0;

                foreach (var name in names)
                {
                    if (IsOwnFamilyName(root, name))
                    {
                        continue;
                    }

                    var index = result.FindIndex(x =>
                        string.Equals(x.FamilyName, name, StringComparison.OrdinalIgnoreCase));

                    if (index < 0)
                    {
                        result.Add((name, position));
                        ranks.Add(rank);
                    }
                    else if (rank > ranks[index])
                    {
                        result[index] = (result[index].FamilyName, position);
                        ranks[index] = rank;
                    }
                }
            }

            static bool IsOwnFamilyName(GlyphTypeface root, string name)
            {
                if (string.Equals(name, root.TypographicFamilyName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, root.FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                foreach (var familyName in root.FamilyNames.Values)
                {
                    if (string.Equals(name, familyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the faces representing the instance family <paramref name="familyName"/>, one
        /// per variable face that has instances in it, creating and registering them under the
        /// family on first use.
        /// </summary>
        internal bool TryGetInstanceFamilyFaces(string familyName,
            [NotNullWhen(true)] out IReadOnlyList<GlyphTypeface>? faces)
        {
            if (!_instanceFamilies.TryGetValue(familyName, out var family))
            {
                faces = null;
                return false;
            }

            faces = family.Materialize(this);
            return faces.Count > 0;
        }

        private void MaterializeInstanceFamily(string familyName)
        {
            if (!_instanceFamilies.IsEmpty &&
                _instanceFamilies.TryGetValue(familyName, out var family) &&
                family.HasPending)
            {
                family.Materialize(this);
            }
        }

        private bool TryGetFamilyFaces(string familyName,
            [NotNullWhen(true)] out ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>? glyphTypefaces)
        {
            MaterializeInstanceFamily(familyName);

            return _glyphTypefaceCache.TryGetValue(familyName, out glyphTypefaces);
        }

        /// <summary>
        /// The variable faces with instances in one STAT-derived family, each at the
        /// position its family sits at.
        /// </summary>
        private sealed class InstanceFamily
        {
            private readonly List<(GlyphTypeface Root, NormalizedVariationPosition Position)> _members = new(1);
            private readonly List<GlyphTypeface> _faces = new(1);
            private volatile bool _hasPending;

            public InstanceFamily(string name) => Name = name;

            public string Name { get; }

            public bool HasPending => _hasPending;

            public bool TryAdd(GlyphTypeface root, NormalizedVariationPosition position)
            {
                lock (_members)
                {
                    foreach (var member in _members)
                    {
                        if (ReferenceEquals(member.Root, root))
                        {
                            return false;
                        }
                    }

                    _members.Add((root, position));
                    _hasPending = true;

                    return true;
                }
            }

            /// <summary>
            /// Creates the faces of members added since the last call and registers them under
            /// the family. The lock is held until they are registered, so a lookup that finds
            /// nothing pending also finds them in the family's cache.
            /// </summary>
            public IReadOnlyList<GlyphTypeface> Materialize(FontCollectionBase collection)
            {
                lock (_members)
                {
                    for (var i = _faces.Count; i < _members.Count; i++)
                    {
                        var (root, position) = _members[i];
                        var face = root.WithVariation(position);

                        _faces.Add(face);
                        collection.TryAddGlyphTypeface(Name, face.ToFontCollectionKey(), face);
                    }

                    _hasPending = false;

                    return _faces.ToArray();
                }
            }
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
            if (!_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface))
            {
                glyphTypeface = null;
                return false;
            }

            glyphTypeface = GlyphTypeface.TryCreate(platformTypeface);
            return glyphTypeface is not null && TryAddGlyphTypeface(glyphTypeface);
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

                            if (!_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface) ||
                                GlyphTypeface.TryCreate(platformTypeface) is not { } glyphTypeface)
                            {
                                continue;
                            }

                            if (TryAddGlyphTypeface(glyphTypeface))
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

                            if (_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface) &&
                                GlyphTypeface.TryCreate(platformTypeface) is { } glyphTypeface &&
                                TryAddGlyphTypeface(glyphTypeface))
                            {
                                result = true;
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

                                    if (_fontManagerImpl.TryCreateGlyphTypeface(stream, FontSimulations.None, out var platformTypeface) &&
                                        GlyphTypeface.TryCreate(platformTypeface) is { } glyphTypeface &&
                                        TryAddGlyphTypeface(glyphTypeface))
                                    {
                                        result = true;
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
        /// <param name="allowNearestMatch">Whether to allow a nearest match (as opposed to only an exact match).</param>
        /// <param name="glyphTypeface">When this method returns, contains the matching <see cref="GlyphTypeface"/> if a match is found; otherwise,
        /// <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching glyph typeface is found; otherwise, <see langword="false"/>.</returns>
        protected bool TryGetGlyphTypeface(
            string familyName,
            FontCollectionKey key,
            bool allowNearestMatch,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            if (TryGetFamilyFaces(familyName, out var glyphTypefaces))
            {
                if (TryGetMatch(glyphTypefaces, key, allowNearestMatch, out glyphTypeface, out var matchKind))
                {
                    var matchedKey = glyphTypeface.ToFontCollectionKey();

                    if (matchKind == MatchKind.VariedFace)
                    {
                        // Register the varied face under the requested key so the next request is
                        // an exact hit instead of another pass over the family's variation space.
                        TryAddGlyphTypeface(familyName, key, glyphTypeface);
                    }
                    else if (matchKind == MatchKind.Nearest && !matchedKey.StyleEquals(key))
                    {
                        if (TryCreateSyntheticGlyphTypeface(glyphTypeface, key.Style, key.Weight, key.Stretch, out var syntheticGlyphTypeface))
                        {
                            glyphTypeface = syntheticGlyphTypeface;
                        }

                        // TryCreateSyntheticGlyphTypeface registers the synthetic only under the
                        // source font's own family names, so a request arriving through a different
                        // name would otherwise miss the cache and re-synthesise on every call,
                        // copying the whole font file each time.
                        TryAddGlyphTypeface(familyName, key, glyphTypeface);
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
                    if (TryGetFamilyFaces(snapshot[mid].Name, out var exactGlyphTypefaces) &&
                        TryGetMatch(exactGlyphTypefaces, key, allowNearestMatch, out glyphTypeface, out _))
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

                    if (TryGetFamilyFaces(fontFamily.Name, out glyphTypefaces) &&
                        TryGetMatch(glyphTypefaces, key, allowNearestMatch, out glyphTypeface, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryGetMatch(
            IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            bool allowNearestMatch,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface,
            out MatchKind matchKind)
        {
            if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface is not null)
            {
                matchKind = MatchKind.Exact;
                return true;
            }

            if (allowNearestMatch)
            {
                // A variable face can reach the requested weight, width or style along its axes,
                // which beats both a nearest static face and a simulation.
                var variableFaces = GetVariableFaces(glyphTypefaces);

                if (variableFaces is not null)
                {
                    if (TryGetVariedMatch(variableFaces, key, out glyphTypeface))
                    {
                        matchKind = MatchKind.VariedFace;
                        return true;
                    }

                    if (TryGetNearestMatch(CreateVariedCandidates(glyphTypefaces, variableFaces, key), key,
                            out glyphTypeface))
                    {
                        matchKind = MatchKind.Nearest;
                        return true;
                    }
                }
                else if (TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                {
                    matchKind = MatchKind.Nearest;
                    return true;
                }
            }

            matchKind = MatchKind.Exact;
            return false;
        }

        private enum MatchKind
        {
            /// <summary>The face is registered under the requested key.</summary>
            Exact,

            /// <summary>A variable face moved along its axes to match the requested key exactly.</summary>
            VariedFace,

            /// <summary>The closest available face; it may need simulation to close the gap.</summary>
            Nearest
        }

        /// <summary>
        /// Returns the unsimulated variable faces registered for a family, or <c>null</c> when the
        /// family has none. A default-instance face represents its whole design space. A varied
        /// clone counts only when the family does not hold its default instance: the family then
        /// sits at that clone's position (an optical-size or other named-instance family), and
        /// matching starts from there. Simulated faces are skipped: axis positions are always taken
        /// from the unsimulated design.
        /// </summary>
        private List<VariableFace>? GetVariableFaces(IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces)
        {
            List<VariableFace>? result = null;
            List<GlyphTypeface>? clones = null;

            foreach (var candidate in glyphTypefaces.Values)
            {
                if (candidate is null ||
                    candidate.VariationAxes.Count == 0 ||
                    candidate.FontSimulations != FontSimulations.None)
                {
                    continue;
                }

                if (!candidate.VariationPosition.IsDefault)
                {
                    (clones ??= new List<GlyphTypeface>(1)).Add(candidate);
                    continue;
                }

                AddFace(candidate);
            }

            if (clones is not null)
            {
                foreach (var clone in clones)
                {
                    // WithVariation(default) returns the clone's source. The clones of one source
                    // registered for a family were resolved from the same face and share its
                    // non-style axes, so one of them is enough.
                    if (!HasSource(clone.WithVariation(default)))
                    {
                        AddFace(clone);
                    }
                }
            }

            return result;

            void AddFace(GlyphTypeface candidate)
            {
                var face = _variableFaces.GetValue(candidate, static gt => new VariableFace(gt));

                result ??= new List<VariableFace>(1);

                if (!result.Contains(face))
                {
                    result.Add(face);
                }
            }

            bool HasSource(GlyphTypeface source)
            {
                if (result is not null)
                {
                    foreach (var face in result)
                    {
                        if (ReferenceEquals(face.Typeface.WithVariation(default), source))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Finds a varied face that matches <paramref name="key"/> exactly: a named instance
        /// first, then a position built from the axis values.
        /// </summary>
        private static bool TryGetVariedMatch(
            List<VariableFace> variableFaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            foreach (var face in variableFaces)
            {
                foreach (var instance in face.Instances)
                {
                    if (instance.Key == key)
                    {
                        glyphTypeface = face.Typeface.WithVariation(instance.Position);
                        return true;
                    }
                }
            }

            foreach (var face in variableFaces)
            {
                if (face.TryCreateAxisPosition(key, out var position))
                {
                    glyphTypeface = face.Typeface.WithVariation(position);
                    return true;
                }
            }

            glyphTypeface = null;
            return false;
        }

        /// <summary>
        /// Builds the candidate set for a nearest match: the family's registered faces plus, for
        /// every variable face, its named instances and the position closest to
        /// <paramref name="key"/> its axis ranges allow. Variation candidates are keyed by the
        /// Weight / Style / Stretch they report, so the regular fallback search finds them.
        /// </summary>
        private static Dictionary<FontCollectionKey, GlyphTypeface?> CreateVariedCandidates(
            IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            List<VariableFace> variableFaces,
            FontCollectionKey key)
        {
            var candidates = new Dictionary<FontCollectionKey, GlyphTypeface?>(glyphTypefaces);

            foreach (var face in variableFaces)
            {
                face.TryCreateAxisPosition(key, out var closest);
                AddCandidate(face.Typeface.WithVariation(closest));

                foreach (var instance in face.Instances)
                {
                    if (!candidates.ContainsKey(instance.Key))
                    {
                        candidates.Add(instance.Key, face.Typeface.WithVariation(instance.Position));
                    }
                }
            }

            return candidates;

            void AddCandidate(GlyphTypeface candidate)
            {
                var candidateKey = new FontCollectionKey(candidate.Style, candidate.Weight, candidate.Stretch);

                if (!candidates.ContainsKey(candidateKey))
                {
                    candidates.Add(candidateKey, candidate);
                }
            }
        }

        /// <summary>
        /// The font matching view of a variable face: its named instances keyed by the
        /// Weight / Style / Stretch they project to, and the ranges of the axes that map onto
        /// those properties. Built once per face. The face may be a varied clone; the axes a style
        /// key does not describe (optical size and custom axes) then stay at the clone's position.
        /// </summary>
        private sealed class VariableFace
        {
            // The oblique angle CSS uses when font-style: oblique names none.
            private const float DefaultObliqueAngle = 14f;

            private readonly FontVariationAxis? _weight;
            private readonly FontVariationAxis? _width;
            private readonly FontVariationAxis? _italic;
            private readonly FontVariationAxis? _slant;

            public VariableFace(GlyphTypeface typeface)
            {
                Typeface = typeface;

                foreach (var axis in typeface.VariationAxes)
                {
                    if (axis.Tag == FvarAxisTags.Weight)
                    {
                        _weight = axis;
                    }
                    else if (axis.Tag == FvarAxisTags.Width)
                    {
                        _width = axis;
                    }
                    else if (axis.Tag == FvarAxisTags.Italic)
                    {
                        _italic = axis;
                    }
                    else if (axis.Tag == FvarAxisTags.Slant)
                    {
                        _slant = axis;
                    }
                }

                var namedInstances = typeface.NamedInstances;
                var instances = new List<(FontCollectionKey, NormalizedVariationPosition)>(namedInstances.Count);

                for (var i = 0; i < namedInstances.Count; i++)
                {
                    var position = typeface.CreateNormalizedPosition(null, namedInstances[i].Index);

                    // An instance at another optical size (or other non-style axis value) is a
                    // different design, not this face at another weight, width or style.
                    if (IsAtFacePosition(typeface, position))
                    {
                        instances.Add((typeface.GetProjectedKey(position), position));
                    }
                }

                Instances = instances.ToArray();
            }

            public GlyphTypeface Typeface { get; }

            public (FontCollectionKey Key, NormalizedVariationPosition Position)[] Instances { get; }

            /// <summary>
            /// Builds the position that brings the face closest to <paramref name="key"/>, each
            /// axis clamped to its range.
            /// </summary>
            /// <returns>
            /// <c>true</c> when the position matches the key exactly; <c>false</c> when an axis
            /// range or a missing axis leaves a gap.
            /// </returns>
            public bool TryCreateAxisPosition(FontCollectionKey key, out NormalizedVariationPosition position)
            {
                var settings = new List<FontVariation>(3);
                var isExact = true;

                if (_weight is { } weightAxis)
                {
                    var weight = Clamp((int)key.Weight, weightAxis);

                    settings.Add(new FontVariation(weightAxis.Tag, weight));
                    isExact &= weight == (int)key.Weight;
                }
                else
                {
                    isExact &= Typeface.Weight == key.Weight;
                }

                if (_width is { } widthAxis)
                {
                    var width = Clamp(GlyphTypeface.GetWidthPercentage(key.Stretch), widthAxis);

                    settings.Add(new FontVariation(widthAxis.Tag, width));
                    isExact &= GlyphTypeface.GetFontStretch(width) == key.Stretch;
                }
                else
                {
                    isExact &= Typeface.Stretch == key.Stretch;
                }

                isExact &= AddStyle(key.Style, settings);

                // Start from the face's own position so the axes the key does not describe keep
                // their values; the settings replace only the style axes.
                position = Typeface.CreateNormalizedPosition(
                    new FontVariationSettings(settings), Typeface.VariationPosition);

                return isExact;
            }

            private static bool IsAtFacePosition(GlyphTypeface typeface, NormalizedVariationPosition position)
            {
                foreach (var axis in typeface.VariationAxes)
                {
                    if (IsStyleAxis(axis.Tag))
                    {
                        continue;
                    }

                    if (position.GetCoordinateOrDefault(axis.Tag) !=
                        typeface.VariationPosition.GetCoordinateOrDefault(axis.Tag))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool IsStyleAxis(OpenTypeTag tag)
                => tag == FvarAxisTags.Weight || tag == FvarAxisTags.Width ||
                   tag == FvarAxisTags.Italic || tag == FvarAxisTags.Slant;

            private bool AddStyle(FontStyle style, List<FontVariation> settings)
            {
                if (style == FontStyle.Normal)
                {
                    if (_italic is { } uprightItalic)
                    {
                        settings.Add(new FontVariation(uprightItalic.Tag, Clamp(0f, uprightItalic)));
                    }

                    if (_slant is { } uprightSlant)
                    {
                        settings.Add(new FontVariation(uprightSlant.Tag, Clamp(0f, uprightSlant)));
                    }

                    return Typeface.Style == FontStyle.Normal || _italic is not null || _slant is not null;
                }

                if (Typeface.Style == style)
                {
                    return true;
                }

                // Italic prefers the italic axis and Oblique the slant axis, but either may stand
                // in for the other, the way CSS falls back between the two styles.
                return style == FontStyle.Italic
                    ? TryAddItalic(settings) || TryAddSlant(settings)
                    : TryAddSlant(settings) || TryAddItalic(settings);
            }

            private bool TryAddItalic(List<FontVariation> settings)
            {
                if (_italic is not { } axis || axis.MaximumValue < 0.5f)
                {
                    return false;
                }

                settings.Add(new FontVariation(axis.Tag, Clamp(1f, axis)));
                return true;
            }

            private bool TryAddSlant(List<FontVariation> settings)
            {
                if (_slant is not { } axis || axis.MinimumValue >= 0f)
                {
                    return false;
                }

                // slnt is counter-clockwise, so a forward slant is a negative angle.
                settings.Add(new FontVariation(axis.Tag, Clamp(-DefaultObliqueAngle, axis)));
                return true;
            }

            private static float Clamp(float value, FontVariationAxis axis)
                => Math.Min(Math.Max(value, axis.MinimumValue), axis.MaximumValue);

            private static int Clamp(int value, FontVariationAxis axis)
                => (int)MathF.Round(Clamp((float)value, axis));
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
            return TryGetNearestMatchCore(glyphTypefaces, key, isLastResort: false, out glyphTypeface)
                || TryGetNearestMatchCore(glyphTypefaces, key, isLastResort: true, out glyphTypeface);
        }

        private static bool TryGetNearestMatchCore(
            IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            bool isLastResort,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            if (glyphTypefaces.TryGetValue(key, out glyphTypeface) &&
                glyphTypeface != null &&
                glyphTypeface.IsLastResort == isLastResort)
            {
                return true;
            }

            if (key.Style != FontStyle.Normal)
            {
                key = key with { Style = FontStyle.Normal };
            }

            if (key.Stretch != FontStretch.Normal)
            {
                if (TryFindStretchFallback(glyphTypefaces, key, isLastResort, out glyphTypeface))
                {
                    return true;
                }

                if (key.Weight != FontWeight.Normal)
                {
                    if (TryFindStretchFallback(glyphTypefaces, key with { Weight = FontWeight.Normal }, isLastResort, out glyphTypeface))
                    {
                        return true;
                    }
                }

                key = key with { Stretch = FontStretch.Normal };
            }

            if (TryFindWeightFallback(glyphTypefaces, key, isLastResort, out glyphTypeface))
            {
                return true;
            }

            if (TryFindStretchFallback(glyphTypefaces, key, isLastResort, out glyphTypeface))
            {
                return true;
            }

            //Take the first glyph typeface we can find.
            foreach (var typeface in glyphTypefaces.Values)
            {
                if (typeface != null && isLastResort == typeface.IsLastResort)
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
        /// <param name="isLastResort">Whether to match last resort fonts.</param>
        /// <param name="glyphTypeface">When this method returns, contains the found glyph typeface with a similar stretch if one exists; otherwise,
        /// null.</param>
        /// <returns>true if a suitable fallback glyph typeface is found; otherwise, false.</returns>
        private static bool TryFindStretchFallback(
           IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
           FontCollectionKey key,
           bool isLastResort,
           [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var stretch = (int)key.Stretch;

            if (stretch < 5)
            {
                for (var i = 0; stretch + i < 9; i++)
                {
                    if (TryGetWithStretch(stretch + i, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; stretch - i > 1; i++)
                {
                    if (TryGetWithStretch(stretch - i, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }

            bool TryGetWithStretch(int effectiveStretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
                => glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)effectiveStretch }, out glyphTypeface) &&
                   glyphTypeface != null &&
                   glyphTypeface.IsLastResort == isLastResort;

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
        /// <param name="isLastResort">Whether to match last resort fonts.</param>
        /// <param name="glyphTypeface">When this method returns, contains the matching glyph typeface if a suitable fallback is found; otherwise,
        /// null.</param>
        /// <returns>true if a fallback glyph typeface matching the requested weight is found; otherwise, false.</returns>
        private static bool TryFindWeightFallback(
            IDictionary<FontCollectionKey, GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            bool isLastResort,
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
                    if (TryGetWithWeight(weight + i, out glyphTypeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (TryGetWithWeight(weight - i, out glyphTypeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights greater than 500, in ascending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (TryGetWithWeight(weight + i, out glyphTypeface))
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
                    if (TryGetWithWeight(weight - i, out glyphTypeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (TryGetWithWeight(weight + i, out glyphTypeface))
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
                    if (TryGetWithWeight(weight + i, out glyphTypeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (TryGetWithWeight(weight - i, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }

            return false;

            bool TryGetWithWeight(int effectiveWeight, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
                => glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)effectiveWeight }, out glyphTypeface) &&
                   glyphTypeface != null &&
                   glyphTypeface.IsLastResort == isLastResort;
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

            foreach (var source in _variedSyntheticSources)
            {
                source.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
