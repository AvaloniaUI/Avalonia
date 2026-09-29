using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Cmap;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Media.Fonts.Tables.Metrics;
using Avalonia.Media.Fonts.Tables.Name;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a glyph typeface, providing access to font metrics, glyph mappings, and other font-related
    /// properties.
    /// </summary>
    /// <remarks>The <see cref="GlyphTypeface"/> class is used to encapsulate font data, including metrics,
    /// character-to-glyph mappings, and supported OpenType features. It supports platform-specific typefaces and
    /// applies optional font simulations such as bold or oblique. This class is typically used in text rendering and
    /// shaping scenarios.</remarks>
    public sealed class GlyphTypeface
    {
        private static readonly IReadOnlyDictionary<CultureInfo, string> s_emptyStringDictionary =
            new Dictionary<CultureInfo, string>(0);

        private static readonly IPlatformRenderInterface _renderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

        private bool _isDisposed;

        private readonly NameTable? _nameTable;
        private readonly OS2Table _os2Table;
        private readonly CharacterToGlyphMap _cmapTable;
        private readonly HorizontalHeaderTable _hhTable;
        private readonly VerticalHeaderTable _vhTable;
        private readonly HorizontalMetricsTable? _hmTable;
        private readonly VerticalMetricsTable? _vmTable;

        private readonly GlyfTable? _glyfTable;

        // Variation tables (null on static fonts). The fvar table is loaded after the
        // name table so axis / instance names can be resolved during parsing; avar is
        // optional and may be absent even on a variable font.
        private readonly FvarTable? _fvarTable;
        private readonly AvarTable? _avarTable;

        // gvar table — per-glyph point delta deformation. Null when the font has no
        // gvar (static fonts, or variable fonts that only carry metric deltas via
        // HVAR / VVAR / MVAR with no outline variation).
        private readonly GvarTable? _gvarTable;

        // HVAR table — per-glyph advance-width and side-bearing deltas. Null on static
        // fonts and on variable fonts that don't carry HVAR (rare; without it, a
        // wght=900 layout uses default-instance advances and glyphs overlap their
        // neighbors). Inter Variable carries HVAR; most production variable fonts do.
        private readonly HvarTable? _hvarTable;

        // MVAR table — font-wide metric deltas (ascender, descender, line gap, underline,
        // strikeout, etc.) at the active variation point. Loaded once per source typeface
        // and applied to the FontMetrics struct in the clone constructor — clones therefore
        // see Metrics that reflect their variation without paying any per-call cost.
        // Null on static fonts and on variable fonts that don't carry MVAR (which is fine —
        // it just means metrics don't vary across axis space, like Inter Variable's
        // ascent/descent).
        private readonly MvarTable? _mvarTable;

        // VVAR table — VVAR is HVAR's vertical-text counterpart, carrying per-glyph
        // advance-height and top-side-bearing deltas for vertical layout (CJK in
        // tategaki, Mongolian, classical scripts). Null on horizontal-only fonts and
        // on static fonts. Horizontal text never reads it.
        private readonly VvarTable? _vvarTable;

        // STAT table — style names of axis values, read only to group and name the instances of
        // a variable font. Parsed on first access on the source typeface; clones read the source's.
        // Parsing twice under a race is harmless (the results are equal), so no lock is taken.
        private StatTable? _statTable;
        private volatile bool _statTableLoaded;

        // Source typeface for variation clones. Null for default-instance typefaces
        // (the source) — every WithVariation clone points back to its source so all
        // variations of the same font share a single cache and resource owner.
        private readonly GlyphTypeface? _sourceTypeface;

        // Whether this typeface owns its PlatformTypeface (and therefore disposes it).
        // True for default-instance typefaces. For variation clones, depends on whether
        // IPlatformTypeface.WithVariation returned a distinct instance — when the
        // default no-op override is in play, clones share the source's platform
        // typeface and don't own it; when a platform's override actually clones the
        // underlying face the ownership flag flips on automatically.
        private readonly bool _ownsPlatformTypeface;

        // Variation point this typeface is bound to. default(NormalizedVariationPosition)
        // for the source and for static fonts; non-default for variation clones.
        private readonly NormalizedVariationPosition _variationPosition;

        // Precomputed projection of _variationPosition onto fvar's axis order. Allocated
        // once during clone construction and reused by every variation-aware lookup
        // (GetGlyphOutline, the gvar deformer, HVAR advance / LSB queries) instead of
        // re-projecting per call. Null on the source typeface and on static fonts —
        // both have IsDefault == true, and every consumer short-circuits before
        // touching this field.
        private readonly float[]? _activeCoords;

        // Pre-computed per-region scaler arrays for each variation table's
        // ItemVariationStore. Built once at clone construction so per-glyph delta
        // lookups become array indices instead of per-axis F2DOT14 ramps. The active
        // coordinates are fixed for a clone's lifetime, and the regions are fixed
        // for the font's, so the scaler vector is invariant — no point computing it
        // per call. Measured ~4x speedup on a paragraph-size batch advance lookup.
        private readonly float[]? _hvarRegionScalers;
        private readonly float[]? _vvarRegionScalers;

        // Per-source variation cache. Only populated on the source typeface (clones
        // delegate WithVariation through _sourceTypeface so a single cache is shared).
        // Lazy-allocated on first variation request.
        private ConcurrentDictionary<NormalizedVariationPosition, GlyphTypeface>? _variationCache;

        private readonly bool _hasOs2Table;
        private readonly bool _hasHorizontalMetrics;
        private readonly bool _hasVerticalMetrics;
        private readonly string[] _designLanguages;
        private readonly string[] _supportedLanguages;

        private IReadOnlyList<OpenTypeTag>? _supportedFeatures;

        // Guards lazy creation of _textShaperTypeface so concurrent first access creates exactly one
        // shaper (otherwise the losing thread's shaper would leak). _textShaperTypeface is volatile so
        // the lock-free fast-path read in the getter safely observes the fully-published instance.
        private readonly object _textShaperLock = new();
        private volatile ITextShaperTypeface? _textShaperTypeface;

        // A shaper without variation support returns the source's own instance from WithVariation;
        // the clone then shares it and must leave its disposal to the source.
        private bool _ownsTextShaperTypeface = true;

        private UnicodeRange? _supportedUnicodeRange;

        // Lazily-built set of OpenType script tags the font declares in GSUB/GPOS, used by
        // CanShapeScript. Parsing copies the layout tables, so it is deferred until a complex script
        // is actually queried (most text never triggers it). Published via the volatile field.
        private volatile HashSet<OpenTypeTag>? _shapingScriptTags;
        private bool _shapingScriptTagsUnknown;
        private readonly object _shapingScriptTagsLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphTypeface"/> class with the specified platform typeface and
        /// font simulations.
        /// </summary>
        /// <remarks>This constructor initializes the glyph typeface by loading various font tables,
        /// including OS/2, CMAP, and metrics tables, to calculate font metrics and other properties. It also determines
        /// font characteristics such as weight, style, stretch, and family names based on the provided typeface and
        /// font simulations.</remarks>
        /// <param name="typeface">The platform-specific typeface to be used for this <see cref="GlyphTypeface"/> instance. This parameter
        /// cannot be <c>null</c>.</param>
        /// <param name="fontSimulations">The font simulations to apply, such as bold or oblique. The default is <see cref="FontSimulations.None"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown if required font tables (e.g., 'maxp') cannot be loaded.</exception>
        public GlyphTypeface(IPlatformTypeface typeface, FontSimulations fontSimulations = FontSimulations.None)
        {
            PlatformTypeface = typeface;

            // This is the default-instance constructor — the resulting typeface owns its
            // platform typeface and represents the unvaried design point.
            _ownsPlatformTypeface = true;
            _variationPosition = default;

            _hasOs2Table = OS2Table.TryLoad(this, out _os2Table);
            _cmapTable = CmapTable.Load(this);

            if (MetaTable.TryLoad(this, out var metaTable))
            {
                _designLanguages = metaTable.DesignLanguages;
                _supportedLanguages = metaTable.SupportedLanguages;
            }
            else
            {
                _designLanguages = Array.Empty<string>();
                _supportedLanguages = Array.Empty<string>();
            }

            if (_hasOs2Table && _os2Table.Version >= 1)
            {
                CodePageCoverage = (FontCodePageCoverage)(
                    _os2Table.CodePageRange1 | ((ulong)_os2Table.CodePageRange2 << 32));
            }
            else
            {
                CodePageCoverage = FontCodePageCoverage.None;
            }

            var maxpTable = MaxpTable.Load(this);

            GlyphCount = maxpTable.NumGlyphs;

            _hasHorizontalMetrics = HorizontalHeaderTable.TryLoad(this, out _hhTable);

            if (_hasHorizontalMetrics)
            {
                _hmTable = HorizontalMetricsTable.Load(this, _hhTable.NumberOfHMetrics, GlyphCount);
            }

            _hasVerticalMetrics = VerticalHeaderTable.TryLoad(this, out _vhTable);

            if (_hasVerticalMetrics)
            {
                _vmTable = VerticalMetricsTable.Load(this, _vhTable.NumberOfVMetrics, GlyphCount);
            }

            var ascent = 0;
            var descent = 0;
            var lineGap = 0;

            if (_hasOs2Table && (_os2Table.Selection & OS2Table.FontSelectionFlags.USE_TYPO_METRICS) != 0)
            {
                ascent = -_os2Table.TypoAscender;
                descent = -_os2Table.TypoDescender;
                lineGap = _os2Table.TypoLineGap;
            }
            else
            {
                if (_hasHorizontalMetrics)
                {
                    ascent = -_hhTable.Ascender;
                    descent = -_hhTable.Descender;
                    lineGap = _hhTable.LineGap;
                }
            }

            if (_hasOs2Table && (ascent == 0 || descent == 0))
            {
                if (_os2Table.TypoAscender != 0 || _os2Table.TypoDescender != 0)
                {
                    ascent = -_os2Table.TypoAscender;
                    descent = -_os2Table.TypoDescender;
                    lineGap = _os2Table.TypoLineGap;
                }
                else
                {
                    ascent = -_os2Table.WinAscent;
                    descent = _os2Table.WinDescent;
                }
            }

            HeadTable.TryLoad(this, out var headTable);

            if (headTable is not null)
            {
                // Load glyf table once and cache for reuse by GetGlyphOutline
                GlyfTable.TryLoad(this, headTable, maxpTable, out _glyfTable);
            }

            IsLastResort = (headTable is not null && (headTable.Flags & HeadFlags.LastResortFont) != 0) ||
                           _cmapTable.Format == CmapFormat.Format13;

            var postTable = PostTable.Load(this);

            var isFixedPitch = postTable.IsFixedPitch;
            var underlineOffset = postTable.UnderlinePosition;
            var underlineSize = postTable.UnderlineThickness;
            var designEmHeight = GetFontDesignEmHeight(headTable);

            Metrics = new FontMetrics
            {
                DesignEmHeight = designEmHeight,
                Ascent = ascent,
                Descent = descent,
                LineGap = lineGap,
                UnderlinePosition = -underlineOffset,
                UnderlineThickness = underlineSize,
                StrikethroughPosition = _hasOs2Table ? -_os2Table.StrikeoutPosition : 0,
                StrikethroughThickness = _hasOs2Table ? _os2Table.StrikeoutSize : 0,
                IsFixedPitch = isFixedPitch
            };

            FontSimulations = fontSimulations;

            var fontWeight = GetFontWeight(_hasOs2Table ? _os2Table : null, headTable);

            Weight = (fontSimulations & FontSimulations.Bold) != 0 ? FontWeight.Bold : fontWeight;

            var style = GetFontStyle(_hasOs2Table ? _os2Table : null, headTable, postTable);

            Style = (fontSimulations & FontSimulations.Oblique) != 0 ? FontStyle.Italic : style;

            var stretch = GetFontStretch(_hasOs2Table ? _os2Table : null);

            Stretch = stretch;

            _nameTable = NameTable.Load(this);

            FamilyName = _nameTable?.FontFamilyName((ushort)CultureInfo.InvariantCulture.LCID) ?? "unknown";

            TypographicFamilyName = _nameTable?.GetNameById((ushort)CultureInfo.InvariantCulture.LCID, KnownNameIds.TypographicFamilyName) ?? FamilyName;

            TypographicSubfamilyName = _nameTable?.GetNameById(
                (ushort)CultureInfo.InvariantCulture.LCID, KnownNameIds.TypographicSubfamilyName) ?? string.Empty;

            if (_nameTable != null)
            {
                Dictionary<CultureInfo, string>? familyNames = null;
                Dictionary<CultureInfo, string>? faceNames = null;

                foreach (var nameRecord in _nameTable)
                {
                    if (nameRecord.NameID == KnownNameIds.FontFamilyName)
                    {
                        if (nameRecord.Platform != Fonts.Tables.PlatformID.Windows || nameRecord.LanguageID == 0)
                        {
                            continue;
                        }

                        var culture = GetCulture(nameRecord.LanguageID);

                        familyNames ??= new Dictionary<CultureInfo, string>(1);

                        if (!familyNames.ContainsKey(culture))
                        {
                            familyNames[culture] = nameRecord.GetValue();
                        }
                    }

                    if (nameRecord.NameID == KnownNameIds.FontSubfamilyName)
                    {
                        if (nameRecord.Platform != Fonts.Tables.PlatformID.Windows || nameRecord.LanguageID == 0)
                        {
                            continue;
                        }

                        var culture = GetCulture(nameRecord.LanguageID);

                        faceNames ??= new Dictionary<CultureInfo, string>(1);

                        if (!faceNames.ContainsKey(culture))
                        {
                            faceNames[culture] = nameRecord.GetValue();
                        }
                    }
                }

                FamilyNames = familyNames ?? s_emptyStringDictionary;
                FaceNames = faceNames ?? s_emptyStringDictionary;
            }
            else
            {
                FamilyNames = new Dictionary<CultureInfo, string> { { CultureInfo.InvariantCulture, FamilyName } };
                FaceNames = new Dictionary<CultureInfo, string> { { CultureInfo.InvariantCulture, Weight.ToString() } };
            }

            // Variable-font tables. fvar declares the axes and any named instances; avar
            // (optional) carries per-axis segment maps that correct the linear fvar
            // normalization. Both are absent on static fonts — they're read once here so
            // the per-call cost on VariationAxes / CreateNormalizedPosition is just a field
            // access.
            FvarTable.TryLoad(this, _nameTable, out _fvarTable);
            if (_fvarTable is not null)
            {
                AvarTable.TryLoad(this, out _avarTable);

                // gvar provides per-glyph point deltas. Loaded here (after fvar) so the
                // axis count cross-check works; loaded once per typeface so per-call
                // GetGlyphOutline only pays a field-access cost when no variation is
                // active.
                GvarTable.TryLoad(this, _fvarTable.Axes.Length, GlyphCount, out _gvarTable);

                // HVAR provides per-glyph horizontal-advance deltas (and optionally LSB /
                // RSB deltas). Loaded once per typeface; per-call TryGetHorizontalGlyphAdvance
                // pays a field-access + IsDefault check when no variation is active.
                HvarTable.TryLoad(this, _fvarTable.Axes.Length, out _hvarTable);

                // MVAR provides font-wide metric deltas (ascent, descent, line gap,
                // underline, strikeout). Loaded here so clones can apply the deltas to
                // their FontMetrics struct in their own constructor — see the clone ctor.
                MvarTable.TryLoad(this, _fvarTable.Axes.Length, out _mvarTable);

                // VVAR is HVAR for vertical text. Loaded the same way; per-call
                // TryGetVerticalGlyphAdvance pays the same minimal check.
                VvarTable.TryLoad(this, _fvarTable.Axes.Length, out _vvarTable);
            }

            static CultureInfo GetCulture(int lcid)
            {
                if (lcid == ushort.MaxValue)
                {
                    return CultureInfo.InvariantCulture;
                }

                try
                {
                    return CultureInfo.GetCultureInfo(lcid);
                }
                catch (CultureNotFoundException)
                {
                    return CultureInfo.InvariantCulture;
                }
            }
        }

        /// <summary>
        /// Clone constructor for <see cref="WithVariation"/>. Builds a new
        /// <see cref="GlyphTypeface"/> that reference-shares every parsed table with
        /// <paramref name="source"/> but is bound to a different variation point.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Reference-shared (no per-clone allocation): every Tables/* parser instance,
        /// the name records, the family / face name dictionaries, the character map,
        /// and the glyph count. None of these depend on variation — the per-glyph
        /// delta tables (HVAR / VVAR / gvar) are read on demand against the clone's
        /// variation point, and MVAR's font-wide deltas are applied to a fresh
        /// <see cref="FontMetrics"/> struct here at clone time.
        /// </para>
        /// <para>
        /// Per-clone: the platform typeface (cloned via
        /// <see cref="IPlatformTypeface.WithVariation"/> — a no-op when the platform
        /// hasn't overridden it, an actual variation-bound face when it has), the
        /// variation position, the lazy shaper typeface (cleared so the clone
        /// materializes its own variation-aware shaper), and <see cref="Weight"/> /
        /// <see cref="Style"/> / <see cref="Stretch"/>, which are projected from the
        /// <c>wght</c>, <c>ital</c> / <c>slnt</c> and <c>wdth</c> axes of the position.
        /// </para>
        /// </remarks>
        private GlyphTypeface(GlyphTypeface source, IPlatformTypeface platformTypeface, NormalizedVariationPosition variation)
        {
            _sourceTypeface = source;

            // The clone owns its platform typeface iff WithVariation produced a distinct
            // instance. With the default no-op IPlatformTypeface.WithVariation override
            // the source's platform typeface is returned unchanged, so clones share —
            // and skip platform-typeface disposal.
            _ownsPlatformTypeface = !ReferenceEquals(platformTypeface, source.PlatformTypeface);

            PlatformTypeface = platformTypeface;
            _variationPosition = variation;

            // Reference-share all parsed tables.
            _nameTable = source._nameTable;
            _os2Table = source._os2Table;
            _cmapTable = source._cmapTable;
            _hhTable = source._hhTable;
            _vhTable = source._vhTable;
            _hmTable = source._hmTable;
            _vmTable = source._vmTable;
            _glyfTable = source._glyfTable;
            _fvarTable = source._fvarTable;
            _avarTable = source._avarTable;
            _gvarTable = source._gvarTable;
            _hvarTable = source._hvarTable;
            _mvarTable = source._mvarTable;
            _vvarTable = source._vvarTable;

            _hasOs2Table = source._hasOs2Table;
            _hasHorizontalMetrics = source._hasHorizontalMetrics;
            _hasVerticalMetrics = source._hasVerticalMetrics;

            // Face-level coverage metadata — variation-invariant, shared from the source.
            _designLanguages = source._designLanguages;
            _supportedLanguages = source._supportedLanguages;
            CodePageCoverage = source.CodePageCoverage;

            // Shareable face-level metadata.
            FamilyName = source.FamilyName;
            TypographicFamilyName = source.TypographicFamilyName;
            TypographicSubfamilyName = source.TypographicSubfamilyName;
            FamilyNames = source.FamilyNames;
            FaceNames = source.FaceNames;
            GlyphCount = source.GlyphCount;
            IsLastResort = source.IsLastResort;
            FontSimulations = source.FontSimulations;

            // Weight / Style / Stretch describe the face the clone draws, so font matching sees
            // a wght=700 clone as Bold and does not synthesize bold on top of it. Axes the font
            // lacks keep the source's static values.
            Weight = ProjectWeight(source, variation);
            Style = ProjectStyle(source, variation);
            Stretch = ProjectStretch(source, variation);

            // Metrics: apply MVAR deltas to the source's default-instance metrics. The
            // sign conventions follow GlyphTypeface's source constructor — Avalonia's
            // Ascent / Descent / UnderlinePosition / StrikethroughPosition are stored
            // negated relative to their OpenType raw values, so the corresponding MVAR
            // deltas get subtracted; thicknesses and line gap are added as-is.
            Metrics = source._mvarTable is not null
                ? ApplyMvarDeltas(source.Metrics, source._mvarTable, variation, source._fvarTable!)
                : source.Metrics;

            // _supportedFeatures is lazy — let each clone materialize independently.
            // _textShaperTypeface is intentionally null so the getter derives a variation-aware
            // shaper from the source's shaper via ITextShaperTypeface.WithVariation.

            // Project the variation settings onto fvar's axis order once. WithVariation
            // guarantees clones only exist for variable fonts, so _fvarTable is always
            // non-null when we get here, and IsDefault is always false (default settings
            // short-circuit to 'return source' before any clone is built).
            var axes = source._fvarTable!.AxisTags;
            _activeCoords = new float[axes.Length];
            for (var i = 0; i < axes.Length; i++)
            {
                variation.TryGetCoordinate(axes[i], out var v);
                _activeCoords[i] = v;
            }

            // Pre-compute per-region scalers for every ItemVariationStore that's likely
            // to be queried per-glyph. Done once here so HVAR / VVAR per-glyph delta
            // lookups become array indices.
            if (source._hvarTable is not null)
            {
                _hvarRegionScalers = new float[source._hvarTable.Store.RegionCount];
                source._hvarTable.Store.ComputeRegionScalers(_activeCoords, _hvarRegionScalers);
            }
            if (source._vvarTable is not null)
            {
                _vvarRegionScalers = new float[source._vvarTable.Store.RegionCount];
                source._vvarTable.Store.ComputeRegionScalers(_activeCoords, _vvarRegionScalers);
            }
        }

        /// <summary>
        /// Builds a varied <see cref="FontMetrics"/> by applying MVAR deltas to the
        /// source's default-instance metrics at the clone's variation point.
        /// </summary>
        /// <remarks>
        /// Sign conventions match GlyphTypeface's source constructor: <c>hasc</c>,
        /// <c>hdsc</c>, <c>undo</c>, <c>stro</c> deltas are <b>subtracted</b> because
        /// Avalonia stores Ascent / Descent / UnderlinePosition / StrikethroughPosition
        /// negated relative to their OpenType raw values; <c>hlgp</c>, <c>unds</c>,
        /// <c>strs</c> are added as-is. Missing MVAR records on a tag leave that field
        /// at the source's value (i.e. constant across axis space — common for fonts
        /// like Inter that hold ascent/descent fixed across the weight axis).
        /// </remarks>
        private static FontMetrics ApplyMvarDeltas(
            FontMetrics baseMetrics,
            MvarTable mvar,
            NormalizedVariationPosition variation,
            FvarTable fvar)
        {
            // Project the variation onto fvar's axis order. We don't reuse the
            // GlyphTypeface._activeCoords cache here because Metrics is computed inside
            // the clone constructor itself, before _activeCoords has been assigned.
            var axes = fvar.AxisTags;
            Span<float> coords = stackalloc float[axes.Length];
            for (var i = 0; i < axes.Length; i++)
            {
                variation.TryGetCoordinate(axes[i], out var v);
                coords[i] = v;
            }

            var ascent = baseMetrics.Ascent;
            var descent = baseMetrics.Descent;
            var lineGap = baseMetrics.LineGap;
            var underlinePosition = baseMetrics.UnderlinePosition;
            var underlineThickness = baseMetrics.UnderlineThickness;
            var strikethroughPosition = baseMetrics.StrikethroughPosition;
            var strikethroughThickness = baseMetrics.StrikethroughThickness;

            if (mvar.TryGetMetricDelta(MvarTags.HorizontalAscender, coords, out var d))
                ascent -= (int)MathF.Round(d);
            if (mvar.TryGetMetricDelta(MvarTags.HorizontalDescender, coords, out d))
                descent -= (int)MathF.Round(d);
            if (mvar.TryGetMetricDelta(MvarTags.HorizontalLineGap, coords, out d))
                lineGap += (int)MathF.Round(d);
            if (mvar.TryGetMetricDelta(MvarTags.UnderlineOffset, coords, out d))
                underlinePosition -= (int)MathF.Round(d);
            if (mvar.TryGetMetricDelta(MvarTags.UnderlineSize, coords, out d))
                underlineThickness += (int)MathF.Round(d);
            if (mvar.TryGetMetricDelta(MvarTags.StrikeoutOffset, coords, out d))
                strikethroughPosition -= (int)MathF.Round(d);
            if (mvar.TryGetMetricDelta(MvarTags.StrikeoutSize, coords, out d))
                strikethroughThickness += (int)MathF.Round(d);

            return baseMetrics with
            {
                Ascent = ascent,
                Descent = descent,
                LineGap = lineGap,
                UnderlinePosition = underlinePosition,
                UnderlineThickness = underlineThickness,
                StrikethroughPosition = strikethroughPosition,
                StrikethroughThickness = strikethroughThickness,
            };
        }

        /// <summary>
        /// Gets the Weight / Style / Stretch a clone of this typeface at
        /// <paramref name="variation"/> reports, without creating the clone.
        /// </summary>
        internal FontCollectionKey GetProjectedKey(NormalizedVariationPosition variation)
        {
            if (_fvarTable is null || variation.IsDefault)
            {
                return new FontCollectionKey(Style, Weight, Stretch);
            }

            return new FontCollectionKey(
                ProjectStyle(this, variation),
                ProjectWeight(this, variation),
                ProjectStretch(this, variation));
        }

        private static FontWeight ProjectWeight(GlyphTypeface source, NormalizedVariationPosition variation)
        {
            if (!source.TryGetUserAxisValue(variation, FvarAxisTags.Weight, out var wght))
            {
                return source.Weight;
            }

            var weight = (FontWeight)Math.Clamp((int)MathF.Round(wght), 1, 1000);

            // A bold-simulated source reports Bold whatever its design weight; the emboldened
            // outlines stay at least that heavy at every position.
            if ((source.FontSimulations & FontSimulations.Bold) != 0 && weight < FontWeight.Bold)
            {
                weight = FontWeight.Bold;
            }

            return weight;
        }

        private static FontStyle ProjectStyle(GlyphTypeface source, NormalizedVariationPosition variation)
        {
            // An oblique-simulated source is slanted at every position.
            if ((source.FontSimulations & FontSimulations.Oblique) != 0)
            {
                return source.Style;
            }

            var hasItal = source.TryGetUserAxisValue(variation, FvarAxisTags.Italic, out var ital);
            var hasSlnt = source.TryGetUserAxisValue(variation, FvarAxisTags.Slant, out var slnt);

            if (hasItal && ital >= 0.5f)
            {
                return FontStyle.Italic;
            }

            // slnt is measured counter-clockwise, so a conventional forward slant is negative.
            if (hasSlnt && slnt < 0f)
            {
                return FontStyle.Oblique;
            }

            return hasItal || hasSlnt ? FontStyle.Normal : source.Style;
        }

        private static FontStretch ProjectStretch(GlyphTypeface source, NormalizedVariationPosition variation)
        {
            return source.TryGetUserAxisValue(variation, FvarAxisTags.Width, out var wdth)
                ? GetFontStretch(wdth)
                : source.Stretch;
        }

        /// <summary>
        /// Maps a <c>wdth</c> axis value (percent of normal width) to the nearest
        /// <see cref="FontStretch"/>, using the OS/2 <c>usWidthClass</c> percentages.
        /// </summary>
        internal static FontStretch GetFontStretch(float widthPercentage)
        {
            // The thresholds are the midpoints between neighbouring usWidthClass percentages.
            return widthPercentage switch
            {
                < 56.25f => FontStretch.UltraCondensed,
                < 68.75f => FontStretch.ExtraCondensed,
                < 81.25f => FontStretch.Condensed,
                < 93.75f => FontStretch.SemiCondensed,
                < 106.25f => FontStretch.Normal,
                < 118.75f => FontStretch.SemiExpanded,
                < 137.5f => FontStretch.Expanded,
                < 175f => FontStretch.ExtraExpanded,
                _ => FontStretch.UltraExpanded
            };
        }

        /// <summary>
        /// Maps a <see cref="FontStretch"/> to its <c>wdth</c> axis value (percent of normal
        /// width), the OS/2 <c>usWidthClass</c> percentage.
        /// </summary>
        internal static float GetWidthPercentage(FontStretch stretch)
        {
            return stretch switch
            {
                FontStretch.UltraCondensed => 50f,
                FontStretch.ExtraCondensed => 62.5f,
                FontStretch.Condensed => 75f,
                FontStretch.SemiCondensed => 87.5f,
                FontStretch.SemiExpanded => 112.5f,
                FontStretch.Expanded => 125f,
                FontStretch.ExtraExpanded => 150f,
                FontStretch.UltraExpanded => 200f,
                _ => 100f
            };
        }


        /// <summary>
        /// Converts the normalized coordinate of <paramref name="axis"/> in
        /// <paramref name="position"/> back to user space, undoing the avar correction and the
        /// fvar normalization <see cref="CreateNormalizedPosition(FontVariationSettings, int?)"/> applies.
        /// </summary>
        /// <remarks>
        /// Normalized coordinates are quantized to F2Dot14, so the result can differ from the
        /// user value the position was created from by a fraction of a unit.
        /// </remarks>
        /// <returns><c>false</c> when the font has no such axis.</returns>
        internal bool TryGetUserAxisValue(NormalizedVariationPosition position, OpenTypeTag axis, out float userValue)
        {
            userValue = 0f;

            if (_fvarTable is null)
            {
                return false;
            }

            var axes = _fvarTable.Axes;

            for (var i = 0; i < axes.Length; i++)
            {
                var record = axes[i];

                if (record.Tag != axis)
                {
                    continue;
                }

                var normalized = position.GetCoordinateOrDefault(axis);

                if (_avarTable is not null)
                {
                    normalized = _avarTable.Unmap(i, normalized);
                }

                userValue = normalized < 0f
                    ? record.DefaultValue + normalized * (record.DefaultValue - record.MinimumValue)
                    : record.DefaultValue + normalized * (record.MaximumValue - record.DefaultValue);

                return true;
            }

            return false;
        }

        private static ushort GetFontDesignEmHeight(HeadTable? headTable)
        {
            var unitsPerEm = headTable?.UnitsPerEm ?? 0;

            // Bitmap fonts may specify 0 or miss the head table completely.
            // Use 2048 as sensible default (used by most fonts).
            if (unitsPerEm == 0)
                unitsPerEm = 2048;

            return unitsPerEm;
        }

        internal static GlyphTypeface? TryCreate(IPlatformTypeface typeface, FontSimulations fontSimulations = FontSimulations.None)
        {
            try
            {
                return new GlyphTypeface(typeface, fontSimulations);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Fonts)?.Log(
                    null,
                    "Could not create glyph typeface from platform typeface named {FamilyName} with simulations {Simulations}: {Exception}",
                    typeface.FamilyName,
                    fontSimulations,
                    ex);

                return null;
            }
        }

        /// <summary>
        /// Gets the family name of the font.
        /// </summary>
        public string FamilyName { get; }

        /// <summary>
        /// Gets the typographic family name of the font.
        /// </summary>
        public string TypographicFamilyName { get; }

        /// <summary>
        /// Gets the typographic subfamily name (name ID 17) of the font, the style name within
        /// <see cref="TypographicFamilyName"/>. Empty when the font has no such name record, which
        /// means the legacy subfamily name (name ID 2) already names the style.
        /// </summary>
        internal string TypographicSubfamilyName { get; }

        /// <summary>
        /// Gets a read-only mapping of localized culture-specific family names.
        /// </summary>
        /// <remarks>The dictionary contains entries for each supported culture, where the key is a <see
        /// cref="CultureInfo"/> representing the culture, and the value is the corresponding localized family name. The
        /// dictionary may be empty if no family names are available.</remarks>
        public IReadOnlyDictionary<CultureInfo, string> FamilyNames { get; }

        /// <summary>
        /// Gets a read-only mapping of culture-specific face names.
        /// </summary>
        /// <remarks>Each entry in the dictionary maps a <see cref="System.Globalization.CultureInfo"/> to
        /// the corresponding localized face name. The dictionary is empty if no face names are defined.</remarks>
        public IReadOnlyDictionary<CultureInfo, string> FaceNames { get; }

        /// <summary>
        /// Gets a read-only mapping of Unicode character codes to glyph indices for the font.
        /// </summary>
        /// <remarks>This dictionary provides the correspondence between Unicode code points and the
        /// glyphs defined in the font. The mapping can be used to look up the glyph index for a given character when
        /// rendering or processing text. The set of mapped characters depends on the font's supported character
        /// set.</remarks>
        public CharacterToGlyphMap CharacterToGlyphMap => _cmapTable;

        /// <summary>
        /// Gets the font metrics associated with this font.
        /// </summary>
        public FontMetrics Metrics { get; }

        /// <summary>
        /// Gets the font weight.
        /// </summary>
        public FontWeight Weight { get; }

        /// <summary>
        /// Gets the font style.
        /// </summary>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the font stretch.
        /// </summary>
        public FontStretch Stretch { get; }

        /// <summary>
        /// Gets the font simulation settings applied to the <see cref="GlyphTypeface"/>.
        /// </summary>
        public FontSimulations FontSimulations { get; }

        /// <summary>
        /// Gets the number of glyphs held by this font.
        /// </summary>
        public int GlyphCount { get; }

        /// <summary>
        /// Gets the list of OpenType feature tags supported by the font.
        /// </summary>
        /// <remarks>The returned list reflects the features available in the underlying font and is
        /// read-only. The order of features in the list is not guaranteed. This property does not return null; if the
        /// font does not support any features, the list will be empty.</remarks>
        public IReadOnlyList<OpenTypeTag> SupportedFeatures
        {
            get
            {
                if (_supportedFeatures != null)
                {
                    return _supportedFeatures;
                }

                _supportedFeatures = LoadSupportedFeatures();

                return _supportedFeatures;
            }
        }

        /// <summary>
        /// Gets the union of Unicode codepoint ranges covered by the font's character map.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="UnicodeRange"/> is derived from the cmap table and represents every
        /// codepoint for which the font defines a glyph. It is computed lazily on first access and cached
        /// for the lifetime of the <see cref="GlyphTypeface"/>. Prefer this property over enumerating
        /// <see cref="CharacterToGlyphMap"/> when only coverage information (not glyph IDs) is required.
        /// </remarks>
        public UnicodeRange SupportedUnicodeRange
        {
            get
            {
                if (_supportedUnicodeRange.HasValue)
                {
                    return _supportedUnicodeRange.Value;
                }

                _supportedUnicodeRange = BuildSupportedUnicodeRange();

                return _supportedUnicodeRange.Value;
            }
        }

        /// <summary>
        /// Gets the codepage coverage advertised by the font via the OpenType
        /// <c>OS/2.ulCodePageRange1/2</c> bitfields.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="FontCodePageCoverage.None"/> when the font does not ship an OS/2 table
        /// or only supplies an OS/2 version &lt; 1 (where the codepage range fields are not present).
        /// </remarks>
        public FontCodePageCoverage CodePageCoverage { get; }

        /// <summary>
        /// Gets the BCP-47 language tags the font's designer declared as the design target for the
        /// font (the <c>dlng</c> data tag in the OpenType <c>meta</c> table).
        /// </summary>
        /// <remarks>
        /// Returns an empty span when the font does not ship a <c>meta</c> table or omits the
        /// <c>dlng</c> data tag.
        /// </remarks>
        public ReadOnlySpan<string> DesignLanguages => _designLanguages;

        /// <summary>
        /// Gets the BCP-47 language tags the font advertises as supported (the <c>slng</c> data tag
        /// in the OpenType <c>meta</c> table).
        /// </summary>
        /// <remarks>
        /// Returns an empty span when the font does not ship a <c>meta</c> table or omits the
        /// <c>slng</c> data tag.
        /// </remarks>
        public ReadOnlySpan<string> SupportedLanguages => _supportedLanguages;

        /// <summary>
        /// Determines whether this font self-declares coverage for the supplied culture via its
        /// OpenType <c>meta</c> table <c>dlng</c> or <c>slng</c> tag list.
        /// </summary>
        /// <param name="culture">
        /// The culture to check. If <c>null</c> the method returns <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> when one of the declared language tags is a BCP-47 prefix of the culture's
        /// <see cref="CultureInfo.Name"/> (or vice versa, when the font specifies a narrower tag).
        /// </returns>
        /// <remarks>
        /// The match is case-insensitive and BCP-47-aware: the comparison succeeds when one tag is
        /// a prefix of the other up to a subtag boundary (e.g. <c>"ja"</c> matches <c>"ja-JP"</c>,
        /// and <c>"zh-Hans"</c> matches <c>"zh-Hans-CN"</c>). Returns <c>false</c> when the font
        /// declares no design or supported languages.
        /// </remarks>
        public bool DeclaresLanguageCoverage(CultureInfo? culture)
        {
            if (culture == null || culture == CultureInfo.InvariantCulture)
            {
                return false;
            }

            if (_designLanguages.Length == 0 && _supportedLanguages.Length == 0)
            {
                return false;
            }

            var name = culture.Name;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return MatchesAny(_designLanguages, name) || MatchesAny(_supportedLanguages, name);

            static bool MatchesAny(string[] tags, string cultureName)
            {
                foreach (var tag in tags)
                {
                    if (IsBcp47PrefixMatch(tag, cultureName))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsBcp47PrefixMatch(string tag, string cultureName)
        {
            // Either side may be the narrower one — match if one is a subtag-prefix of the other.
            return IsPrefix(tag, cultureName) || IsPrefix(cultureName, tag);

            static bool IsPrefix(string prefix, string candidate)
            {
                if (prefix.Length == 0 || prefix.Length > candidate.Length)
                {
                    return false;
                }

                if (!candidate.AsSpan(0, prefix.Length).Equals(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Either exact match, or the next character is a subtag separator.
                return prefix.Length == candidate.Length
                    || candidate[prefix.Length] == '-'
                    || candidate[prefix.Length] == '_';
            }
        }

        /// <summary>
        /// Determines whether the font advertises support for the supplied Unicode script.
        /// </summary>
        /// <remarks>
        /// When the font ships an OS/2 table the answer is taken from the OS/2 ulUnicodeRange bitfield
        /// (the font's own self-declaration of script coverage). When OS/2 is absent or the bit is unset,
        /// this falls back to probing the cmap with a representative codepoint for the script. Returns
        /// <c>true</c> for scripts that don't have a meaningful per-script signal (for example
        /// <see cref="Script.Common"/> or <see cref="Script.Unknown"/>).
        /// </remarks>
        public bool SupportsScript(Script script)
        {
            // For scripts we don't track per-script, treat the font as supporting them — the cmap
            // is still the final authority via TryGetGlyph at the call site.
            if (!FontFallbackScriptHints.TryGetOS2Bit(script, out var bit) &&
                FontFallbackScriptHints.GetProbeCodepoint(script) == 0)
            {
                return true;
            }

            if (_hasOs2Table && bit >= 0)
            {
                var range = bit switch
                {
                    < 32 => _os2Table.UnicodeRange1,
                    < 64 => _os2Table.UnicodeRange2,
                    < 96 => _os2Table.UnicodeRange3,
                    _ => _os2Table.UnicodeRange4,
                };

                if ((range & (1u << (bit & 31))) != 0)
                {
                    return true;
                }
            }

            var probe = FontFallbackScriptHints.GetProbeCodepoint(script);

            if (probe != 0 && _cmapTable.TryGetGlyph(probe, out _))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this font can <em>shape</em> the specified script, not merely map its
        /// codepoints. Scripts that need OpenType complex shaping (e.g. Arabic joining, Indic
        /// conjuncts) require the font to declare the script in its GSUB/GPOS tables; scripts that
        /// render acceptably from cmap alone always return <c>true</c>. Used by the fallback itemizer
        /// to avoid selecting a font that has the glyphs but cannot form them correctly.
        /// </summary>
        public bool CanShapeScript(Script script)
        {
            if (!FontFallbackScriptHints.TryGetComplexShapingTags(script, out var primary, out var secondary))
            {
                // Simple script: cmap coverage (checked by the caller) is sufficient.
                return true;
            }

            var tags = EnsureShapingScriptTags();

            // A present-but-unparseable GSUB/GPOS leaves capability unknown — don't reject on that
            // basis; cmap remains the authority as it was before.
            if (_shapingScriptTagsUnknown)
            {
                return true;
            }

            return tags.Contains(primary) || tags.Contains(secondary);
        }

        private HashSet<OpenTypeTag> EnsureShapingScriptTags()
        {
            var tags = _shapingScriptTags;

            if (tags is not null)
            {
                return tags;
            }

            lock (_shapingScriptTagsLock)
            {
                if (_shapingScriptTags is not null)
                {
                    return _shapingScriptTags;
                }

                var set = new HashSet<OpenTypeTag>();

                // Set the "unknown" flag before publishing the set so a lock-free reader that sees the
                // volatile set also sees the flag.
                _shapingScriptTagsUnknown = !ScriptListTable.TryReadScriptTags(this, set);

                return _shapingScriptTags = set;
            }
        }

        private UnicodeRange BuildSupportedUnicodeRange()
        {
            var segments = new List<UnicodeRangeSegment>();
            var enumerator = _cmapTable.GetMappedRanges();

            while (enumerator.MoveNext())
            {
                var range = enumerator.Current;
                segments.Add(new UnicodeRangeSegment(range.Start, range.End));
            }

            if (segments.Count == 0)
            {
                return new UnicodeRange(0, -1);
            }

            return new UnicodeRange(segments);
        }

        /// <summary>
        /// Gets the variation point this typeface is bound to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Equals <c>default(NormalizedVariationPosition)</c> for static fonts and for
        /// variable fonts at their default instance. Non-default for typefaces produced
        /// by <see cref="WithVariation"/> on a variable font.
        /// </para>
        /// <para>
        /// These are the normalized coordinates used by gvar / HVAR / MVAR / VVAR
        /// consumers. They are produced from human-readable user-space values
        /// (e.g. <c>wght = 700</c>) by <see cref="CreateNormalizedPosition(FontVariationSettings, int?)"/>.
        /// </para>
        /// </remarks>
        internal NormalizedVariationPosition VariationPosition => _variationPosition;

        /// <summary>
        /// Gets the font's STAT table, or <c>null</c> when the font has none or it is malformed.
        /// </summary>
        internal StatTable? StatTable
        {
            get
            {
                var source = _sourceTypeface ?? this;

                if (!source._statTableLoaded)
                {
                    Fonts.Tables.Variation.StatTable.TryLoad(source, out var statTable);

                    // Publish the table before the flag so a reader that sees the flag sees it.
                    source._statTable = statTable;
                    source._statTableLoaded = true;
                }

                return source._statTable;
            }
        }

        /// <summary>
        /// Composes the family and style name of this variable font's instance at
        /// <paramref name="position"/>, following the STAT naming model described on
        /// <see cref="VariableFontNaming"/>.
        /// </summary>
        /// <remarks>
        /// Normalized coordinates are quantized, so each coordinate is matched back to the STAT or
        /// named-instance value that normalizes to it before the names are selected.
        /// </remarks>
        /// <returns>
        /// <c>false</c> for static fonts, and for a position that is no named instance of a font
        /// without STAT axis values.
        /// </returns>
        internal bool TryGetInstanceNames(NormalizedVariationPosition position, out FontInstanceNames names)
        {
            if (_fvarTable is null)
            {
                names = default;
                return false;
            }

            var axes = _fvarTable.Axes;
            var userCoordinates = new Dictionary<OpenTypeTag, float>(axes.Length);

            for (var i = 0; i < axes.Length; i++)
            {
                var tag = axes[i].Tag;

                TryGetUserAxisValue(position, tag, out var userValue);

                userCoordinates[tag] = SnapToDeclaredValue(i, position.GetCoordinateOrDefault(tag), userValue);
            }

            return TryGetInstanceNames(userCoordinates, out names);
        }

        /// <summary>
        /// Composes the family and style name of this variable font's instance at the user-space
        /// <paramref name="userCoordinates"/>, such as a named instance's coordinates.
        /// </summary>
        /// <returns>
        /// <c>false</c> for static fonts, and for coordinates that are no named instance of a font
        /// without STAT axis values.
        /// </returns>
        internal bool TryGetInstanceNames(
            IReadOnlyDictionary<OpenTypeTag, float> userCoordinates,
            out FontInstanceNames names)
        {
            if (_fvarTable is null)
            {
                names = default;
                return false;
            }

            var nameTable = _nameTable;
            var familyName = string.IsNullOrEmpty(TypographicFamilyName) ? FamilyName : TypographicFamilyName;

            return VariableFontNaming.TryGetInstanceNames(
                StatTable,
                _fvarTable.Axes,
                _fvarTable.Instances,
                familyName,
                userCoordinates,
                nameId => nameTable?.GetNameById((ushort)CultureInfo.InvariantCulture.LCID, nameId),
                out names);
        }

        /// <summary>
        /// Returns the value a STAT axis value or a named instance declares for the fvar axis at
        /// <paramref name="axisIndex"/> when it normalizes to <paramref name="normalized"/>;
        /// otherwise <paramref name="userValue"/>.
        /// </summary>
        private float SnapToDeclaredValue(int axisIndex, float normalized, float userValue)
        {
            var axis = _fvarTable!.Axes[axisIndex];

            if (normalized == 0f)
            {
                return axis.DefaultValue;
            }

            if (StatTable is { } stat)
            {
                foreach (var value in stat.AxisValues)
                {
                    foreach (var record in value.Records)
                    {
                        if (stat.DesignAxes[record.AxisIndex].Tag == axis.Tag &&
                            NormalizesTo(axisIndex, record.Value, normalized))
                        {
                            return record.Value;
                        }
                    }
                }
            }

            foreach (var instance in _fvarTable.Instances)
            {
                if (instance.Coordinates.TryGetValue(axis.Tag, out var value) &&
                    NormalizesTo(axisIndex, value, normalized))
                {
                    return value;
                }
            }

            return userValue;
        }

        // NormalizeAxisValue clamps, so a value outside the axis range would claim the axis ends.
        private bool NormalizesTo(int axisIndex, float userValue, float normalized)
        {
            var axis = _fvarTable!.Axes[axisIndex];

            return userValue >= axis.MinimumValue && userValue <= axis.MaximumValue &&
                   NormalizeAxisValue(axisIndex, userValue) == normalized;
        }

        /// <summary>
        /// Gets the variation axes declared by the font's <c>fvar</c> table, in declaration
        /// order. Empty for static fonts.
        /// </summary>
        /// <remarks>
        /// Axes describe the design dimensions the font exposes — common ones are
        /// <c>wght</c> (weight), <c>wdth</c> (width), <c>opsz</c> (optical size),
        /// <c>ital</c> (italic), and <c>slnt</c> (slant). Each <see cref="FontVariationAxis"/>
        /// carries its minimum / default / maximum user-space values and a human-readable
        /// name. Desired user-space values are expressed as a
        /// <see cref="FontVariationSettings"/>; the renderer normalizes them per font when
        /// the settings are applied.
        /// </remarks>
        public IReadOnlyList<FontVariationAxis> VariationAxes
            => _fvarTable?.Axes ?? (IReadOnlyList<FontVariationAxis>)Array.Empty<FontVariationAxis>();

        /// <summary>
        /// Gets the named variation instances declared by the font's <c>fvar</c> table, in
        /// declaration order. Empty for static fonts.
        /// </summary>
        /// <remarks>
        /// Named instances are pre-defined points in variation space the font designer has
        /// labeled (e.g. "SemiBold" at <c>wght=600</c>). Pass an instance's
        /// <see cref="FontVariationInstance.Index"/> to
        /// <see cref="CreateNormalizedPosition(FontVariationSettings, int?)"/> as a shorthand for "give me the
        /// position of this preset".
        /// </remarks>
        public IReadOnlyList<FontVariationInstance> NamedInstances
            => _fvarTable?.Instances ?? (IReadOnlyList<FontVariationInstance>)Array.Empty<FontVariationInstance>();

        /// <summary>
        /// Gets the platform-specific typeface associated with this font.
        /// </summary>
        public IPlatformTypeface PlatformTypeface { get; }

        /// <summary>
        /// Gets the typeface information used by the text shaper for this font.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned typeface is created on demand and cached for subsequent accesses.
        /// This property is typically used by text rendering components that require
        /// low-level font shaping details.
        /// </para>
        /// <para>
        /// For variation clones, the shaper is derived from the source's shaper via
        /// <see cref="ITextShaperTypeface.WithVariation"/> so face-level state (HarfBuzz
        /// <c>hb_face_t</c>, parsed shaping tables) stays shared. The HarfBuzz shaper
        /// returns a distinct shaping font carrying the clone's normalized coordinates,
        /// which the clone owns and releases on <see cref="Dispose()"/>. A shaper without
        /// variation support returns the source's instance, and the clone is shaped at
        /// the default instance.
        /// </para>
        /// </remarks>
        public ITextShaperTypeface TextShaperTypeface
        {
            get
            {
                var shaper = _textShaperTypeface;
                if (shaper != null)
                {
                    return shaper;
                }

                lock (_textShaperLock)
                {
                    if (_textShaperTypeface != null)
                    {
                        return _textShaperTypeface;
                    }

                    if (_sourceTypeface is not null)
                    {
                        var sourceShaper = _sourceTypeface.TextShaperTypeface;
                        var variedShaper = sourceShaper.WithVariation(_variationPosition);

                        _ownsTextShaperTypeface = !ReferenceEquals(variedShaper, sourceShaper);
                        _textShaperTypeface = variedShaper;
                    }
                    else
                    {
                        var textShaper = AvaloniaLocator.Current.GetRequiredService<ITextShaperImpl>();
                        _textShaperTypeface = textShaper.CreateTypeface(this);
                    }

                    return _textShaperTypeface;
                }
            }
        }

        /// <summary>
        /// Gets whether the font should be used as a last resort, if no other fonts matched.
        /// </summary>
        internal bool IsLastResort { get; }

        /// <summary>
        /// Attempts to retrieve the horizontal advance width for the specified glyph.
        /// </summary>
        /// <remarks>Returns false if horizontal metrics are not available or if the specified glyph is
        /// not present in the metrics table.</remarks>
        /// <param name="glyphIndex">The identifier of the glyph for which to obtain the horizontal advance width.</param>
        /// <param name="advance">When this method returns, contains the horizontal advance width of the glyph if found; otherwise, zero. This
        /// parameter is passed uninitialized.</param>
        /// <returns>true if the horizontal advance width was successfully retrieved; otherwise, false.</returns>
        public bool TryGetHorizontalGlyphAdvance(ushort glyphIndex, out ushort advance)
        {
            advance = default;

            if (!_hasHorizontalMetrics || _hmTable is null)
            {
                return false;
            }

            if (!_hmTable.TryGetAdvance(glyphIndex, out advance))
            {
                return false;
            }

            // HVAR: variation-aware advance widths. Without this, a bolder glyph keeps
            // its default-instance advance and overlaps the next slot. The
            // _hvarRegionScalers null check is the fast path that lets static-font and
            // default-instance callers pay nothing beyond a field access.
            if (_hvarTable is not null && _hvarRegionScalers is not null)
            {
                if (_hvarTable.TryGetAdvanceDeltaWithScalers(glyphIndex, _hvarRegionScalers, out var delta))
                {
                    var adjusted = advance + (int)MathF.Round(delta);
                    advance = adjusted < 0 ? (ushort)0 : (ushort)Math.Min(adjusted, ushort.MaxValue);
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve horizontal advance widths for multiple glyphs in a single operation.
        /// </summary>
        /// <remarks>This method is significantly more efficient than calling <see cref="TryGetHorizontalGlyphAdvance"/>
        /// multiple times as it minimizes memory access overhead and exploits data locality. This is the preferred method
        /// for batch glyph metrics retrieval in text layout and rendering scenarios. Returns false if horizontal metrics
        /// are not available.</remarks>
        /// <param name="glyphIndices">Read-only span of glyph identifiers for which to retrieve advance widths.</param>
        /// <param name="advances">Output span to write the advance widths. Must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <returns>true if horizontal metrics are available and all advances were successfully retrieved; otherwise, false.</returns>
        public bool TryGetHorizontalGlyphAdvances(ReadOnlySpan<ushort> glyphIndices, Span<ushort> advances)
        {
            if (!_hasHorizontalMetrics || _hmTable is null)
            {
                return false;
            }

            // Fast path: no variation. Dispatch to the plain hmtx batch reader, which
            // never touches HVAR.
            if (_hvarTable is null || _hvarRegionScalers is null)
            {
                return _hmTable.TryGetAdvances(glyphIndices, advances);
            }

            // Variation path: hand the cached region scalers + HVAR table to the fused
            // single-pass loop inside HorizontalMetricsTable.TryGetAdvances.
            return _hmTable.TryGetAdvances(glyphIndices, advances, _hvarTable, _hvarRegionScalers);
        }

        /// <summary>
        /// Attempts to retrieve the vertical advance height for the specified glyph.
        /// </summary>
        /// <remarks>Returns false if vertical metrics are not available (the font has no
        /// <c>vmtx</c> table — the common case for Latin fonts) or if the specified glyph
        /// is not present in the metrics table.</remarks>
        /// <param name="glyphIndex">The identifier of the glyph for which to obtain the vertical advance height.</param>
        /// <param name="advance">When this method returns, contains the vertical advance height of the glyph if found; otherwise, zero. This
        /// parameter is passed uninitialized.</param>
        /// <returns>true if the vertical advance height was successfully retrieved; otherwise, false.</returns>
        public bool TryGetVerticalGlyphAdvance(ushort glyphIndex, out ushort advance)
        {
            advance = default;

            if (!_hasVerticalMetrics || _vmTable is null)
            {
                return false;
            }

            if (!_vmTable.TryGetAdvance(glyphIndex, out advance))
            {
                return false;
            }

            // VVAR: variation-aware advance heights, mirroring the HVAR adjustment in
            // TryGetHorizontalGlyphAdvance. Without it a varied clone returns default-instance
            // heights from this advance-only path while TryGetGlyphMetrics applies VVAR — an
            // asymmetry that mis-positions vertical layout at varied instances. The null check
            // keeps static-font and default-instance callers on the zero-cost path.
            if (_vvarTable is not null && _activeCoords is not null)
            {
                if (_vvarTable.TryGetAdvanceHeightDelta(glyphIndex, _activeCoords, out var delta) && delta != 0f)
                {
                    var adjusted = advance + (int)MathF.Round(delta);
                    advance = adjusted < 0 ? (ushort)0 : (ushort)Math.Min(adjusted, ushort.MaxValue);
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve vertical advance heights for multiple glyphs in a single operation.
        /// </summary>
        /// <remarks>This method is significantly more efficient than calling <see cref="TryGetVerticalGlyphAdvance"/>
        /// multiple times as it minimizes memory access overhead and exploits data locality. This is the preferred method
        /// for batch vertical-layout scenarios (CJK, Mongolian). Returns false if vertical metrics
        /// are not available.</remarks>
        /// <param name="glyphIndices">Read-only span of glyph identifiers for which to retrieve advance heights.</param>
        /// <param name="advances">Output span to write the advance heights. Must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <returns>true if vertical metrics are available and all advances were successfully retrieved; otherwise, false.</returns>
        public bool TryGetVerticalGlyphAdvances(ReadOnlySpan<ushort> glyphIndices, Span<ushort> advances)
        {
            if (!_hasVerticalMetrics || _vmTable is null)
            {
                return false;
            }

            // Fast path: no variation. Dispatch to the plain vmtx batch reader, which never
            // touches VVAR.
            if (_vvarTable is null || _activeCoords is null)
            {
                return _vmTable.TryGetAdvances(glyphIndices, advances);
            }

            // Variation path: hand the cached active coords + VVAR table to the fused
            // single-pass loop inside VerticalMetricsTable.TryGetAdvances (mirrors the
            // horizontal path above).
            return _vmTable.TryGetAdvances(glyphIndices, advances, _vvarTable, _activeCoords);
        }

        /// <summary>
        /// Attempts to retrieve the metrics for the specified glyph.
        /// </summary>
        /// <remarks>This method returns metrics only if horizontal or vertical metrics are available for
        /// the specified glyph. If neither is available, the method returns false and the output parameter is set to
        /// its default value.</remarks>
        /// <param name="glyph">The identifier of the glyph for which to obtain metrics.</param>
        /// <param name="metrics">When this method returns, contains the metrics for the specified glyph if found; otherwise, contains the
        /// default value.</param>
        /// <returns>true if metrics for the specified glyph are available; otherwise, false.</returns>
        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = default;

            HorizontalGlyphMetric hMetric = default;
            VerticalGlyphMetric vMetric = default;

            var hasHorizontal = false;
            var hasVertical = false;

            if (_hasHorizontalMetrics && _hmTable != null)
            {
                hasHorizontal = _hmTable.TryGetMetrics(glyph, out hMetric);
            }

            if (_hasVerticalMetrics && _vmTable != null)
            {
                hasVertical = _vmTable.TryGetMetrics(glyph, out vMetric);
            }

            short xMin = 0, yMin = 0, xMax = 0, yMax = 0;
            var hasBounds = _glyfTable != null
                && _glyfTable.TryGetGlyphBounds(glyph, out xMin, out yMin, out xMax, out yMax);

            if (!hasHorizontal && !hasVertical && !hasBounds)
            {
                return false;
            }

            var advanceWidth = hMetric.AdvanceWidth;
            var leftSideBearing = hMetric.LeftSideBearing;
            var advanceHeight = vMetric.AdvanceHeight;
            var topSideBearing = vMetric.TopSideBearing;

            // HVAR adjusts advance width (and optionally LSB) at the active variation
            // point. Without it, varied text laid out via these metrics overlaps.
            if (hasHorizontal && _hvarTable is not null && _hvarRegionScalers is not null)
            {
                if (_hvarTable.TryGetAdvanceDeltaWithScalers(glyph, _hvarRegionScalers, out var advDelta) && advDelta != 0f)
                {
                    var adjusted = advanceWidth + (int)MathF.Round(advDelta);
                    advanceWidth = adjusted < 0 ? (ushort)0 : (ushort)Math.Min(adjusted, ushort.MaxValue);
                }

                if (_hvarTable.TryGetLeftSideBearingDeltaWithScalers(glyph, _hvarRegionScalers, out var lsbDelta) && lsbDelta != 0f)
                {
                    var adjusted = leftSideBearing + (int)MathF.Round(lsbDelta);
                    leftSideBearing = (short)Math.Clamp(adjusted, short.MinValue, short.MaxValue);
                }
            }

            // VVAR mirrors HVAR for vertical metrics. Only fires for fonts that actually
            // ship a VVAR table (most horizontal-text fonts don't); _vvarTable stays null
            // otherwise and we keep the unvaried vmtx values.
            if (hasVertical && _vvarTable is not null && _vvarRegionScalers is not null)
            {
                if (_vvarTable.TryGetAdvanceHeightDeltaWithScalers(glyph, _vvarRegionScalers, out var advDelta) && advDelta != 0f)
                {
                    var adjusted = advanceHeight + (int)MathF.Round(advDelta);
                    advanceHeight = adjusted < 0 ? (ushort)0 : (ushort)Math.Min(adjusted, ushort.MaxValue);
                }

                if (_vvarTable.TryGetTopSideBearingDeltaWithScalers(glyph, _vvarRegionScalers, out var tsbDelta) && tsbDelta != 0f)
                {
                    var adjusted = topSideBearing + (int)MathF.Round(tsbDelta);
                    topSideBearing = (short)Math.Clamp(adjusted, short.MinValue, short.MaxValue);
                }
            }

            // Funnel the raw header values through GlyphBounds so the ink extent is computed
            // (and clamped to non-negative) the same way as the batch path below — a malformed
            // header with xMax < xMin must not wrap when narrowed to the ushort Width/Height.
            var box = new GlyphBounds(xMin, yMin, xMax, yMax);

            metrics = new GlyphMetrics
            {
                // Bounding box (ink extent) from the glyf header; side bearings fall back
                // to hmtx/vmtx (HVAR/VVAR-adjusted) when the glyph has no outline data.
                XBearing = hasBounds ? box.XMin : (hasHorizontal ? leftSideBearing : (short)0),
                YBearing = hasBounds ? box.YMax : (hasVertical ? topSideBearing : (short)0),
                Width = hasBounds ? (ushort)box.Width : (ushort)0,
                Height = hasBounds ? (ushort)box.Height : (ushort)0,
                // Advances come from the metrics tables, with HVAR/VVAR applied.
                AdvanceWidth = hasHorizontal ? advanceWidth : (ushort)0,
                AdvanceHeight = hasVertical ? advanceHeight : (ushort)0,
            };

            return true;
        }

        /// <summary>
        /// Attempts to retrieve glyph metrics for multiple glyphs in a single operation.
        /// </summary>
        /// <remarks>This method is significantly more efficient than calling <see cref="TryGetGlyphMetrics(ushort, out GlyphMetrics)"/>
        /// multiple times as it minimizes memory access overhead and exploits data locality. This is the preferred
        /// method for batch glyph metrics retrieval in text layout and rendering scenarios. Returns false if neither
        /// horizontal nor vertical metrics are available.</remarks>
        /// <param name="glyphIndices">Read-only span of glyph identifiers for which to retrieve metrics.</param>
        /// <param name="metrics">Output span to write the glyph metrics. Must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <returns>true if metrics are available and all were successfully retrieved; otherwise, false.</returns>
        public bool TryGetGlyphMetrics(ReadOnlySpan<ushort> glyphIndices, Span<GlyphMetrics> metrics)
        {
            if (metrics.Length < glyphIndices.Length)
            {
                throw new ArgumentException("Output span must be at least as long as input span", nameof(metrics));
            }

            if (!_hasHorizontalMetrics && !_hasVerticalMetrics)
            {
                return false;
            }

            // Size each temporary buffer to zero (a free, empty stackalloc) when its source
            // table is absent, so a font without hmtx / vmtx never pays for a buffer that is
            // never read. Only a present-but-large (> 256) source falls back to the heap.
            var hCount = _hasHorizontalMetrics && _hmTable != null ? glyphIndices.Length : 0;
            Span<HorizontalGlyphMetric> hMetrics = hCount <= 256
                ? stackalloc HorizontalGlyphMetric[hCount]
                : new HorizontalGlyphMetric[hCount];

            var vCount = _hasVerticalMetrics && _vmTable != null ? glyphIndices.Length : 0;
            Span<VerticalGlyphMetric> vMetrics = vCount <= 256
                ? stackalloc VerticalGlyphMetric[vCount]
                : new VerticalGlyphMetric[vCount];

            bool hasHorizontal = false;
            bool hasVertical = false;

            // hmtx + HVAR are fused inside HorizontalMetricsTable.TryGetMetrics — when
            // variation is active we hand the cached scalers + HVAR table through so
            // hMetrics[i] is written exactly once per glyph rather than
            // hmtx-writes-then-HVAR-overwrites.
            if (_hasHorizontalMetrics && _hmTable != null)
            {
                if (_hvarTable is not null && _hvarRegionScalers is not null)
                {
                    hasHorizontal = _hmTable.TryGetMetrics(glyphIndices, hMetrics, _hvarTable, _hvarRegionScalers);
                }
                else
                {
                    hasHorizontal = _hmTable.TryGetMetrics(glyphIndices, hMetrics);
                }
            }

            // vmtx + VVAR fuse in the same fashion HVAR fuses with hmtx.
            if (_hasVerticalMetrics && _vmTable != null)
            {
                if (_vvarTable is not null && _vvarRegionScalers is not null)
                {
                    hasVertical = _vmTable.TryGetMetrics(glyphIndices, vMetrics, _vvarTable, _vvarRegionScalers);
                }
                else
                {
                    hasVertical = _vmTable.TryGetMetrics(glyphIndices, vMetrics);
                }
            }

            if (!hasHorizontal && !hasVertical)
            {
                return false;
            }

            if (_glyfTable != null)
            {
                // Read all bounding boxes in one batch, so the glyf and loca spans are fetched once
                // for the whole run rather than per glyph.
                Span<GlyphBounds> bounds = glyphIndices.Length <= 256
                    ? stackalloc GlyphBounds[glyphIndices.Length]
                    : new GlyphBounds[glyphIndices.Length];

                _glyfTable.GetGlyphBounds(glyphIndices, bounds);

                for (int i = 0; i < glyphIndices.Length; i++)
                {
                    var b = bounds[i];

                    metrics[i] = new GlyphMetrics
                    {
                        XBearing = b.XMin,
                        YBearing = b.YMax,
                        Width = (ushort)b.Width,
                        Height = (ushort)b.Height,
                        AdvanceWidth = hasHorizontal ? hMetrics[i].AdvanceWidth : (ushort)0,
                        AdvanceHeight = hasVertical ? vMetrics[i].AdvanceHeight : (ushort)0,
                    };
                }
            }
            else
            {
                // No glyf table (CFF / CFF2): there are no ink bounds to read, so bearings fall
                // back to hmtx/vmtx and the box stays zero. No bounds buffer is allocated either.
                for (int i = 0; i < glyphIndices.Length; i++)
                {
                    metrics[i] = new GlyphMetrics
                    {
                        XBearing = hasHorizontal ? hMetrics[i].LeftSideBearing : (short)0,
                        YBearing = hasVertical ? vMetrics[i].TopSideBearing : (short)0,
                        Width = 0,
                        Height = 0,
                        AdvanceWidth = hasHorizontal ? hMetrics[i].AdvanceWidth : (ushort)0,
                        AdvanceHeight = hasVertical ? vMetrics[i].AdvanceHeight : (ushort)0,
                    };
                }
            }

            return true;
        }


        /// <summary>
        /// Reads ink bounding boxes for a batch of glyphs from the font's <c>glyf</c> table.
        /// </summary>
        /// <remarks>
        /// Allocation-free hot path for glyph ink-bounds computation: the <c>glyf</c> and
        /// <c>loca</c> spans are fetched once for the whole batch. Use this rather than
        /// <see cref="TryGetGlyphMetrics(ReadOnlySpan{ushort}, Span{GlyphMetrics})"/> when
        /// only bounds are needed and advances are already known (e.g. from shaping).
        /// </remarks>
        /// <param name="glyphIndices">Glyph identifiers to read.</param>
        /// <param name="bounds">Output; must be at least as long as <paramref name="glyphIndices"/>.
        /// Out-of-range, empty, or malformed glyphs are written as the default (zero) box.</param>
        /// <returns><c>true</c> if the font has a <c>glyf</c> table; otherwise <c>false</c>.</returns>
        internal bool TryGetGlyphBounds(ReadOnlySpan<ushort> glyphIndices, Span<GlyphBounds> bounds)
        {
            if (bounds.Length < glyphIndices.Length)
            {
                throw new ArgumentException("Output span must be at least as long as input span", nameof(bounds));
            }

            if (_glyfTable is null)
            {
                return false;
            }

            _glyfTable.GetGlyphBounds(glyphIndices, bounds);

            return true;
        }

        /// <summary>
        /// Retrieves the vector outline geometry for the specified glyph, in font design-unit space.
        /// </summary>
        /// <remarks>
        /// Returns <c>null</c> when the glyph ID is out of range, the font has no <c>glyf</c> table
        /// (e.g. CFF / CFF2), or the glyph data cannot be parsed (malformed font, cyclic composite,
        /// depth limit exceeded). The outline is in font design units (Y-up): apply the
        /// <c>emSize / DesignEmHeight</c> scale, the Y-flip, and the glyph position yourself — via
        /// <c>IGeometryImpl.WithTransform</c> or a drawing-context transform. Variable-font axis
        /// configuration is taken from the typeface instance itself.
        /// </remarks>
        /// <param name="glyphIndex">The identifier of the glyph to retrieve.</param>
        /// <returns>
        /// An immutable <see cref="IGeometryImpl"/> outline — safe to cache and share, and drawable
        /// via the <c>DrawGeometry</c> overload that takes an <see cref="IGeometryImpl"/> — or
        /// <c>null</c> when no outline is available. Returned as the lightweight platform geometry
        /// rather than a <see cref="Geometry"/> (<see cref="AvaloniaObject"/>) so it can be cached
        /// and used on the hot path; do not mutate it.
        /// </returns>
        public IGeometryImpl? GetGlyphOutline(ushort glyphIndex)
        {
            if (glyphIndex >= GlyphCount)
            {
                return null;
            }

            if (_glyfTable is null)
            {
                return null;
            }

            var geometry = _renderInterface.CreateStreamGeometry();

            // The active variation coords are precomputed once at clone time and stored
            // on the typeface — see _activeCoords. Static fonts and default-instance
            // lookups (where _activeCoords is null) pass an empty span and skip the
            // gvar deformation path entirely.
            ReadOnlySpan<float> activeCoords = _gvarTable is not null && _activeCoords is not null
                ? _activeCoords
                : default;

            using (var ctx = geometry.Open())
            {
                // Build the outline in font design-unit space (identity transform); callers apply
                // the scale / position. Wrapped so the shared, cacheable result is immutable.
                if (_glyfTable.TryBuildGlyphGeometry(
                        (int)glyphIndex,
                        Matrix.Identity,
                        ctx,
                        _gvarTable,
                        activeCoords))
                {
                    return new ImmutableGeometryImpl(geometry);
                }
            }

            return null;
        }

        /// <summary>
        /// Derives this font's <see cref="NormalizedVariationPosition"/> from user-space
        /// settings (e.g. <c>wght = 700</c>), normalizing through the font's <c>fvar</c>
        /// and <c>avar</c> tables. Internal: normalized coordinates are font-relative, so
        /// the public API speaks user space (<see cref="FontVariationSettings"/>) and this
        /// conversion happens per font at application time.
        /// </summary>
        /// <param name="settings">
        /// Axis values in the same space the font designer exposed
        /// (<see cref="FontVariationAxis.MinimumValue"/> .. <see cref="FontVariationAxis.MaximumValue"/>).
        /// Axes the font does not declare are silently ignored; axes present in the font
        /// but absent from the settings use their default value (overridable by
        /// <paramref name="instanceIndex"/>). Pass <c>null</c> when relying entirely on a
        /// named instance.
        /// </param>
        /// <param name="instanceIndex">
        /// Optional zero-based index into <see cref="NamedInstances"/>. When provided, the
        /// instance's coordinates are used as the baseline; values in
        /// <paramref name="settings"/> override them per axis. Useful as a shorthand for
        /// "start from this preset and tweak one axis".
        /// </param>
        /// <returns>
        /// The font's normalized position. Returns
        /// <c>default(NormalizedVariationPosition)</c> when the font has no <c>fvar</c>
        /// table (static fonts) or when every axis resolves to its default value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="instanceIndex"/> is non-null and outside the bounds of
        /// <see cref="NamedInstances"/>.
        /// </exception>
        internal NormalizedVariationPosition CreateNormalizedPosition(
            FontVariationSettings? settings,
            int? instanceIndex = null)
        {
            if (_fvarTable is null)
            {
                // Static font — no axes to normalize.
                return default;
            }

            NormalizedVariationPosition basePosition = default;

            if (instanceIndex is int idx)
            {
                if ((uint)idx >= (uint)_fvarTable.Instances.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(instanceIndex),
                        idx,
                        $"Instance index must be in the range [0, {_fvarTable.Instances.Length}).");
                }

                basePosition = NormalizeUserValues(_fvarTable.Instances[idx].Coordinates);
            }

            return CreateNormalizedPosition(settings, basePosition);
        }

        /// <summary>
        /// Derives this font's <see cref="NormalizedVariationPosition"/> by starting from
        /// <paramref name="basePosition"/> and overriding it per axis with the user-space
        /// <paramref name="settings"/>.
        /// </summary>
        /// <remarks>
        /// This is the CSS order of font matching: the position chosen from weight, width and
        /// style comes first, and <c>font-variation-settings</c> overrides only the axes it names.
        /// </remarks>
        /// <param name="settings">
        /// User-space axis values; axes the font does not declare are ignored. <c>null</c> or
        /// empty keeps <paramref name="basePosition"/> unchanged.
        /// </param>
        /// <param name="basePosition">
        /// A normalized position of this font, for example the <see cref="VariationPosition"/>
        /// of a clone picked during font matching.
        /// </param>
        /// <returns>
        /// The combined normalized position; <c>default(NormalizedVariationPosition)</c> for
        /// static fonts and when every axis resolves to its default value.
        /// </returns>
        internal NormalizedVariationPosition CreateNormalizedPosition(
            FontVariationSettings? settings,
            NormalizedVariationPosition basePosition)
        {
            if (_fvarTable is null)
            {
                return default;
            }

            if (settings is null || settings.IsEmpty)
            {
                return basePosition;
            }

            var axes = _fvarTable.Axes;
            var variations = settings.Variations;

            // Skip axes that resolve to the default so the result equals
            // default(NormalizedVariationPosition) when the caller asked for the default-instance
            // point — important for cache identity at the GlyphTypeface layer
            // (WithVariation(default) returns the source).
            Dictionary<OpenTypeTag, float>? normalized = null;

            for (var i = 0; i < axes.Length; i++)
            {
                var axis = axes[i];
                var normalizedValue = basePosition.GetCoordinateOrDefault(axis.Tag);

                // The last setting for an axis wins, matching CSS declaration order.
                for (var j = variations.Length - 1; j >= 0; j--)
                {
                    if (variations[j].Tag == axis.Tag)
                    {
                        normalizedValue = NormalizeAxisValue(i, (float)variations[j].Value);
                        break;
                    }
                }

                if (normalizedValue != 0f)
                {
                    normalized ??= new Dictionary<OpenTypeTag, float>(axes.Length);
                    normalized[axis.Tag] = normalizedValue;
                }
            }

            return normalized is null ? default : NormalizedVariationPosition.FromCoordinates(normalized);
        }

        /// <summary>
        /// Normalizes a full set of user-space axis values, such as a named instance's
        /// coordinates. Axes missing from <paramref name="userValues"/> take their default.
        /// </summary>
        private NormalizedVariationPosition NormalizeUserValues(IReadOnlyDictionary<OpenTypeTag, float> userValues)
        {
            var axes = _fvarTable!.Axes;

            Dictionary<OpenTypeTag, float>? normalized = null;

            for (var i = 0; i < axes.Length; i++)
            {
                if (!userValues.TryGetValue(axes[i].Tag, out var userValue))
                {
                    continue;
                }

                var normalizedValue = NormalizeAxisValue(i, userValue);

                if (normalizedValue != 0f)
                {
                    normalized ??= new Dictionary<OpenTypeTag, float>(axes.Length);
                    normalized[axes[i].Tag] = normalizedValue;
                }
            }

            return normalized is null ? default : NormalizedVariationPosition.FromCoordinates(normalized);
        }

        /// <summary>
        /// Converts a user-space value of the axis at <paramref name="axisIndex"/> to its
        /// normalized, avar-corrected, F2Dot14-quantized coordinate.
        /// </summary>
        private float NormalizeAxisValue(int axisIndex, float userValue)
        {
            var axis = _fvarTable!.Axes[axisIndex];

            // Clamp to the axis range. fvar treats values outside [min, max] as a
            // best-effort clamp rather than an error — matches CSS and DirectWrite.
            if (userValue < axis.MinimumValue)
            {
                userValue = axis.MinimumValue;
            }
            else if (userValue > axis.MaximumValue)
            {
                userValue = axis.MaximumValue;
            }

            // Linear fvar normalization into [-1, 1] anchored at the default value.
            // The two halves of the axis (below and above default) are normalized
            // independently — this is what makes the design "default" land at 0
            // regardless of where min and max sit.
            float normalizedValue;
            if (userValue == axis.DefaultValue)
            {
                normalizedValue = 0f;
            }
            else if (userValue < axis.DefaultValue)
            {
                var range = axis.DefaultValue - axis.MinimumValue;
                normalizedValue = range > 0f
                    ? (userValue - axis.DefaultValue) / range
                    : 0f;
            }
            else
            {
                var range = axis.MaximumValue - axis.DefaultValue;
                normalizedValue = range > 0f
                    ? (userValue - axis.DefaultValue) / range
                    : 0f;
            }

            // avar segment-map correction. Identity on axes the table doesn't cover.
            if (_avarTable is not null)
            {
                normalizedValue = _avarTable.Remap(axisIndex, normalizedValue);
            }

            // Quantize to the F2Dot14 grid (1/16384) the font binary itself uses for
            // normalized coordinates — gvar, avar and the item variation stores cannot
            // represent a finer position, so this loses nothing. It also bounds the
            // per-source variation-clone cache under animation: a swept axis lands on
            // at most 32769 distinct positions instead of one per float progress value.
            return MathF.Round(normalizedValue * 16384f) / 16384f;
        }

        /// <summary>
        /// Returns a <see cref="GlyphTypeface"/> configured for the given user-space
        /// variation settings — the public front door for variable-font configuration.
        /// </summary>
        /// <param name="settings">
        /// The desired axis values in user space (e.g. <c>wght = 700</c>), as declared by
        /// the font's <c>fvar</c> table. Values are clamped to each axis range and axes
        /// the font does not declare are ignored. <c>null</c> or
        /// <see cref="FontVariationSettings.Empty"/> means the design defaults.
        /// </param>
        /// <param name="instanceIndex">
        /// Optional index of a named instance (see <see cref="NamedInstances"/>) to use
        /// as the base position; explicit <paramref name="settings"/> values override the
        /// instance's value per axis.
        /// </param>
        /// <returns>
        /// <c>this</c> for static fonts, for design-default requests, and for requests
        /// matching the receiver's own position; otherwise a cached or freshly-cloned
        /// typeface. Settings are normalized per font before caching, so two settings
        /// that resolve to the same position (for example two values clamped to the same
        /// axis maximum) share one clone.
        /// </returns>
        public GlyphTypeface WithVariations(FontVariationSettings? settings, int? instanceIndex = null)
            => WithVariation(CreateNormalizedPosition(settings, instanceIndex));

        /// <summary>
        /// Returns a <see cref="GlyphTypeface"/> at this typeface's own
        /// <see cref="VariationPosition"/> with the axes named by <paramref name="settings"/>
        /// replaced. Unlike <see cref="WithVariations"/>, which measures settings from the
        /// design default, the axes the settings leave out keep the values font matching chose.
        /// </summary>
        internal GlyphTypeface WithVariationOverrides(FontVariationSettings? settings)
            => WithVariation(CreateNormalizedPosition(settings, _variationPosition));

        /// <summary>
        /// Returns a <see cref="GlyphTypeface"/> bound to the same underlying font face
        /// but at the specified variation point.
        /// </summary>
        /// <param name="variation">
        /// Normalized variation coordinates, typically produced by
        /// <see cref="CreateNormalizedPosition(FontVariationSettings, int?)"/>. Pass
        /// <c>default(NormalizedVariationPosition)</c> to request the default-instance
        /// typeface.
        /// </param>
        /// <returns>
        /// <para>
        /// <c>this</c> if <paramref name="variation"/> matches the receiver's
        /// <see cref="VariationPosition"/>, or if the font has no <c>fvar</c> table
        /// (a static font — variation requests are silently ignored, matching CSS
        /// behavior).
        /// </para>
        /// <para>
        /// Otherwise a cached or freshly-cloned <see cref="GlyphTypeface"/> bound to
        /// the requested variation point. Repeated calls with equal settings return
        /// the same instance.
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// Variation tracking lives on the <see cref="GlyphTypeface"/> layer. The
        /// platform layer (<see cref="IPlatformTypeface"/>) and shaping layer
        /// (<see cref="ITextShaperTypeface"/>) participate via their own
        /// <c>WithVariation</c> overrides; when a platform hasn't implemented the
        /// override the default returns <c>this</c>, so the varied
        /// <see cref="GlyphTypeface"/> still tracks the requested position and the
        /// outline-API consumers that read <see cref="VariationPosition"/> become
        /// variation-correct independently of native rendering.
        /// </para>
        /// <para>
        /// Per-variation typefaces are cached on the source. The cache key is the
        /// <see cref="NormalizedVariationPosition"/>, which already carries its own
        /// structural equality + cached hash. The cache is unbounded — LRU eviction
        /// is a possible follow-up if profiling shows the cache growing without bound
        /// (e.g. animating a weight axis across many distinct values without ever
        /// settling).
        /// </para>
        /// </remarks>
        internal GlyphTypeface WithVariation(NormalizedVariationPosition variation)
        {
            // Static font — no axes to vary on; silently ignore non-default requests.
            if (_fvarTable is null)
            {
                return this;
            }

            // Delegate to the source's cache so all variations of the same underlying
            // font share resources and a single ownership chain.
            var source = _sourceTypeface ?? this;

            // The default position always resolves to the source. This makes
            // clone.WithVariation(default) return the original default-instance
            // typeface and clone.WithVariation(clone.VariationPosition) return the
            // clone itself (via the cache hit below).
            if (variation.IsDefault)
            {
                return source;
            }

            // Allocate the cache lazily. We tolerate the rare race where two threads
            // both initialize and one allocation loses — the loser's empty dict is
            // discarded and the winner's dict serves both threads.
            if (source._variationCache is null)
            {
                Interlocked.CompareExchange(
                    ref source._variationCache,
                    new ConcurrentDictionary<NormalizedVariationPosition, GlyphTypeface>(),
                    null);
            }

            return source._variationCache!.GetOrAdd(
                variation,
                static (v, src) => src.CreateVariation(v),
                source);
        }

        /// <summary>
        /// Builds a variation clone for the cache miss path. Always called on the
        /// source typeface (<see cref="WithVariation"/> redirects via <see cref="_sourceTypeface"/>).
        /// </summary>
        private GlyphTypeface CreateVariation(NormalizedVariationPosition variation)
        {
            var platformVariation = PlatformTypeface.WithVariation(variation);
            return new GlyphTypeface(this, platformVariation, variation);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IReadOnlyList<OpenTypeTag> LoadSupportedFeatures()
        {
            var gPosFeatures = FeatureListTable.LoadGPos(this);
            var gSubFeatures = FeatureListTable.LoadGSub(this);

            var count = (gPosFeatures?.Features.Count ?? 0) + (gSubFeatures?.Features.Count ?? 0);

            if (count == 0)
            {
                return [];
            }

            var supportedFeatures = new List<OpenTypeTag>(count);

            if (gPosFeatures != null)
            {
                foreach (var gPosFeature in gPosFeatures.Features)
                {
                    if (supportedFeatures.Contains(gPosFeature))
                    {
                        continue;
                    }

                    supportedFeatures.Add(gPosFeature);
                }
            }

            if (gSubFeatures != null)
            {
                foreach (var gSubFeature in gSubFeatures.Features)
                {
                    if (supportedFeatures.Contains(gSubFeature))
                    {
                        continue;
                    }

                    supportedFeatures.Add(gSubFeature);
                }
            }

            return supportedFeatures;
        }

        private static FontStyle GetFontStyle(OS2Table? oS2Table, HeadTable? headTable, PostTable postTable)
        {
            bool isItalic = false;
            bool isOblique = false;

            if (oS2Table.HasValue)
            {
                isItalic = (oS2Table.Value.Selection & OS2Table.FontSelectionFlags.ITALIC) != 0;
                isOblique = (oS2Table.Value.Selection & OS2Table.FontSelectionFlags.OBLIQUE) != 0;
            }

            if (!isItalic && headTable != null)
            {
                isItalic = headTable.MacStyle.HasFlag(MacStyleFlags.Italic);
            }

            var italicAngle = postTable.ItalicAngle;

            if (isOblique)
            {
                return FontStyle.Oblique;
            }

            if (Math.Abs(italicAngle) > 0.01f && !isItalic)
            {
                return FontStyle.Oblique;
            }

            if (isItalic)
            {
                return FontStyle.Italic;
            }

            return FontStyle.Normal;
        }

        private static FontWeight GetFontWeight(OS2Table? os2Table, HeadTable? headTable)
        {
            if (os2Table.HasValue && os2Table.Value.WeightClass >= 1 && os2Table.Value.WeightClass <= 1000)
            {
                return (FontWeight)os2Table.Value.WeightClass;
            }

            if (headTable != null && headTable.MacStyle.HasFlag(MacStyleFlags.Bold))
            {
                return FontWeight.Bold;
            }

            if (os2Table.HasValue && os2Table.Value.Panose.FamilyKind == PanoseFamilyKind.LatinText)
            {
                return os2Table.Value.Panose.Weight switch
                {
                    PanoseWeight.VeryLight => FontWeight.Thin,
                    PanoseWeight.Light => FontWeight.Light,
                    PanoseWeight.Thin => FontWeight.ExtraLight,
                    PanoseWeight.Book => FontWeight.Normal,
                    PanoseWeight.Medium => FontWeight.Medium,
                    PanoseWeight.Demi => FontWeight.SemiBold,
                    PanoseWeight.Bold => FontWeight.Bold,
                    PanoseWeight.Heavy => FontWeight.ExtraBold,
                    PanoseWeight.Black => FontWeight.Black,
                    PanoseWeight.ExtraBlack => FontWeight.ExtraBlack,
                    _ => FontWeight.Normal
                };
            }

            return FontWeight.Normal;
        }

        private static FontStretch GetFontStretch(OS2Table? os2Table)
        {
            if (os2Table.HasValue && os2Table.Value.WidthClass >= 1 && os2Table.Value.WidthClass <= 9)
            {
                return (FontStretch)os2Table.Value.WidthClass;
            }

            if (os2Table.HasValue && os2Table.Value.Panose.FamilyKind == PanoseFamilyKind.LatinText)
            {
                return os2Table.Value.Panose.Proportion switch
                {
                    PanoseProportion.VeryCondensed => FontStretch.UltraCondensed,
                    PanoseProportion.Condensed => FontStretch.Condensed,
                    PanoseProportion.Modern or PanoseProportion.EvenWidth or PanoseProportion.OldStyle => FontStretch.Normal,
                    PanoseProportion.Extended => FontStretch.Expanded,
                    PanoseProportion.VeryExtended => FontStretch.UltraExpanded,
                    PanoseProportion.Monospaced => FontStretch.Normal,
                    _ => FontStretch.Normal
                };
            }

            return FontStretch.Normal;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!disposing)
            {
                return;
            }

            // Dispose all cached variation clones before tearing down the platform
            // typeface — clones may hold shaper handles bound to it. The cache lives
            // only on the source; for a variation clone _variationCache is null so this
            // loop is a no-op.
            var cache = _variationCache;
            if (cache is not null)
            {
                foreach (var entry in cache)
                {
                    entry.Value.Dispose();
                }
                cache.Clear();
            }

            if (_ownsTextShaperTypeface)
            {
                _textShaperTypeface?.Dispose();
            }

            // Only the source-of-truth owns and disposes the platform typeface. When
            // the platform's WithVariation override returned 'this', the clone shares
            // the source's IPlatformTypeface and skips the disposal call so the source
            // can release it exactly once. When the override actually cloned (e.g. a
            // real SKTypeface clone), the ownership flag flips on automatically and
            // the cloned platform typeface is released here.
            if (_ownsPlatformTypeface)
            {
                PlatformTypeface.Dispose();
            }
        }
    }
}
