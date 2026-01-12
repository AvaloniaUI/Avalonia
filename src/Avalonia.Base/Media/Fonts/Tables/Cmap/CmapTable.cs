using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    /// <summary>
    /// Represents the 'cmap' table in an OpenType font, which maps character codes to glyph indices.
    /// </summary>
    /// <remarks>The 'cmap' table is a critical component of an OpenType font, enabling the mapping of
    /// character codes (e.g., Unicode) to glyph indices used for rendering text. This class provides functionality to
    /// load and parse the 'cmap' table from a font's platform-specific typeface.</remarks>
    internal sealed class CmapTable
    {
        internal const string TableName = "cmap";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public static CharacterToGlyphMap Load(GlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                throw new InvalidOperationException("No cmap table found.");
            }

            var reader = new BigEndianBinaryReader(table.Span);

            reader.ReadUInt16(); // version

            var numTables = reader.ReadUInt16();

            var entries = new CmapSubtableEntry[numTables];

            for (var i = 0; i < numTables; i++)
            {
                var platformID = (PlatformID)reader.ReadUInt16();
                var encodingID = (CmapEncoding)reader.ReadUInt16();
                var offset = (int)reader.ReadUInt32();

                var position = reader.Position;

                reader.Seek(offset);

                var format = (CmapFormat)reader.ReadUInt16();

                reader.Seek(position);

                var entry = new CmapSubtableEntry(platformID, encodingID, offset, format);

                entries[i] = entry;
            }

            // Try to find the best Format 12 subtable entry
            if (TryFindFormat12Entry(entries, out var format12Entry))
            {
                // Prefer Format 12 if available
                return new CharacterToGlyphMap(new CmapFormat12Table(format12Entry.GetSubtableMemory(table)));
            }

            // Fallback to Format 4
            if (TryFindFormat4Entry(entries, out var format4Entry))
            {
                return new CharacterToGlyphMap(new CmapFormat4Table(format4Entry.GetSubtableMemory(table)));
            }

            throw new InvalidOperationException("No suitable cmap subtable found.");

            // Tries to find the best Format 12 subtable entry based on platform and encoding preferences
            static bool TryFindFormat12Entry(CmapSubtableEntry[] entries, out CmapSubtableEntry result)
            {
                result = default;
                var foundPlatformScore = int.MaxValue;
                var foundEncodingScore = int.MaxValue;

                foreach (var entry in entries)
                {
                    if (entry.Format != CmapFormat.Format12)
                    {
                        continue;
                    }

                    var platformScore = entry.Platform switch
                    {
                        PlatformID.Unicode => 0,
                        PlatformID.Windows => 1,
                        _ => 2
                    };

                    var encodingScore = 2; // Default: lowest preference

                    switch (entry.Platform)
                    {
                        case PlatformID.Unicode when entry.Encoding == CmapEncoding.Unicode_2_0_full:
                            encodingScore = 0; // non-BMP preferred
                            break;
                        case PlatformID.Unicode when entry.Encoding == CmapEncoding.Unicode_2_0_BMP:
                            encodingScore = 1; // BMP
                            break;
                        case PlatformID.Windows when entry.Encoding == CmapEncoding.Microsoft_UCS4 && platformScore != 0:
                            encodingScore = 0; // non-BMP preferred
                            break;
                        case PlatformID.Windows when entry.Encoding == CmapEncoding.Microsoft_UnicodeBMP && platformScore != 0:
                            encodingScore = 1; // BMP
                            break;
                    }

                    if (encodingScore < foundEncodingScore || encodingScore == foundEncodingScore && platformScore < foundPlatformScore)
                    {
                        result = entry;
                        foundEncodingScore = encodingScore;
                        foundPlatformScore = platformScore;
                    }
                    else
                    {
                        if (platformScore < foundPlatformScore)
                        {
                            result = entry;
                            foundEncodingScore = encodingScore;
                            foundPlatformScore = platformScore;
                        }
                    }

                    if (foundPlatformScore == 0 && foundEncodingScore == 0)
                    {
                        break; // Best possible match found
                    }
                }

                return result.Format != CmapFormat.Format0;
            }

            // Tries to find the best Format 4 subtable entry based on platform preferences
            static bool TryFindFormat4Entry(CmapSubtableEntry[] entries, out CmapSubtableEntry result)
            {
                result = default;
                var foundPlatformScore = int.MaxValue;

                foreach (var entry in entries)
                {
                    if (entry.Format != CmapFormat.Format4)
                    {
                        continue;
                    }

                    var platformScore = entry.Platform switch
                    {
                        PlatformID.Unicode => 0,
                        PlatformID.Windows => 1,
                        _ => 2
                    };

                    if (platformScore < foundPlatformScore)
                    {
                        result = entry;
                        foundPlatformScore = platformScore;
                    }

                    if (foundPlatformScore == 0)
                    {
                        break; // Best possible match found
                    }
                }

                return result.Format != CmapFormat.Format0;
            }
        }
    }
}
