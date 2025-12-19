// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;

namespace Avalonia.Media.Fonts.Tables
{
    internal sealed class OS2Table
    {
        internal const string TableName = "OS/2";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public OS2Table(
            ushort weightClass,
            ushort widthClass,
            short strikeoutSize,
            short strikeoutPosition,
            FontSelectionFlags fontStyle,
            short typoAscender,
            short typoDescender,
            short typoLineGap,
            ushort winAscent,
            ushort winDescent)
        {
            WeightClass = weightClass;
            WidthClass = widthClass;
            StrikeoutSize = strikeoutSize;
            StrikeoutPosition = strikeoutPosition;
            Selection = fontStyle;
            TypoAscender = typoAscender;
            TypoDescender = typoDescender;
            TypoLineGap = typoLineGap;
            WinAscent = winAscent;
            WinDescent = winDescent;
        }

        [Flags]
        internal enum FontSelectionFlags : ushort
        {
            /// <summary>
            /// Font contains italic or oblique characters, otherwise they are upright.
            /// </summary>
            ITALIC = 1,

            /// <summary>
            /// Characters are underscored.
            /// </summary>
            UNDERSCORE = 1 << 1,

            /// <summary>
            /// Characters have their foreground and background reversed.
            /// </summary>
            NEGATIVE = 1 << 2,

            /// <summary>
            /// characters, otherwise they are solid.
            /// </summary>
            OUTLINED = 1 << 3,

            /// <summary>
            /// Characters are overstruck.
            /// </summary>
            STRIKEOUT = 1 << 4,

            /// <summary>
            /// Characters are emboldened.
            /// </summary>
            BOLD = 1 << 5,

            /// <summary>
            /// Characters are in the standard weight/style for the font.
            /// </summary>
            REGULAR = 1 << 6,

            /// <summary>
            /// If set, it is strongly recommended to use OS/2.typoAscender - OS/2.typoDescender+ OS/2.typoLineGap as a value for default line spacing for this font.
            /// </summary>
            USE_TYPO_METRICS = 1 << 7,

            /// <summary>
            /// The font has 'name' table strings consistent with a weight/width/slope family without requiring use of 'name' IDs 21 and 22. (Please see more detailed description below.)
            /// </summary>
            WWS = 1 << 8,

            /// <summary>
            /// Font contains oblique characters.
            /// </summary>
            OBLIQUE = 1 << 9,

            // 10–15        <reserved>  Reserved; set to 0.
        }

        public FontSelectionFlags Selection { get; }

        public short TypoAscender { get; }

        public short TypoDescender { get; }

        public short TypoLineGap { get; }

        public ushort WinAscent { get; }

        public ushort WinDescent { get; }

        public short StrikeoutPosition { get; }

        public short StrikeoutSize { get; }

        public ushort WeightClass { get; }

        public ushort WidthClass { get; }

        public static OS2Table? Load(IGlyphTypeface fontFace)
        {
            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return null;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            return Load(ref binaryReader);
        }

        private static OS2Table Load(ref BigEndianBinaryReader reader)
        {
            // Version 1.0
            // Type   | Name                   | Comments
            // -------|------------------------|-----------------------
            // uint16 |version                 | 0x0005
            // int16  |xAvgCharWidth           |
            // uint16 |usWeightClass           |
            // uint16 |usWidthClass            |
            // uint16 |fsType                  |
            // int16  |ySubscriptXSize         |
            // int16  |ySubscriptYSize         |
            // int16  |ySubscriptXOffset       |
            // int16  |ySubscriptYOffset       |
            // int16  |ySuperscriptXSize       |
            // int16  |ySuperscriptYSize       |
            // int16  |ySuperscriptXOffset     |
            // int16  |ySuperscriptYOffset     |
            // int16  |yStrikeoutSize          |
            // int16  |yStrikeoutPosition      |
            // int16  |sFamilyClass            |
            // uint8  |panose[10]              |
            // uint32 |ulUnicodeRange1         | Bits 0–31
            // uint32 |ulUnicodeRange2         | Bits 32–63
            // uint32 |ulUnicodeRange3         | Bits 64–95
            // uint32 |ulUnicodeRange4         | Bits 96–127
            // Tag    |achVendID               |
            // uint16 |fsSelection             |
            // uint16 |usFirstCharIndex        |
            // uint16 |usLastCharIndex         |
            // int16  |sTypoAscender           |
            // int16  |sTypoDescender          |
            // int16  |sTypoLineGap            |
            // uint16 |usWinAscent             |
            // uint16 |usWinDescent            |
            // uint32 |ulCodePageRange1        | Bits 0–31
            // uint32 |ulCodePageRange2        | Bits 32–63
            // int16  |sxHeight                |
            // int16  |sCapHeight              |
            // uint16 |usDefaultChar           |
            // uint16 |usBreakChar             |
            // uint16 |usMaxContext            |
            // uint16 |usLowerOpticalPointSize |
            // uint16 |usUpperOpticalPointSize |
            reader.ReadUInt16(); // version
            reader.ReadInt16(); // averageCharWidth
            ushort weightClass = reader.ReadUInt16();
            ushort widthClass = reader.ReadUInt16();
            reader.ReadUInt16(); // styleType
            reader.ReadInt16(); // subscriptXSize
            reader.ReadInt16(); // subscriptYSize
            reader.ReadInt16(); // subscriptXOffset
            reader.ReadInt16(); // subscriptYOffset

            reader.ReadInt16(); // superscriptXSize
            reader.ReadInt16(); // superscriptYSize
            reader.ReadInt16(); // superscriptXOffset
            reader.ReadInt16(); // superscriptYOffset

            short strikeoutSize = reader.ReadInt16();
            short strikeoutPosition = reader.ReadInt16();
            reader.ReadInt16(); // familyClass
            
            // Skip panose[10] without allocating byte array
            reader.Seek(reader.Position + 10);
            
            // Skip unicode ranges (4 × uint32 = 16 bytes)
            reader.Seek(reader.Position + 16);
            
            // Skip vendor tag (4 bytes)
            reader.Seek(reader.Position + 4);
            
            FontSelectionFlags fontStyle = reader.ReadUInt16<FontSelectionFlags>();
            reader.ReadUInt16(); // firstCharIndex
            reader.ReadUInt16(); // lastCharIndex
            short typoAscender = reader.ReadInt16();
            short typoDescender = reader.ReadInt16();
            short typoLineGap = reader.ReadInt16();
            ushort winAscent = reader.ReadUInt16();
            ushort winDescent = reader.ReadUInt16();

            return new OS2Table(
                    weightClass,
                    widthClass,
                    strikeoutSize,
                    strikeoutPosition,
                    fontStyle,
                    typoAscender,
                    typoDescender,
                    typoLineGap,
                    winAscent,
                    winDescent);
        }
    }
}
