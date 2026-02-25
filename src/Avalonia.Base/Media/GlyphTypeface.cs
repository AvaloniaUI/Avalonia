using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Cmap;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.Media.Fonts.Tables.Glyf;
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
        private readonly ColrTable? _colrTable;
        private readonly CpalTable? _cpalTable;

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

            if (HeadTable.TryLoad(this, out var headTable))
            {
                // Load glyf table once and cache for reuse by GetGlyphOutline
                GlyfTable.TryLoad(this, headTable, maxpTable, out _glyfTable);

                // Load COLR and CPAL tables for color glyph support
                ColrTable.TryLoad(this, out _colrTable);
                CpalTable.TryLoad(this, out _cpalTable);
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

        /// <summary>
        /// Gets a color glyph drawing for the specified glyph ID, if color data is available.
        /// </summary>
        /// <remarks>If the glyph does not have color data (such as COLR v1 or COLR v0 layers), this
        /// method returns null. For outline-only glyphs, use GetGlyphOutline instead to obtain the vector
        /// outline.</remarks>
        /// <param name="glyphId">The identifier of the glyph to retrieve. Must be less than or equal to the total number of glyphs in the
        /// font.</param>
        /// <param name="variation">The font variation settings to use when selecting the glyph drawing, or null to use the default variation.</param>
        /// <returns>An object representing the color glyph drawing for the specified glyph ID, or null if no color drawing is
        /// available for the glyph.</returns>
        public IGlyphDrawing? GetGlyphDrawing(ushort glyphId, FontVariationSettings? variation = null)
        {
            if (glyphId > GlyphCount)
            {
                return null;
            }

            // Try COLR v1 first
            if (_colrTable != null && _cpalTable != null && _colrTable.HasV1Data)
            {
                if (_colrTable.TryGetBaseGlyphV1Record(glyphId, out var record))
                {
                    return new ColorGlyphV1Drawing(this, _colrTable, _cpalTable, glyphId, record);
                }
            }

            // Fallback to COLR v0
            if (_colrTable != null && _cpalTable != null && _colrTable.HasColorLayers(glyphId))
            {
                return new ColorGlyphDrawing(this, _colrTable, _cpalTable, glyphId);
            }

            // For outline-only glyphs, return null - caller should use GetGlyphOutline() instead
            return null;
        }

        /// <summary>
        /// Retrieves the vector outline geometry for the specified glyph, optionally applying a transformation and font
        /// variation settings.
        /// </summary>
        /// <remarks>The returned geometry reflects any transformation and variation settings provided. If
        /// the font does not contain outline data for the specified glyph, or if the glyph identifier is out of range,
        /// the method returns null.</remarks>
        /// <param name="glyphId">The identifier of the glyph to retrieve. Must be less than or equal to the total number of glyphs in the
        /// font.</param>
        /// <param name="transform">A transformation matrix to apply to the glyph outline geometry.</param>
        /// <param name="variation">Optional font variation settings to use when retrieving the glyph outline. If null, default font variations
        /// are used.</param>
        /// <returns>A Geometry object representing the outline of the specified glyph, or null if the glyph does not exist or
        /// the outline cannot be retrieved.</returns>
        public Geometry? GetGlyphOutline(ushort glyphId, Matrix transform, FontVariationSettings? variation = null)
        {
            if (glyphId > GlyphCount)
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
                // Try to build the glyph geometry using the glyf table
                if (_glyfTable.TryBuildGlyphGeometry((int)glyphId, transform, ctx))
                {
                    var platformGeometry = new PlatformGeometry(geometry);

                    return platformGeometry;
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

        /// <summary>
        /// Attempts to retrieve and resolve the paint definition for a base glyph using COLR v1 data.
        /// </summary>
        /// <remarks>This method returns false if the COLR or CPAL tables are unavailable, if the glyph
        /// does not have COLR v1 data, or if the paint data cannot be parsed or resolved.</remarks>
        /// <param name="context">The color rendering context used to interpret the paint data.</param>
        /// <param name="record">The base glyph record containing the paint offset information.</param>
        /// <param name="paint">When this method returns, contains the resolved paint definition if successful; otherwise, null. This
        /// parameter is passed uninitialized.</param>
        /// <returns>true if the paint definition was successfully retrieved and resolved; otherwise, false.</returns>
        internal bool TryGetBaseGlyphV1Paint(ColrContext context, BaseGlyphV1Record record, [NotNullWhen(true)] out Paint? paint)
        {
            paint = null;

            var absolutePaintOffset = _colrTable!.GetAbsolutePaintOffset(record.PaintOffset);

            var decycler = PaintDecycler.Rent();
            try
            {
                if (!PaintParser.TryParse(_colrTable.ColrData.Span, absolutePaintOffset, in context, in decycler, out var parsedPaint))
                {
                    return false;
                }

                paint = PaintResolver.ResolvePaint(parsedPaint, in context);

                return true;
            }
            finally
            {
                PaintDecycler.Return(decycler);
            }
        }
    }

    /// <summary>
    /// Represents a color glyph drawing with multiple colored layers (COLR v0).
    /// </summary>
    internal sealed class ColorGlyphDrawing : IGlyphDrawing
    {
        private readonly GlyphTypeface _glyphTypeface;
        private readonly ColrTable _colrTable;
        private readonly CpalTable _cpalTable;
        private readonly ushort _glyphId;
        private readonly int _paletteIndex;

        public ColorGlyphDrawing(GlyphTypeface glyphTypeface, ColrTable colrTable, CpalTable cpalTable, ushort glyphId, int paletteIndex = 0)
        {
            _glyphTypeface = glyphTypeface;
            _colrTable = colrTable;
            _cpalTable = cpalTable;
            _glyphId = glyphId;
            _paletteIndex = paletteIndex;
        }

        public GlyphDrawingType Type => GlyphDrawingType.ColorLayers;

        public Rect Bounds
        {
            get
            {
                Rect? combinedBounds = null;
                var layerRecords = _colrTable.GetLayers(_glyphId);

                foreach (var layerRecord in layerRecords)
                {
                    var geometry = _glyphTypeface.GetGlyphOutline(layerRecord.GlyphId, Matrix.CreateScale(1, -1));
                    if (geometry != null)
                    {
                        var layerBounds = geometry.Bounds;
                        combinedBounds = combinedBounds.HasValue
                            ? combinedBounds.Value.Union(layerBounds)
                            : layerBounds;
                    }
                }

                return combinedBounds ?? default;
            }
        }

        /// <summary>
        /// Draws the color glyph at the specified origin using the provided drawing context.
        /// </summary>
        /// <remarks>This method renders a multi-layered color glyph by drawing each layer with its
        /// associated color. The colors are determined by the current palette and may fall back to black if a color is
        /// not found. The method does not apply any transformations; the glyph is drawn at the specified origin in the
        /// current context.</remarks>
        /// <param name="context">The drawing context to use for rendering the glyph. Must not be null.</param>
        /// <param name="origin">The point, in device-independent pixels, that specifies the origin at which to draw the glyph.</param>
        public void Draw(DrawingContext context, Point origin)
        {
            var layerRecords = _colrTable.GetLayers(_glyphId);

            foreach (var layerRecord in layerRecords)
            {
                // Get the color for this layer from the CPAL table
                if (!_cpalTable.TryGetColor(_paletteIndex, layerRecord.PaletteIndex, out var color))
                {
                    color = Colors.Black; // Fallback
                }

                // Get the outline geometry for the layer glyph
                var geometry = _glyphTypeface.GetGlyphOutline(layerRecord.GlyphId, Matrix.CreateScale(1, -1));

                if (geometry != null)
                {
                    using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
                    {
                        context.DrawGeometry(new SolidColorBrush(color), null, geometry);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a COLR v1 color glyph drawing with paint-based rendering.
    /// </summary>
    internal sealed class ColorGlyphV1Drawing : IGlyphDrawing
    {
        private readonly ColrContext _context;
        private readonly ushort _glyphId;
        private readonly int _paletteIndex;

        private readonly Rect _bounds;
        private readonly Paint? _paint;

        public ColorGlyphV1Drawing(GlyphTypeface glyphTypeface, ColrTable colrTable, CpalTable cpalTable,
            ushort glyphId, BaseGlyphV1Record record, int paletteIndex = 0)
        {
            _context = new ColrContext(glyphTypeface, colrTable, cpalTable, paletteIndex);
            _glyphId = glyphId;
            _paletteIndex = paletteIndex;

            var decycler = PaintDecycler.Rent();

            try
            {
                if (glyphTypeface.TryGetBaseGlyphV1Paint(_context, record, out _paint))
                {
                    if (_context.ColrTable.TryGetClipBox(_glyphId, out var clipRect))
                    {
                        // COLR v1 paint graphs operate in font-space coordinates (Y-up).
                        _bounds = clipRect.TransformToAABB(Matrix.CreateScale(1, -1));
                    }
                }
            }
            finally
            {
                PaintDecycler.Return(decycler);

            }
        }

        public GlyphDrawingType Type => GlyphDrawingType.ColorLayers;

        public Rect Bounds => _bounds;

        public void Draw(DrawingContext context, Point origin)
        {
            if (_paint == null)
            {
                return;
            }

            var decycler = PaintDecycler.Rent();

            try
            {
                using (context.PushTransform(Matrix.CreateScale(1, -1) * Matrix.CreateTranslation(origin)))
                {
                    PaintTraverser.Traverse(_paint, new ColorGlyphV1Painter(context, _context), Matrix.Identity);
                }
            }
            finally
            {
                PaintDecycler.Return(decycler);
            }
        }
    }
}
