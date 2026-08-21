using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Cmap;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Media.Fonts.Tables.Metrics;
using Avalonia.Media.Fonts.Tables.Name;
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

        private readonly bool _hasOs2Table;
        private readonly bool _hasHorizontalMetrics;
        private readonly bool _hasVerticalMetrics;
        private readonly string[] _designLanguages;
        private readonly string[] _supportedLanguages;

        private IReadOnlyList<OpenTypeTag>? _supportedFeatures;
        private ITextShaperTypeface? _textShaperTypeface;
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
        /// Gets the platform-specific typeface associated with this font.
        /// </summary>
        public IPlatformTypeface PlatformTypeface { get; }

        /// <summary>
        /// Gets the typeface information used by the text shaper for this font.
        /// </summary>
        /// <remarks>The returned typeface is created on demand and cached for subsequent accesses. This
        /// property is typically used by text rendering components that require low-level font shaping
        /// details.</remarks>
        public ITextShaperTypeface TextShaperTypeface
        {
            get
            {
                if (_textShaperTypeface != null)
                {
                    return _textShaperTypeface;
                }

                var textShaper = AvaloniaLocator.Current.GetRequiredService<ITextShaperImpl>();

                _textShaperTypeface = textShaper.CreateTypeface(this);

                return _textShaperTypeface;
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
        /// <param name="glyphId">The identifier of the glyph for which to obtain the horizontal advance width.</param>
        /// <param name="advance">When this method returns, contains the horizontal advance width of the glyph if found; otherwise, zero. This
        /// parameter is passed uninitialized.</param>
        /// <returns>true if the horizontal advance width was successfully retrieved; otherwise, false.</returns>
        public bool TryGetHorizontalGlyphAdvance(ushort glyphId, out ushort advance)
        {
            advance = default;

            if (!_hasHorizontalMetrics || _hmTable is null)
            {
                return false;
            }

            if (!_hmTable.TryGetAdvance(glyphId, out advance))
            {
                return false;
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
        /// <param name="glyphIds">Read-only span of glyph identifiers for which to retrieve advance widths.</param>
        /// <param name="advances">Output span to write the advance widths. Must be at least as long as <paramref name="glyphIds"/>.</param>
        /// <returns>true if horizontal metrics are available and all advances were successfully retrieved; otherwise, false.</returns>
        public bool TryGetHorizontalGlyphAdvances(ReadOnlySpan<ushort> glyphIds, Span<ushort> advances)
        {
            if (!_hasHorizontalMetrics || _hmTable is null)
            {
                return false;
            }

            return _hmTable.TryGetAdvances(glyphIds, advances);
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

            if (!hasHorizontal && !hasVertical)
            {
                return false;
            }

            metrics = new GlyphMetrics
            {
                XBearing = hMetric.LeftSideBearing,
                YBearing = vMetric.TopSideBearing,
                Width = hMetric.AdvanceWidth,
                Height = vMetric.AdvanceHeight
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
        /// <param name="glyphIds">Read-only span of glyph identifiers for which to retrieve metrics.</param>
        /// <param name="metrics">Output span to write the glyph metrics. Must be at least as long as <paramref name="glyphIds"/>.</param>
        /// <returns>true if metrics are available and all were successfully retrieved; otherwise, false.</returns>
        public bool TryGetGlyphMetrics(ReadOnlySpan<ushort> glyphIds, Span<GlyphMetrics> metrics)
        {
            if (metrics.Length < glyphIds.Length)
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
            var hCount = _hasHorizontalMetrics && _hmTable != null ? glyphIds.Length : 0;
            Span<HorizontalGlyphMetric> hMetrics = hCount <= 256
                ? stackalloc HorizontalGlyphMetric[hCount]
                : new HorizontalGlyphMetric[hCount];

            var vCount = _hasVerticalMetrics && _vmTable != null ? glyphIds.Length : 0;
            Span<VerticalGlyphMetric> vMetrics = vCount <= 256
                ? stackalloc VerticalGlyphMetric[vCount]
                : new VerticalGlyphMetric[vCount];

            bool hasHorizontal = false;
            bool hasVertical = false;

            // Batch retrieve horizontal metrics
            if (_hasHorizontalMetrics && _hmTable != null)
            {
                hasHorizontal = _hmTable.TryGetMetrics(glyphIds, hMetrics);
            }

            // Batch retrieve vertical metrics
            if (_hasVerticalMetrics && _vmTable != null)
            {
                hasVertical = _vmTable.TryGetMetrics(glyphIds, vMetrics);
            }

            if (!hasHorizontal && !hasVertical)
            {
                return false;
            }

            // Combine horizontal and vertical metrics
            for (int i = 0; i < glyphIds.Length; i++)
            {
                metrics[i] = new GlyphMetrics
                {
                    XBearing = hasHorizontal ? hMetrics[i].LeftSideBearing : (short)0,
                    YBearing = hasVertical ? vMetrics[i].TopSideBearing : (short)0,
                    Width = hasHorizontal ? hMetrics[i].AdvanceWidth : (ushort)0,
                    Height = hasVertical ? vMetrics[i].AdvanceHeight : (ushort)0
                };
            }

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
        /// <param name="glyphId">The identifier of the glyph to retrieve.</param>
        /// <returns>
        /// An immutable <see cref="IGeometryImpl"/> outline — safe to cache and share, and drawable
        /// via the <c>DrawGeometry</c> overload that takes an <see cref="IGeometryImpl"/> — or
        /// <c>null</c> when no outline is available. Returned as the lightweight platform geometry
        /// rather than a <see cref="Geometry"/> (<see cref="AvaloniaObject"/>) so it can be cached
        /// and used on the hot path; do not mutate it.
        /// </returns>
        public IGeometryImpl? GetGlyphOutline(ushort glyphId)
        {
            if (glyphId >= GlyphCount)
            {
                return null;
            }

            if (_glyfTable is null)
            {
                return null;
            }

            var geometry = _renderInterface.CreateStreamGeometry();

            using (var ctx = geometry.Open())
            {
                // Build the outline in font design-unit space (identity transform); callers apply
                // the scale / position. Wrapped so the shared, cacheable result is immutable.
                if (_glyfTable.TryBuildGlyphGeometry((int)glyphId, Matrix.Identity, ctx))
                {
                    return new ImmutableGeometryImpl(geometry);
                }
            }

            return null;
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

            PlatformTypeface.Dispose();
        }
    }
}
