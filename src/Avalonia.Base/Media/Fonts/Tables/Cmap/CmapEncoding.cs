namespace Avalonia.Media.Fonts.Tables.Cmap
{
    // Encoding IDs. The meaning depends on the platform; common values are listed here.
    internal enum CmapEncoding : ushort
    {
        // Unicode platform encodings
        Unicode_1_0 = 0,
        Unicode_1_1 = 1,
        Unicode_ISO_10646 = 2,
        Unicode_2_0_BMP = 3,
        Unicode_2_0_full = 4,

        // Macintosh encodings (selected)
        Macintosh_Roman = 0,
        Macintosh_Japanese = 1,
        Macintosh_ChineseTraditional = 2,
        Macintosh_Korean = 3,
        Macintosh_Arabic = 4,
        Macintosh_Hebrew = 5,
        Macintosh_Greek = 6,
        Macintosh_Russian = 7,
        Macintosh_RSymbol = 8,

        // Microsoft encodings
        Microsoft_Symbol = 0,
        Microsoft_UnicodeBMP = 1, // UCS-2 / UTF-16 (BMP)
        Microsoft_ShiftJIS = 2,
        Microsoft_PRChina = 3,
        Microsoft_Big5 = 4,
        Microsoft_Wansung = 5,
        Microsoft_Johab = 6,
        Microsoft_UCS4 = 10 // UTF-32 (format 12)
    }
}
