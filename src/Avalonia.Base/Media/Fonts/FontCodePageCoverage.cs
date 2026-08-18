using System;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Codepage coverage flags advertised by a font through the OpenType
    /// <c>OS/2.ulCodePageRange1</c> and <c>ulCodePageRange2</c> bitfields.
    /// </summary>
    /// <remarks>
    /// Bit positions match the OpenType specification verbatim. Bits 0..31 are
    /// sourced from <c>ulCodePageRange1</c> and bits 32..63 from
    /// <c>ulCodePageRange2</c>, so the full 64-bit value can be tested with a
    /// single mask. See
    /// <see href="https://learn.microsoft.com/typography/opentype/spec/os2#ulcodepagerange1-bits-031">
    /// the OpenType OS/2 spec
    /// </see>.
    /// </remarks>
    [Flags]
    public enum FontCodePageCoverage : ulong
    {
        /// <summary>No codepage coverage information is advertised.</summary>
        None = 0UL,

        // OS/2 ulCodePageRange1 — bits 0..31.

        /// <summary>1252 Latin 1.</summary>
        Latin1 = 1UL << 0,
        /// <summary>1250 Latin 2: Eastern Europe.</summary>
        Latin2EasternEurope = 1UL << 1,
        /// <summary>1251 Cyrillic.</summary>
        Cyrillic = 1UL << 2,
        /// <summary>1253 Greek.</summary>
        Greek = 1UL << 3,
        /// <summary>1254 Turkish.</summary>
        Turkish = 1UL << 4,
        /// <summary>1255 Hebrew.</summary>
        Hebrew = 1UL << 5,
        /// <summary>1256 Arabic.</summary>
        Arabic = 1UL << 6,
        /// <summary>1257 Windows Baltic.</summary>
        WindowsBaltic = 1UL << 7,
        /// <summary>1258 Vietnamese.</summary>
        Vietnamese = 1UL << 8,
        /// <summary>874 Thai.</summary>
        Thai = 1UL << 16,
        /// <summary>932 JIS/Japan.</summary>
        JapaneseJis = 1UL << 17,
        /// <summary>936 Chinese: Simplified (PRC, Singapore).</summary>
        ChineseSimplified = 1UL << 18,
        /// <summary>949 Korean Wansung.</summary>
        KoreanWansung = 1UL << 19,
        /// <summary>950 Chinese: Traditional (Taiwan, Hong Kong SAR).</summary>
        ChineseTraditional = 1UL << 20,
        /// <summary>1361 Korean Johab.</summary>
        KoreanJohab = 1UL << 21,
        /// <summary>Macintosh Character Set (US Roman).</summary>
        MacRoman = 1UL << 29,
        /// <summary>OEM Character Set.</summary>
        Oem = 1UL << 30,
        /// <summary>Symbol Character Set.</summary>
        Symbol = 1UL << 31,

        // OS/2 ulCodePageRange2 — bits 32..63 (legacy DOS code pages).

        /// <summary>869 IBM Greek.</summary>
        Ibm869 = 1UL << 48,
        /// <summary>866 MS-DOS Russian.</summary>
        Msdos866 = 1UL << 49,
        /// <summary>865 MS-DOS Nordic.</summary>
        Msdos865 = 1UL << 50,
        /// <summary>864 Arabic.</summary>
        Arabic864 = 1UL << 51,
        /// <summary>863 MS-DOS Canadian French.</summary>
        Msdos863 = 1UL << 52,
        /// <summary>862 Hebrew.</summary>
        Hebrew862 = 1UL << 53,
        /// <summary>861 MS-DOS Icelandic.</summary>
        Msdos861 = 1UL << 54,
        /// <summary>860 MS-DOS Portuguese.</summary>
        Msdos860 = 1UL << 55,
        /// <summary>857 IBM Turkish.</summary>
        IbmTurkish857 = 1UL << 56,
        /// <summary>855 IBM Cyrillic; primarily Russian.</summary>
        IbmCyrillic855 = 1UL << 57,
        /// <summary>852 Latin 2.</summary>
        Latin2_852 = 1UL << 58,
        /// <summary>775 MS-DOS Baltic.</summary>
        Msdos775 = 1UL << 59,
        /// <summary>737 Greek (formerly 437G).</summary>
        Greek737 = 1UL << 60,
        /// <summary>708 Arabic ASMO 708.</summary>
        Arabic708 = 1UL << 61,
        /// <summary>850 WE/Latin 1.</summary>
        WeLatin1_850 = 1UL << 62,
        /// <summary>437 US.</summary>
        Us437 = 1UL << 63,
    }
}
