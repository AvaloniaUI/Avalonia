using System;
using System.Collections.Generic;
using System.Globalization;
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
    internal class GlyphTypeface : IGlyphTypeface
    {
        private bool _isDisposed;

        private readonly NameTable? _nameTable;
        private readonly OS2Table? _os2Table;
        private readonly IReadOnlyDictionary<int, ushort> _cmapTable;
        private readonly HorizontalHeaderTable? _hhTable;
        private readonly VerticalHeaderTable? _vhTable;
        private readonly HorizontalMetricsTable? _hmTable;
        private readonly VerticalMetricsTable? _vmTable;

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

            _os2Table = OS2Table.Load(this);
            _cmapTable = CmapTable.Load(this);

            var maxpTable = MaxpTable.Load(this) ?? throw new InvalidOperationException("Could not load the 'maxp' table.");

            GlyphCount = maxpTable.NumGlyphs;

            _hhTable = HorizontalHeaderTable.Load(this);

            if (_hhTable is not null)
            {
                _hmTable = HorizontalMetricsTable.Load(this, _hhTable.NumberOfHMetrics, GlyphCount);
            }

            _vhTable = VerticalHeaderTable.Load(this);

            if (_vhTable is not null)
            {
                _vmTable = VerticalMetricsTable.Load(this, _vhTable.NumberOfVMetrics, GlyphCount);
            }

            var ascent = 0;
            var descent = 0;
            var lineGap = 0;

            if (_os2Table != null && (_os2Table.Selection & OS2Table.FontSelectionFlags.USE_TYPO_METRICS) != 0)
            {
                ascent = -_os2Table.TypoAscender;
                descent = -_os2Table.TypoDescender;
                lineGap = _os2Table.TypoLineGap;
            }
            else
            {
                if (_hhTable != null)
                {
                    ascent = -_hhTable.Ascender;
                    descent = -_hhTable.Descender;
                    lineGap = _hhTable.LineGap;
                }
            }

            if (_os2Table != null && (ascent == 0 || descent == 0))
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

            var headTable = HeadTable.Load(this);
            var postTable = PostTable.Load(this);

            var isFixedPitch = postTable.IsFixedPitch;
            var underlineOffset = postTable.UnderlinePosition;
            var underlineSize = postTable.UnderlineThickness;

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)headTable.UnitsPerEm,
                Ascent = ascent,
                Descent = descent,
                LineGap = lineGap,
                UnderlinePosition = -underlineOffset,
                UnderlineThickness = underlineSize,
                StrikethroughPosition = -_os2Table?.StrikeoutPosition ?? 0,
                StrikethroughThickness = _os2Table?.StrikeoutSize ?? 0,
                IsFixedPitch = isFixedPitch
            };

            FontSimulations = fontSimulations;

            var fontWeight = _os2Table != null ? (FontWeight)_os2Table.WeightClass : FontWeight.Normal;

            Weight = (fontSimulations & FontSimulations.Bold) != 0 ? FontWeight.Bold : fontWeight;

            var style = _os2Table != null ? GetFontStyle(_os2Table, headTable, postTable) : FontStyle.Normal;

            Style = (fontSimulations & FontSimulations.Oblique) != 0 ? FontStyle.Italic : style;

            var stretch = _os2Table != null ? (FontStretch)_os2Table.WidthClass : FontStretch.Normal;

            Stretch = stretch;

            _nameTable = NameTable.Load(this);

            FamilyName = _nameTable?.FontFamilyName((ushort)CultureInfo.InvariantCulture.LCID) ?? "unknown";

            TypographicFamilyName = _nameTable?.GetNameById((ushort)CultureInfo.InvariantCulture.LCID, KnownNameIds.TypographicFamilyName) ?? FamilyName;

            if (_nameTable != null)
            {
                var familyNames = new Dictionary<CultureInfo, string>(1);
                var faceNames = new Dictionary<CultureInfo, string>(1);

                foreach (var nameRecord in _nameTable)
                {
                    if (nameRecord.NameID == KnownNameIds.FontFamilyName)
                    {
                        if (nameRecord.Platform != Fonts.Tables.PlatformID.Windows || nameRecord.LanguageID == 0)
                        {
                            continue;
                        }

                        var culture = GetCulture(nameRecord.LanguageID);

                        if (!familyNames.ContainsKey(culture))
                        {
                            familyNames[culture] = nameRecord.Value;
                        }

                    }

                    if (nameRecord.NameID == KnownNameIds.FontSubfamilyName)
                    {
                        if (nameRecord.Platform != Fonts.Tables.PlatformID.Windows || nameRecord.LanguageID == 0)
                        {
                            continue;
                        }

                        var culture = GetCulture(nameRecord.LanguageID);

                        if (!faceNames.ContainsKey(culture))
                        {
                            faceNames[culture] = nameRecord.Value;
                        }
                    }
                }

                FamilyNames = familyNames;
                FaceNames = faceNames;
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
                    return CultureInfo.GetCultureInfo(lcid) ?? CultureInfo.InvariantCulture;
                }
                catch (CultureNotFoundException)
                {
                    return CultureInfo.InvariantCulture;
                }
            }
        }

        public string TypographicFamilyName { get; }

        public IReadOnlyDictionary<CultureInfo, string> FamilyNames { get; }

        public IReadOnlyDictionary<CultureInfo, string> FaceNames { get; }

        public IReadOnlyList<OpenTypeTag> SupportedFeatures
        {
            get
            {
                if (_supportedFeatures != null)
                {
                    return _supportedFeatures;
                }

                var gPosFeatures = FeatureListTable.LoadGPos(this);
                var gSubFeatures = FeatureListTable.LoadGSub(this);

                var supportedFeatures = new List<OpenTypeTag>(gPosFeatures?.Features.Count ?? 0 + gSubFeatures?.Features.Count ?? 0);

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

                _supportedFeatures = supportedFeatures;

                return supportedFeatures;
            }
        }

        public FontSimulations FontSimulations { get; }

        public int ReplacementCodepoint { get; }

        public FontMetrics Metrics { get; }

        public uint GlyphCount { get; }

        public string FamilyName { get; }

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public IReadOnlyDictionary<int, ushort> CharacterToGlyphMap => _cmapTable;

        public IPlatformTypeface PlatformTypeface { get; }

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

        private static FontStyle GetFontStyle(OS2Table oS2Table, HeadTable headTable, PostTable postTable)
        {
            var isItalic = (oS2Table.Selection & OS2Table.FontSelectionFlags.ITALIC) != 0 || (headTable.MacStyle & 0x02) != 0;

            var isOblique = (oS2Table.Selection & OS2Table.FontSelectionFlags.OBLIQUE) != 0;

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ushort GetGlyphAdvance(ushort glyphId)
        {
            if (_hmTable is null)
            {
                return 0;
            }

            return _hmTable.GetAdvance(glyphId);
        }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = default;

            HorizontalGlyphMetric hMetric = default;
            VerticalGlyphMetric vMetric = default;

            if (_hmTable != null)
            {
                hMetric = _hmTable.GetMetrics(glyph);
            }

            if (_vmTable != null)
            {
                vMetric = _vmTable.GetMetrics(glyph);
            }

            if (hMetric.Equals(default) && vMetric.Equals(default))
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
    }
}
