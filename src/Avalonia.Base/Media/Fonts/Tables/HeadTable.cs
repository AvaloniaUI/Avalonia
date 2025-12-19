using System;

namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct HeadTable
    {
        internal const string TableName = "head";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public ushort MajorVersion { get; }
        public ushort MinorVersion { get; }
        public float FontRevision { get; }
        public uint CheckSumAdjustment { get; }
        public uint MagicNumber { get; }
        public ushort Flags { get; }
        public ushort UnitsPerEm { get; }
        public long Created { get; }
        public long Modified { get; }
        public short XMin { get; }
        public short YMin { get; }
        public short XMax { get; }
        public short YMax { get; }
        public ushort MacStyle { get; }
        public ushort LowestRecPPEM { get; }
        public short FontDirectionHint { get; }
        public short IndexToLocFormat { get; }
        public short GlyphDataFormat { get; }

        private HeadTable(
            ushort majorVersion,
            ushort minorVersion,
            float fontRevision,
            uint checkSumAdjustment,
            uint magicNumber,
            ushort flags,
            ushort unitsPerEm,
            long created,
            long modified,
            short xMin,
            short yMin,
            short xMax,
            short yMax,
            ushort macStyle,
            ushort lowestRecPPEM,
            short fontDirectionHint,
            short indexToLocFormat,
            short glyphDataFormat)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            FontRevision = fontRevision;
            CheckSumAdjustment = checkSumAdjustment;
            MagicNumber = magicNumber;
            Flags = flags;
            UnitsPerEm = unitsPerEm;
            Created = created;
            Modified = modified;
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
            MacStyle = macStyle;
            LowestRecPPEM = lowestRecPPEM;
            FontDirectionHint = fontDirectionHint;
            IndexToLocFormat = indexToLocFormat;
            GlyphDataFormat = glyphDataFormat;
        }

        public static HeadTable Load(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                throw new InvalidOperationException("Could not load the 'head' table.");
            }

            var reader = new BigEndianBinaryReader(table.Span);

            return Load(ref reader);
        }

        private static HeadTable Load(ref BigEndianBinaryReader reader)
        {
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            float fontRevision = reader.ReadFixed();
            uint checkSumAdjustment = reader.ReadUInt32();
            uint magicNumber = reader.ReadUInt32();
            ushort flags = reader.ReadUInt16();
            ushort unitsPerEm = reader.ReadUInt16();
            long created = reader.ReadInt64();
            long modified = reader.ReadInt64();
            short xMin = reader.ReadInt16();
            short yMin = reader.ReadInt16();
            short xMax = reader.ReadInt16();
            short yMax = reader.ReadInt16();
            ushort macStyle = reader.ReadUInt16();
            ushort lowestRecPPEM = reader.ReadUInt16();
            short fontDirectionHint = reader.ReadInt16();
            short indexToLocFormat = reader.ReadInt16();
            short glyphDataFormat = reader.ReadInt16();

            return new HeadTable(
                majorVersion,
                minorVersion,
                fontRevision,
                checkSumAdjustment,
                magicNumber,
                flags,
                unitsPerEm,
                created,
                modified,
                xMin,
                yMin,
                xMax,
                yMax,
                macStyle,
                lowestRecPPEM,
                fontDirectionHint,
                indexToLocFormat,
                glyphDataFormat);
        }
    }
}
