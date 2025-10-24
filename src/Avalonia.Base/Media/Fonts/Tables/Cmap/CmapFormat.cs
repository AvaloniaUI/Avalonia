namespace Avalonia.Media.Fonts.Tables.Cmap
{
    // cmap format types
    internal enum CmapFormat : ushort
    {
        Format0 = 0,   // Byte encoding table
        Format2 = 2,   // High-byte mapping through table (multi-byte charsets)
        Format4 = 4,   // Segment mapping to delta values (most common)
        Format6 = 6,   // Trimmed table mapping
        Format8 = 8,   // Mixed 16/32-bit coverage
        Format10 = 10, // Trimmed array mapping (32-bit)
        Format12 = 12, // Segmented coverage (32-bit)
        Format13 = 13, // Many-to-one mappings
        Format14 = 14,  // Unicode Variation Sequences
    }
}
