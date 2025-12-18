using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Cmap;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Media.Fonts.Tables.Metrics;
using Avalonia.Media.Fonts.Tables.Name;
using Avalonia.Media.Imaging;
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
    public sealed class GlyphTypeface : IGlyphTypeface
    {
        private static readonly IReadOnlyDictionary<CultureInfo, string> s_emptyStringDictionary = 
            new Dictionary<CultureInfo, string>(0);

        private bool _isDisposed;

        private readonly NameTable? _nameTable;
        private readonly OS2Table? _os2Table;
        private readonly IReadOnlyDictionary<int, ushort> _cmapTable;
        private readonly HorizontalHeaderTable? _hhTable;
        private readonly VerticalHeaderTable? _vhTable;
        private readonly HorizontalMetricsTable? _hmTable;
        private readonly VerticalMetricsTable? _vmTable;
        private readonly GlyfTable? _glyfTable;
        private readonly ColrTable? _colrTable;
        private readonly CpalTable? _cpalTable;

        private IReadOnlyList<OpenTypeTag>? _supportedFeatures;
        private ITextShaperTypeface? _textShaperTypeface;
        private readonly IPlatformRenderInterface _renderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

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
                _hmTable = HorizontalMetricsTable.Load(this, _hhTable.Value.NumberOfHMetrics, GlyphCount);
            }

            _vhTable = VerticalHeaderTable.Load(this);

            if (_vhTable is not null)
            {
                _vmTable = VerticalMetricsTable.Load(this, _vhTable.Value.NumberOfVMetrics, GlyphCount);
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
                    ascent = -_hhTable.Value.Ascender;
                    descent = -_hhTable.Value.Descender;
                    lineGap = _hhTable.Value.LineGap;
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

                        faceNames ??= new Dictionary<CultureInfo, string>(1);

                        if (!faceNames.ContainsKey(culture))
                        {
                            faceNames[culture] = nameRecord.Value;
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
                    return CultureInfo.GetCultureInfo(lcid) ?? CultureInfo.InvariantCulture;
                }
                catch (CultureNotFoundException)
                {
                    return CultureInfo.InvariantCulture;
                }
            }

            // Load glyf table once and cache for reuse by GetGlyphOutline
            _glyfTable = GlyfTable.Load(this);

            // Load COLR and CPAL tables for color glyph support
            _colrTable = ColrTable.Load(this);
            _cpalTable = CpalTable.Load(this);
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

        public FontMetrics Metrics { get; }

        public int GlyphCount { get; }

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

            if (!_hmTable.TryGetAdvance(glyphId, out var advance))
            {
                return 0;
            }

            return advance;
        }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = default;

            HorizontalGlyphMetric hMetric = default;
            VerticalGlyphMetric vMetric = default;

            var hasHorizontal = false;
            var hasVertical = false;

            if (_hmTable != null)
            {
                hasHorizontal = _hmTable.TryGetMetrics(glyph, out hMetric);
            }

            if (_vmTable != null)
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


        /// <summary>
        /// Attempts to create a new instance of the ColrContext using the specified palette index and paint decycler.
        /// </summary>
        /// <remarks>This method returns false if the required COLR or CPAL tables are not available. The
        /// output parameter is set to its default value in this case.</remarks>
        /// <param name="paletteIndex">The zero-based index of the color palette to use when creating the context.</param>
        /// <param name="context">When this method returns, contains the created ColrContext if the operation succeeds; otherwise, the default
        /// value.</param>
        /// <returns>true if the ColrContext was successfully created; otherwise, false.</returns>
        internal bool TryCreateColrContext(int paletteIndex, out ColrContext context)
        {
            context = default;

            if (_colrTable == null || _cpalTable == null)
            {
                return false;
            }

            context = new ColrContext(
                this,
                _colrTable,
                _cpalTable,
                paletteIndex);

            return true;
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
