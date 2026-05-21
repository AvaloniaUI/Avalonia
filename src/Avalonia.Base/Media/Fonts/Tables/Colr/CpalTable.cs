using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Reader for the 'CPAL' (Color Palette) table. Provides access to color palettes used by COLR glyphs.
    /// </summary>
    internal sealed class CpalTable
    {
        internal const string TableName = "CPAL";

        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private readonly ReadOnlyMemory<byte> _cpalData;
        private readonly ushort _version;
        private readonly ushort _numPaletteEntries;
        private readonly ushort _numPalettes;
        private readonly ushort _numColorRecords;
        private readonly uint _colorRecordsArrayOffset;

        private CpalTable(
            ReadOnlyMemory<byte> cpalData,
            ushort version,
            ushort numPaletteEntries,
            ushort numPalettes,
            ushort numColorRecords,
            uint colorRecordsArrayOffset)
        {
            _cpalData = cpalData;
            _version = version;
            _numPaletteEntries = numPaletteEntries;
            _numPalettes = numPalettes;
            _numColorRecords = numColorRecords;
            _colorRecordsArrayOffset = colorRecordsArrayOffset;
        }

        /// <summary>
        /// Gets the version of the CPAL table.
        /// </summary>
        public ushort Version => _version;

        /// <summary>
        /// Gets the number of palette entries in each palette.
        /// </summary>
        public int PaletteEntryCount => _numPaletteEntries;

        /// <summary>
        /// Gets the number of palettes.
        /// </summary>
        public int PaletteCount => _numPalettes;

        /// <summary>
        /// Attempts to load the CPAL (Color Palette) table from the specified glyph typeface.
        /// </summary>
        /// <remarks>This method supports CPAL table versions 0 and 1. If the glyph typeface does not
        /// contain a valid CPAL table, or if the table version is not supported, the method returns false and sets
        /// cpalTable to null.</remarks>
        /// <param name="glyphTypeface">The glyph typeface from which to load the CPAL table. Cannot be null.</param>
        /// <param name="cpalTable">When this method returns, contains the loaded CPAL table if successful; otherwise, null. This parameter is
        /// passed uninitialized.</param>
        /// <returns>true if the CPAL table was successfully loaded; otherwise, false.</returns>
        public static bool TryLoad(GlyphTypeface glyphTypeface, [NotNullWhen(true)] out CpalTable? cpalTable)
        {
            cpalTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var cpalData))
            {
                return false;
            }

            if (cpalData.Length < 12)
            {
                return false; // Minimum size for CPAL header
            }

            var span = cpalData.Span;

            // Parse CPAL table header
            // uint16 version
            // uint16 numPaletteEntries
            // uint16 numPalettes
            // uint16 numColorRecords
            // Offset32 colorRecordsArrayOffset

            var version = BinaryPrimitives.ReadUInt16BigEndian(span);
            var numPaletteEntries = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
            var numPalettes = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4));
            var numColorRecords = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));
            var colorRecordsArrayOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));

            // Currently support CPAL v0 and v1
            if (version > 1)
            {
                return false;
            }

            // Validate offset
            if (colorRecordsArrayOffset >= cpalData.Length)
            {
                return false;
            }

            cpalTable = new CpalTable(
                cpalData,
                version,
                numPaletteEntries,
                numPalettes,
                numColorRecords,
                colorRecordsArrayOffset);

            return true;
        }

        /// <summary>
        /// Gets the offset to the first color record for the specified palette index.
        /// </summary>
        private bool TryGetPaletteOffset(int paletteIndex, out int firstColorIndex)
        {
            firstColorIndex = 0;

            if (paletteIndex < 0 || paletteIndex >= _numPalettes)
            {
                return false;
            }

            var span = _cpalData.Span;

            // The colorRecordIndices array starts at offset 12 (after the header)
            // Each entry is uint16
            int offsetTableOffset = 12 + (paletteIndex * 2);

            if (offsetTableOffset + 2 > span.Length)
            {
                return false;
            }

            firstColorIndex = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offsetTableOffset, 2));
            return true;
        }

        /// <summary>
        /// Tries to get the color at the specified palette index and color index.
        /// </summary>
        /// <param name="paletteIndex">The palette index (0-based).</param>
        /// <param name="colorIndex">The color index within the palette (0-based).</param>
        /// <param name="color">The resulting color.</param>
        /// <returns>True if the color was successfully retrieved; otherwise, false.</returns>
        public bool TryGetColor(int paletteIndex, int colorIndex, out Color color)
        {
            color = default;

            if (!TryGetPaletteOffset(paletteIndex, out var firstColorIndex))
            {
                return false;
            }

            if (colorIndex < 0 || colorIndex >= _numPaletteEntries)
            {
                return false;
            }

            var actualColorIndex = firstColorIndex + colorIndex;

            if (actualColorIndex >= _numColorRecords)
            {
                return false;
            }

            var span = _cpalData.Span;

            // Each color record is 4 bytes: BGRA
            int offset = (int)_colorRecordsArrayOffset + (actualColorIndex * 4);

            if (offset + 4 > span.Length)
            {
                return false;
            }

            var colorSpan = span.Slice(offset, 4);

            // Colors are stored as BGRA (little-endian uint32 when viewed as 0xAARRGGBB)
            var b = colorSpan[0];
            var g = colorSpan[1];
            var r = colorSpan[2];
            var a = colorSpan[3];

            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        /// <summary>
        /// Gets all colors in the specified palette.
        /// Returns an empty array if the palette index is invalid.
        /// </summary>
        public Color[] GetPalette(int paletteIndex)
        {
            if (!TryGetPaletteOffset(paletteIndex, out var firstColorIndex))
            {
                return Array.Empty<Color>();
            }

            var colors = new Color[_numPaletteEntries];

            for (int i = 0; i < _numPaletteEntries; i++)
            {
                if (TryGetColor(paletteIndex, i, out var color))
                {
                    colors[i] = color;
                }
                else
                {
                    colors[i] = Colors.Black; // Fallback
                }
            }

            return colors;
        }

        /// <summary>
        /// Tries to get a color from the default palette (palette 0).
        /// </summary>
        public bool TryGetColor(int colorIndex, out Color color)
        {
            return TryGetColor(0, colorIndex, out color);
        }
    }
}
