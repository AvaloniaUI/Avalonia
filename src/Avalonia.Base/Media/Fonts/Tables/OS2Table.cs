// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;

namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct OS2Table
    {
        internal const string TableName = "OS/2";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        [Flags]
        internal enum FontSelectionFlags : ushort
        {
            ITALIC = 1,
            UNDERSCORE = 1 << 1,
            NEGATIVE = 1 << 2,
            OUTLINED = 1 << 3,
            STRIKEOUT = 1 << 4,
            BOLD = 1 << 5,
            REGULAR = 1 << 6,
            USE_TYPO_METRICS = 1 << 7,
            WWS = 1 << 8,
            OBLIQUE = 1 << 9,
        }

        public ushort Version { get; }
        public short XAvgCharWidth { get; }
        public ushort WeightClass { get; }
        public ushort WidthClass { get; }
        public ushort FsType { get; }
        public short YSubscriptXSize { get; }
        public short YSubscriptYSize { get; }
        public short YSubscriptXOffset { get; }
        public short YSubscriptYOffset { get; }
        public short YSuperscriptXSize { get; }
        public short YSuperscriptYSize { get; }
        public short YSuperscriptXOffset { get; }
        public short YSuperscriptYOffset { get; }
        public short StrikeoutSize { get; }
        public short StrikeoutPosition { get; }
        public short FamilyClass { get; }
        public Panose Panose { get; }
        public uint UnicodeRange1 { get; }
        public uint UnicodeRange2 { get; }
        public uint UnicodeRange3 { get; }
        public uint UnicodeRange4 { get; }
        public uint VendorId { get; }
        public FontSelectionFlags Selection { get; }
        public ushort FirstCharIndex { get; }
        public ushort LastCharIndex { get; }
        public short TypoAscender { get; }
        public short TypoDescender { get; }
        public short TypoLineGap { get; }
        public ushort WinAscent { get; }
        public ushort WinDescent { get; }
        
        public uint CodePageRange1 { get; }
        public uint CodePageRange2 { get; }
        
        public short XHeight { get; }
        public short CapHeight { get; }
        public ushort DefaultChar { get; }
        public ushort BreakChar { get; }
        public ushort MaxContext { get; }
        
        public ushort LowerOpticalPointSize { get; }
        public ushort UpperOpticalPointSize { get; }

        private OS2Table(
            ushort version,
            short xAvgCharWidth,
            ushort weightClass,
            ushort widthClass,
            ushort fsType,
            short ySubscriptXSize,
            short ySubscriptYSize,
            short ySubscriptXOffset,
            short ySubscriptYOffset,
            short ySuperscriptXSize,
            short ySuperscriptYSize,
            short ySuperscriptXOffset,
            short ySuperscriptYOffset,
            short strikeoutSize,
            short strikeoutPosition,
            short familyClass,
            Panose panose,
            uint unicodeRange1,
            uint unicodeRange2,
            uint unicodeRange3,
            uint unicodeRange4,
            uint vendorId,
            FontSelectionFlags selection,
            ushort firstCharIndex,
            ushort lastCharIndex,
            short typoAscender,
            short typoDescender,
            short typoLineGap,
            ushort winAscent,
            ushort winDescent,
            uint codePageRange1,
            uint codePageRange2,
            short xHeight,
            short capHeight,
            ushort defaultChar,
            ushort breakChar,
            ushort maxContext,
            ushort lowerOpticalPointSize,
            ushort upperOpticalPointSize)
        {
            Version = version;
            XAvgCharWidth = xAvgCharWidth;
            WeightClass = weightClass;
            WidthClass = widthClass;
            FsType = fsType;
            YSubscriptXSize = ySubscriptXSize;
            YSubscriptYSize = ySubscriptYSize;
            YSubscriptXOffset = ySubscriptXOffset;
            YSubscriptYOffset = ySubscriptYOffset;
            YSuperscriptXSize = ySuperscriptXSize;
            YSuperscriptYSize = ySuperscriptYSize;
            YSuperscriptXOffset = ySuperscriptXOffset;
            YSuperscriptYOffset = ySuperscriptYOffset;
            StrikeoutSize = strikeoutSize;
            StrikeoutPosition = strikeoutPosition;
            FamilyClass = familyClass;
            Panose = panose;
            UnicodeRange1 = unicodeRange1;
            UnicodeRange2 = unicodeRange2;
            UnicodeRange3 = unicodeRange3;
            UnicodeRange4 = unicodeRange4;
            VendorId = vendorId;
            Selection = selection;
            FirstCharIndex = firstCharIndex;
            LastCharIndex = lastCharIndex;
            TypoAscender = typoAscender;
            TypoDescender = typoDescender;
            TypoLineGap = typoLineGap;
            WinAscent = winAscent;
            WinDescent = winDescent;
            CodePageRange1 = codePageRange1;
            CodePageRange2 = codePageRange2;
            XHeight = xHeight;
            CapHeight = capHeight;
            DefaultChar = defaultChar;
            BreakChar = breakChar;
            MaxContext = maxContext;
            LowerOpticalPointSize = lowerOpticalPointSize;
            UpperOpticalPointSize = upperOpticalPointSize;
        }

        public static bool TryLoad(GlyphTypeface fontFace, out OS2Table os2Table)
        {
            os2Table = default;
            
            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return false;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            os2Table = Load(ref binaryReader);

            return true;
        }

        private static OS2Table Load(ref BigEndianBinaryReader reader)
        {
            ushort version = reader.ReadUInt16();
            short xAvgCharWidth = reader.ReadInt16();
            ushort weightClass = reader.ReadUInt16();
            ushort widthClass = reader.ReadUInt16();
            ushort fsType = reader.ReadUInt16();
            short ySubscriptXSize = reader.ReadInt16();
            short ySubscriptYSize = reader.ReadInt16();
            short ySubscriptXOffset = reader.ReadInt16();
            short ySubscriptYOffset = reader.ReadInt16();
            short ySuperscriptXSize = reader.ReadInt16();
            short ySuperscriptYSize = reader.ReadInt16();
            short ySuperscriptXOffset = reader.ReadInt16();
            short ySuperscriptYOffset = reader.ReadInt16();
            short strikeoutSize = reader.ReadInt16();
            short strikeoutPosition = reader.ReadInt16();
            short familyClass = reader.ReadInt16();

            Panose panose = Panose.Load(ref reader);

            uint unicodeRange1 = reader.ReadUInt32();
            uint unicodeRange2 = reader.ReadUInt32();
            uint unicodeRange3 = reader.ReadUInt32();
            uint unicodeRange4 = reader.ReadUInt32();
            
            uint vendorId = reader.ReadUInt32();
            
            FontSelectionFlags selection = reader.ReadUInt16<FontSelectionFlags>();
            ushort firstCharIndex = reader.ReadUInt16();
            ushort lastCharIndex = reader.ReadUInt16();
            short typoAscender = reader.ReadInt16();
            short typoDescender = reader.ReadInt16();
            short typoLineGap = reader.ReadInt16();
            ushort winAscent = reader.ReadUInt16();
            ushort winDescent = reader.ReadUInt16();

            uint codePageRange1 = 0;
            uint codePageRange2 = 0;
            short xHeight = 0;
            short capHeight = 0;
            ushort defaultChar = 0;
            ushort breakChar = 0;
            ushort maxContext = 0;
            ushort lowerOpticalPointSize = 0;
            ushort upperOpticalPointSize = 0xFFFF;

            if (version >= 1)
            {
                codePageRange1 = reader.ReadUInt32();
                codePageRange2 = reader.ReadUInt32();
            }

            if (version >= 2)
            {
                xHeight = reader.ReadInt16();
                capHeight = reader.ReadInt16();
                defaultChar = reader.ReadUInt16();
                breakChar = reader.ReadUInt16();
                maxContext = reader.ReadUInt16();
            }

            if (version >= 5)
            {
                lowerOpticalPointSize = reader.ReadUInt16();
                upperOpticalPointSize = reader.ReadUInt16();
            }

            return new OS2Table(
                version,
                xAvgCharWidth,
                weightClass,
                widthClass,
                fsType,
                ySubscriptXSize,
                ySubscriptYSize,
                ySubscriptXOffset,
                ySubscriptYOffset,
                ySuperscriptXSize,
                ySuperscriptYSize,
                ySuperscriptXOffset,
                ySuperscriptYOffset,
                strikeoutSize,
                strikeoutPosition,
                familyClass,
                panose,
                unicodeRange1,
                unicodeRange2,
                unicodeRange3,
                unicodeRange4,
                vendorId,
                selection,
                firstCharIndex,
                lastCharIndex,
                typoAscender,
                typoDescender,
                typoLineGap,
                winAscent,
                winDescent,
                codePageRange1,
                codePageRange2,
                xHeight,
                capHeight,
                defaultChar,
                breakChar,
                maxContext,
                lowerOpticalPointSize,
                upperOpticalPointSize);
        }
    }
}
