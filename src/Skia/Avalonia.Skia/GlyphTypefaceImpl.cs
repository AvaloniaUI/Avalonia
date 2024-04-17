using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;
using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypeface, IGlyphTypeface2
    {
        private bool _isDisposed;
        private readonly SKTypeface _typeface;

        public GlyphTypefaceImpl(SKTypeface typeface, FontSimulations fontSimulations)
        {
            _typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
            var hasOs2Metrics = TryReadFontMetrics(typeface, out int fmAscent, out int fmDescent, out int fmLineGap);

            Face = new Face(GetTable)
            {
                UnitsPerEm = typeface.UnitsPerEm
            };

            Font = new Font(Face);

            Font.SetFunctionsOpenType();

            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.HorizontalAscender, out var ascent);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.HorizontalDescender, out var descent);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.HorizontalLineGap, out var lineGap);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.StrikeoutOffset, out var strikethroughOffset);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.StrikeoutSize, out var strikethroughSize);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineOffset, out var underlineOffset);
            Font.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.UnderlineSize, out var underlineSize);

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)Face.UnitsPerEm,
                Ascent = hasOs2Metrics ? (-fmAscent) : (-ascent), // have to invert OS2 ascent here for some reason
                Descent = hasOs2Metrics ? (fmDescent) : (-descent),
                LineGap = hasOs2Metrics ? fmLineGap : (lineGap),
                UnderlinePosition = -underlineOffset,
                UnderlineThickness = underlineSize,
                StrikethroughPosition = -strikethroughOffset,
                StrikethroughThickness = strikethroughSize,
                IsFixedPitch = typeface.IsFixedPitch
            };

            GlyphCount = typeface.GlyphCount;

            FontSimulations = fontSimulations;

            Weight = (FontWeight)typeface.FontWeight;

            Style = typeface.FontSlant.ToAvalonia();

            Stretch = (FontStretch)typeface.FontStyle.Width;
        }

        public Face Face { get; }

        public Font Font { get; }

        public FontSimulations FontSimulations { get; }

        public int ReplacementCodepoint { get; }

        public FontMetrics Metrics { get; }

        public int GlyphCount { get; }

        public string FamilyName => _typeface.FamilyName;

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

        private static bool TryReadFontMetrics(SKTypeface typeface, out int ascent, out int descent, out int lineGap)
        {
            const int TagOs2 = 1330851634; // pre-computed value for HarfBuzzSharp.Tag.Parse("OS/2")
            const int TagHhea = 1751672161; // pre-computed value for HarfBuzzSharp.Tag.Parse("hhea")

            if (typeface.TryGetTableData(TagHhea, out byte[]? hheaTable) && 
                ReadHHEATable(hheaTable, out int hheaAscender, out int hheaDescender, out int hheaLineGap))
            {
                // See: https://learn.microsoft.com/en-us/typography/opentype/spec/recom#baseline-to-baseline-distances
                // See Also: https://github.com/mono/libgdiplus/blob/94a49875487e296376f209fe64b921c6020f74c0/src/font.c#L757-L792

                if (typeface.TryGetTableData(TagOs2, out byte[]? os2Table) && 
                    ReadOS2Table(os2Table, out var os2))
                {
                    descent = os2.usWinDescent;
                    ascent = os2.usWinAscent;
                    lineGap = Math.Max(0, (hheaAscender - hheaDescender + hheaLineGap) - (os2.usWinAscent + os2.usWinDescent));
                    return true;
                }
            }

            ascent = descent = lineGap = 0;
            return false;
        }

        private static bool ReadOS2Table(byte[]? array, out OS2TableInfo os2)
        {
            os2 = default;
            if (array == null || array.Length < 78)
            {
                return false;
            }

            // 62 is the offset to the fsSelection field in the OS/2 table
            var span = new Span<byte>(array, 62, 16);

            os2.fsSelection = BinaryPrimitives.ReadUInt16BigEndian(span);
            os2.sTypoAscender = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));
            os2.sTypoDescender = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));
            os2.sTypoLineGap = BinaryPrimitives.ReadInt16BigEndian(span.Slice(10));
            os2.usWinAscent = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(12));
            os2.usWinDescent = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(14));
            return true;
        }

        private static bool ReadHHEATable(byte[]? array, out int ascender, out int descender, out int lineGap)
        {
            if (array == null || array.Length < 10) // 10 would be the endIndex (exclusive) of lineGap
            {
                ascender = descender = lineGap = 0;
                return false;
            }

            // 4 is the offset to the hheaAscender field in the hhea table
            var span = new Span<byte>(array, 4, 6);

            ascender = BinaryPrimitives.ReadInt16BigEndian(span);
            descender = BinaryPrimitives.ReadInt16BigEndian(span.Slice(2));
            lineGap = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4));

            return true;
        }

        private struct OS2TableInfo
        {
            public ushort fsSelection;
            public short sTypoAscender;
            public short sTypoDescender;
            public short sTypoLineGap;
            public ushort usWinAscent;
            public ushort usWinDescent;
        }
    }
}
