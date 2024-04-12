using System;
using System.Buffers;
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

        private static uint GetIntTag(string v)
        {
            return (uint)v[0] << 24 | (uint)v[1] << 16 | (uint)v[2] << 08 | (uint)v[3] << 00;
        }

        public static bool TryReadFontMetrics(SKTypeface typeface, out int Ascent, out int Descent, out int LineGap)
        {
            // See: https://learn.microsoft.com/en-us/typography/opentype/spec/recom#baseline-to-baseline-distances
            if (typeface.TryGetTableData(GetIntTag("OS/2"), out byte[]? os2Table) && 
                typeface.TryGetTableData(GetIntTag("hhea"), out byte[]? hheaTable))
            {
                if (ReadOS2Table(os2Table, out int usWinAscent, out int usWinDescent) && 
                    ReadHHEATable(hheaTable, out int ascender, out int descender, out int lineGap) &&
                    usWinAscent > 0 && usWinDescent > 0)
                {
                    Ascent = usWinAscent;
                    Descent = usWinDescent;
                    LineGap = Math.Max(0, (ascender - descender + lineGap) - (usWinAscent + usWinDescent));
                    return true;
                }
            }

            Ascent = Descent = LineGap = 0;
            return false;
        }

        public GlyphTypefaceImpl(SKTypeface typeface, FontSimulations fontSimulations)
        {
            _typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));

            var hasOs2Metrics = TryReadFontMetrics(typeface, out int FMAscent, out int FMDescent, out int FMLineGap);

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
                Ascent = hasOs2Metrics ? (-FMAscent) : (-ascent), // have to invert OS2 ascent here for some reason
                Descent = hasOs2Metrics ? FMDescent : (-descent),
                LineGap = hasOs2Metrics ? FMLineGap : (lineGap),
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

        private static bool ReadOS2Table(byte[]? array, out int usWinAscent, out int usWinDescent)
        {
            if (array == null || array.Length < 78)
            {
                usWinAscent = 0;
                usWinDescent = 0;
                return false;
            }

            using (MemoryStream stream = new MemoryStream(array))
            {
                // The offset to get to the usWinAscent field in the table
                stream.Seek(74, SeekOrigin.Begin);
                usWinAscent = ReadU2BE(stream);
                usWinDescent = ReadU2BE(stream);

                return true;
            }
        }

        private static bool ReadHHEATable(byte[]? array, out int ascender, out int descender, out int lineGap)
        {
            if (array == null || array.Length < 8)
            {
                ascender = descender = lineGap = 0;
                return false;
            }

            using (MemoryStream stream = new MemoryStream(array))
            {
                // The offset to get to the ascender field in the table
                stream.Seek(4, SeekOrigin.Begin);
                ascender = ReadU2BE(stream);
                descender = ReadU2BE(stream);
                lineGap = ReadU2BE(stream);

                return true;
            }
        }

        // Helper method to read a big endian UInt16
        private static ushort ReadU2BE(Stream stream)
        {
            var buf = ArrayPool<byte>.Shared.Rent(2);
            try
            {
                int numRead = stream.Read(buf, 0, 2);
                if (numRead != 2)
                {
                    throw new IOException("Could not read 2 bytes for UInt16");
                }

                return BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(buf, 0, numRead));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}
