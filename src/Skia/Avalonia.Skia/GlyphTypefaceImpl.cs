using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Name;
using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypeface2
    {
        private bool _isDisposed;
        private readonly SKTypeface _typeface;
        private readonly NameTable? _nameTable;
        private readonly OS2Table? _os2Table;
        private readonly HorizontalHeadTable? _hhTable;
        private IReadOnlyList<OpenTypeTag>? _supportedFeatures;

        public GlyphTypefaceImpl(SKTypeface typeface, FontSimulations fontSimulations)
        {
            _typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));

            Face = new Face(GetTable) { UnitsPerEm = typeface.UnitsPerEm };

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineOffset, out var underlineOffset);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineSize, out var underlineSize);

            _os2Table = OS2Table.Load(this);
            _hhTable = HorizontalHeadTable.Load(this);

            var ascent = 0;
            var descent = 0;
            var lineGap = 0;

            if (_os2Table != null && (_os2Table.FontStyle & OS2Table.FontStyleSelection.USE_TYPO_METRICS) != 0)
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

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)Face.UnitsPerEm,
                Ascent = ascent,
                Descent = descent,
                LineGap = lineGap,
                UnderlinePosition = -underlineOffset,
                UnderlineThickness = underlineSize,
                StrikethroughPosition = -_os2Table?.StrikeoutPosition ?? 0,
                StrikethroughThickness = _os2Table?.StrikeoutSize ?? 0,
                IsFixedPitch = typeface.IsFixedPitch
            };

            GlyphCount = typeface.GlyphCount;

            FontSimulations = fontSimulations;

            var fontWeight = _os2Table != null ? (FontWeight)_os2Table.WeightClass : FontWeight.Normal;

            Weight = (fontSimulations & FontSimulations.Bold) != 0 ? FontWeight.Bold : fontWeight;

            var style = _os2Table != null ? GetFontStyle(_os2Table.FontStyle) : FontStyle.Normal;

            Style = (fontSimulations & FontSimulations.Oblique) != 0 ? FontStyle.Italic : style;

            var stretch = _os2Table != null ? (FontStretch)_os2Table.WidthClass : FontStretch.Normal;

            Stretch = stretch;

            _nameTable = NameTable.Load(this);

            //Rely on Skia if no name table is present
            FamilyName = _nameTable?.FontFamilyName((ushort)CultureInfo.InvariantCulture.LCID) ?? typeface.FamilyName;

            TypographicFamilyName = _nameTable?.GetNameById((ushort)CultureInfo.InvariantCulture.LCID, KnownNameIds.TypographicFamilyName) ?? FamilyName;

            if(_nameTable != null)
            {
                var familyNames = new Dictionary<ushort, string>(1);
                var faceNames = new Dictionary<ushort, string>(1);

                foreach (var nameRecord in _nameTable)
                {
                    if(nameRecord.NameID == KnownNameIds.FontFamilyName)
                    {
                        if (nameRecord.Platform != PlatformIDs.Windows || nameRecord.LanguageID == 0)
                        {
                            continue;
                        }

                        if (!familyNames.ContainsKey(nameRecord.LanguageID))
                        {
                            familyNames[nameRecord.LanguageID] = nameRecord.Value;
                        }
                    }

                    if(nameRecord.NameID == KnownNameIds.FontSubfamilyName)
                    {
                        if (nameRecord.Platform != PlatformIDs.Windows || nameRecord.LanguageID == 0)
                        {
                            continue;
                        }

                        if (!faceNames.ContainsKey(nameRecord.LanguageID))
                        {
                            faceNames[nameRecord.LanguageID] = nameRecord.Value;
                        }
                    }
                }

                FamilyNames = familyNames;
                FaceNames = faceNames;
            }
            else
            {
                FamilyNames = new Dictionary<ushort, string> { { (ushort)CultureInfo.InvariantCulture.LCID, FamilyName } };
                FaceNames = new Dictionary<ushort, string> { { (ushort)CultureInfo.InvariantCulture.LCID, Weight.ToString() } };
            }
        }

        public string TypographicFamilyName { get; }

        public IReadOnlyDictionary<ushort, string> FamilyNames { get; }

        public IReadOnlyDictionary<ushort, string> FaceNames { get; }

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

        public Face Face { get; }

        public Font Font { get; }

        public FontSimulations FontSimulations { get; }

        public int ReplacementCodepoint { get; }

        public FontMetrics Metrics { get; }

        public int GlyphCount { get; }

        public string FamilyName { get; }

        public FontWeight Weight { get; }

        public FontStyle Style { get; }

        public FontStretch Stretch { get; }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = default;

            if (!Font.TryGetGlyphExtents(glyph, out var extents))
            {
                return false;
            }

            metrics = new GlyphMetrics
            {
                XBearing = extents.XBearing,
                YBearing = extents.YBearing,
                Width = extents.Width,
                Height = extents.Height
            };

            return true;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort GetGlyph(uint codepoint)
        {
            if (Font.TryGetGlyph(codepoint, out var glyph))
            {
                return (ushort)glyph;
            }

            return 0;
        }

        public bool TryGetGlyph(uint codepoint, out ushort glyph)
        {
            glyph = GetGlyph(codepoint);

            return glyph != 0;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            var glyphs = new ushort[codepoints.Length];

            for (var i = 0; i < codepoints.Length; i++)
            {
                if (Font.TryGetGlyph(codepoints[i], out var glyph))
                {
                    glyphs[i] = (ushort)glyph;
                }
            }

            return glyphs;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public int GetGlyphAdvance(ushort glyph)
        {
            return Font.GetHorizontalGlyphAdvance(glyph);
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new uint[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphIndices[i] = glyphs[i];
            }

            return Font.GetHorizontalGlyphAdvances(glyphIndices);
        }

        private static FontStyle GetFontStyle(OS2Table.FontStyleSelection styleSelection)
        {
            if((styleSelection & OS2Table.FontStyleSelection.ITALIC) != 0)
            {
                return FontStyle.Italic;
            }

            if((styleSelection & OS2Table.FontStyleSelection.OBLIQUE) != 0)
            {
                return FontStyle.Oblique;
            }

            return FontStyle.Normal;
        }

        private Blob? GetTable(Face face, Tag tag)
        {
            var size = _typeface.GetTableSize(tag);

            var data = Marshal.AllocCoTaskMem(size);

            var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeCoTaskMem(data));

            return _typeface.TryGetTableData(tag, 0, size, data) ?
                new Blob(data, size, MemoryMode.ReadOnly, releaseDelegate) : null;
        }

        public SKFont CreateSKFont(float size)
            => new(_typeface, size, skewX: (FontSimulations & FontSimulations.Oblique) != 0 ? -0.3f : 0.0f)
            {
                LinearMetrics = true,
                Embolden = (FontSimulations & FontSimulations.Bold) != 0
            };

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

            Font.Dispose();
            Face.Dispose();
            _typeface.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool TryGetTable(uint tag, out byte[] table)
        {
            return _typeface.TryGetTableData(tag, out table);
        }

        public bool TryGetStream([NotNullWhen(true)] out Stream? stream)
        {
            try
            {
                var asset = _typeface.OpenStream();
                var size = asset.Length;
                var buffer = new byte[size];

                asset.Read(buffer, size);

                stream = new MemoryStream(buffer);

                return true;
            }
            catch
            {
                stream = null;

                return false;
            }
        }
    }
}
