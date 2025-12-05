using System;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    // Representation of a subtable entry in the 'cmap' table directory
    internal readonly record struct CmapSubtableEntry
    {
        public CmapSubtableEntry(PlatformID platform, CmapEncoding encoding, int offset, CmapFormat format) : this()
        {
            Platform = platform;
            Encoding = encoding;
            Offset = offset;
            Format = format;
        }

        /// <summary>
        /// Gets the platform identifier for the current environment.
        /// </summary>
        public PlatformID Platform { get; init; }

        /// <summary>
        /// Gets the character map (CMap) encoding associated with this instance.
        /// </summary>
        /// 
        public CmapEncoding Encoding { get; init; }

        /// <summary>
        /// Gets the offset of the sub table.
        /// </summary>
        public int Offset { get; init; }

        /// <summary>
        /// Gets the format of the character-to-glyph mapping (cmap) table.
        /// </summary>
        public CmapFormat Format { get; init; }

        public ReadOnlyMemory<byte> GetSubtableMemory(ReadOnlyMemory<byte> table)
        {
            return table.Slice(Offset);
        }
    }
}
