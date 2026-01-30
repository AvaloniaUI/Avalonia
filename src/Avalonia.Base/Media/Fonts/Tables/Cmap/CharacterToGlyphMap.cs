using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    /// <summary>
    /// Provides a read-only mapping from Unicode code points to glyph identifiers for a font's character map (cmap)
    /// table.
    /// </summary>
    /// <remarks>This struct enables efficient lookup of glyph IDs corresponding to Unicode code points,
    /// supporting both Format 4 (BMP) and Format 12 (Unicode full repertoire) cmap subtables. 
    /// </remarks>
#pragma warning disable CA1815 // Override equals not needed for readonly struct
    public readonly struct CharacterToGlyphMap
#pragma warning restore CA1815 // Override equals not needed for readonly struct
    {
        private readonly CmapFormat _format;
        private readonly CmapFormat4Table? _format4;
        private readonly CmapFormat12Table? _format12;

        /// <summary>
        /// Initializes a new instance of the CharacterToGlyphMap class using the specified Format 4 cmap table.
        /// </summary>
        /// <param name="table">The Format 4 cmap table that provides character-to-glyph mapping data. Cannot be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CharacterToGlyphMap(CmapFormat4Table table)
        {
            _format = CmapFormat.Format4;
            _format4 = table;
            _format12 = null;
        }

        /// <summary>
        /// Initializes a new instance of the CharacterToGlyphMap class using the specified Format 12 character-to-glyph
        /// mapping table.
        /// </summary>
        /// <param name="table">The Format 12 cmap table that defines the mapping from Unicode code points to glyph indices. Cannot be null.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CharacterToGlyphMap(CmapFormat12Table table)
        {
            _format = CmapFormat.Format12;
            _format12 = table;
            _format4 = null;
        }

        /// <summary>
        /// Gets the glyph index associated with the specified Unicode code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point for which to retrieve the glyph index.</param>
        /// <returns>The glyph index corresponding to the specified code point.</returns>
        public ushort this[int codePoint]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetGlyph(codePoint);
        }

        /// <summary>
        /// Retrieves the glyph index that corresponds to the specified Unicode code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point for which to obtain the glyph index.</param>
        /// <returns>The glyph index associated with the specified code point. Returns 0 if the code point is not mapped to any
        /// glyph.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetGlyph(int codePoint)
        {
            return _format switch
            {
                CmapFormat.Format4 => _format4!.GetGlyph(codePoint),
                CmapFormat.Format12 => _format12!.GetGlyph(codePoint),
                _ => 0
            };
        }

        /// <summary>
        /// Determines whether the character map contains a glyph for the specified Unicode code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point to check for the presence of a corresponding glyph.</param>
        /// <returns>true if a glyph exists for the specified code point; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsGlyph(int codePoint)
        {
            return _format switch
            {
                CmapFormat.Format4 => _format4!.ContainsGlyph(codePoint),
                CmapFormat.Format12 => _format12!.ContainsGlyph(codePoint),
                _ => false
            };
        }

        /// <summary>
        /// Maps a sequence of Unicode code points to their corresponding glyph IDs using the current character mapping
        /// format.
        /// </summary>
        /// <remarks>If the current character mapping format is not supported, all entries in <paramref
        /// name="glyphIds"/> are set to zero. The mapping is performed in place, and the method does not allocate
        /// additional memory.</remarks>
        /// <param name="codePoints">A read-only span of Unicode code points to be mapped to glyph IDs.</param>
        /// <param name="glyphIds">A span in which the resulting glyph IDs are written. Must be at least as long as <paramref
        /// name="codePoints"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetGlyphs(ReadOnlySpan<int> codePoints, Span<ushort> glyphIds)
        {
            switch (_format)
            {
                case CmapFormat.Format4:
                    _format4!.GetGlyphs(codePoints, glyphIds);
                    return;
                case CmapFormat.Format12:
                    _format12!.GetGlyphs(codePoints, glyphIds);
                    return;
                default:
                    glyphIds.Clear();
                    return;
            }
        }
        

        /// <summary>
        /// Attempts to retrieve the glyph identifier corresponding to the specified Unicode code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point for which to obtain the glyph identifier.</param>
        /// <param name="glyphId">When this method returns, contains the glyph identifier associated with the specified code point, if found;
        /// otherwise, zero. This parameter is passed uninitialized.</param>
        /// <returns>true if a glyph identifier was found for the specified code point; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public bool TryGetGlyph(int codePoint, out ushort glyphId) 
        { 
            switch (_format) 
            { 
                case CmapFormat.Format4: return _format4!.TryGetGlyph(codePoint, out glyphId); 
                case CmapFormat.Format12: return _format12!.TryGetGlyph(codePoint, out glyphId);
                default: glyphId = 0; return false; 
            } 
        }

        /// <summary>
        /// Returns an enumerator that iterates through all code point ranges mapped by this instance.
        /// </summary>
        /// <returns>A <see cref="CodepointRangeEnumerator"/> that can be used to enumerate the mapped code point ranges.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public CodepointRangeEnumerator GetMappedRanges() 
        { 
            return new CodepointRangeEnumerator(_format, _format4, _format12);
        }
    }
}
