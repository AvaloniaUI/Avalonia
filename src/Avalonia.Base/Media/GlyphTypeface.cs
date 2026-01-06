using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Cmap;
using Avalonia.Media.Fonts.Tables.Metrics;
using Avalonia.Media.Fonts.Tables.Name;
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

        private bool _isDisposed;

        private readonly NameTable? _nameTable;
        private readonly OS2Table _os2Table;
        private readonly CharacterToGlyphMap _cmapTable;
        private readonly HorizontalHeaderTable _hhTable;
        private readonly VerticalHeaderTable _vhTable;
        private readonly HorizontalMetricsTable? _hmTable;
        private readonly VerticalMetricsTable? _vmTable;
        private readonly bool _hasOs2Table;
        private readonly bool _hasHorizontalMetrics;
        private readonly bool _hasVerticalMetrics;

        private IReadOnlyList<OpenTypeTag>? _supportedFeatures;
        private ITextShaperTypeface? _textShaperTypeface;

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

            var postTable = PostTable.Load(this);

            var isFixedPitch = postTable.IsFixedPitch;
            var underlineOffset = postTable.UnderlinePosition;
            var underlineSize = postTable.UnderlineThickness;

            Metrics = new FontMetrics
            {
                DesignEmHeight = headTable?.UnitsPerEm ?? 0,
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

            // Use stackalloc for temporary buffers to avoid heap allocations
            Span<HorizontalGlyphMetric> hMetrics = glyphIds.Length <= 256
                ? stackalloc HorizontalGlyphMetric[glyphIds.Length]
                : new HorizontalGlyphMetric[glyphIds.Length];

            Span<VerticalGlyphMetric> vMetrics = glyphIds.Length <= 256
                ? stackalloc VerticalGlyphMetric[glyphIds.Length]
                : new VerticalGlyphMetric[glyphIds.Length];

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
