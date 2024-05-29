// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System.IO;

namespace Avalonia.Media.Fonts.Tables
{
    internal class HorizontalHeadTable
    {
        internal const string TableName = "hhea";
        internal static OpenTypeTag Tag = OpenTypeTag.Parse(TableName);

        public HorizontalHeadTable(
            short ascender,
            short descender,
            short lineGap,
            ushort advanceWidthMax,
            short minLeftSideBearing,
            short minRightSideBearing,
            short xMaxExtent,
            short caretSlopeRise,
            short caretSlopeRun,
            short caretOffset,
            ushort numberOfHMetrics)
        {
            Ascender = ascender;
            Descender = descender;
            LineGap = lineGap;
            AdvanceWidthMax = advanceWidthMax;
            MinLeftSideBearing = minLeftSideBearing;
            MinRightSideBearing = minRightSideBearing;
            XMaxExtent = xMaxExtent;
            CaretSlopeRise = caretSlopeRise;
            CaretSlopeRun = caretSlopeRun;
            CaretOffset = caretOffset;
            NumberOfHMetrics = numberOfHMetrics;
        }

        public ushort AdvanceWidthMax { get; }

        public short Ascender { get; }

        public short CaretOffset { get; }

        public short CaretSlopeRise { get; }

        public short CaretSlopeRun { get; }

        public short Descender { get; }

        public short LineGap { get; }

        public short MinLeftSideBearing { get; }

        public short MinRightSideBearing { get; }

        public ushort NumberOfHMetrics { get; }

        public short XMaxExtent { get; }

        public static HorizontalHeadTable Load(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.TryGetTable(Tag, out var table))
            {
                throw new MissingFontTableException("Could not load table", "name");
            }

            using var stream = new MemoryStream(table);
            using var binaryReader = new BigEndianBinaryReader(stream, false);

            // Move to start of table.
            return Load(binaryReader);
        }

        public static HorizontalHeadTable Load(BigEndianBinaryReader reader)
        {
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | Type   | Name                | Description                                                                     |
            // +========+=====================+=================================================================================+
            // | Fixed  | version             | 0x00010000 (1.0)                                                                |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | ascent              | Distance from baseline of highest ascender                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | descent             | Distance from baseline of lowest descender                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | lineGap             | typographic line gap                                                            |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | uFWord | advanceWidthMax     | must be consistent with horizontal metrics                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | minLeftSideBearing  | must be consistent with horizontal metrics                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | minRightSideBearing | must be consistent with horizontal metrics                                      |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | xMaxExtent          | max(lsb + (xMax-xMin))                                                          |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | caretSlopeRise      | used to calculate the slope of the caret (rise/run) set to 1 for vertical caret |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | caretSlopeRun       | 0 for vertical                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | FWord  | caretOffset         | set value to 0 for non-slanted fonts                                            |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | reserved            | set value to 0                                                                  |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | int16  | metricDataFormat    | 0 for current format                                                            |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | uint16 | numOfLongHorMetrics | number of advance widths in metrics table                                       |
            // +--------+---------------------+---------------------------------------------------------------------------------+
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            short ascender = reader.ReadFWORD();
            short descender = reader.ReadFWORD();
            short lineGap = reader.ReadFWORD();
            ushort advanceWidthMax = reader.ReadUFWORD();
            short minLeftSideBearing = reader.ReadFWORD();
            short minRightSideBearing = reader.ReadFWORD();
            short xMaxExtent = reader.ReadFWORD();
            short caretSlopeRise = reader.ReadInt16();
            short caretSlopeRun = reader.ReadInt16();
            short caretOffset = reader.ReadInt16();
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            reader.ReadInt16(); // reserved
            short metricDataFormat = reader.ReadInt16(); // 0
            if (metricDataFormat != 0)
            {
                throw new InvalidFontTableException($"Expected metricDataFormat = 0 found {metricDataFormat}", TableName);
            }

            ushort numberOfHMetrics = reader.ReadUInt16();

            return new HorizontalHeadTable(
                ascender,
                descender,
                lineGap,
                advanceWidthMax,
                minLeftSideBearing,
                minRightSideBearing,
                xMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                numberOfHMetrics);
        }
    }
}
