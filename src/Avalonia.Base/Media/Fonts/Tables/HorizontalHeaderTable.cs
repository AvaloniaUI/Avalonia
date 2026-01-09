// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct HorizontalHeaderTable
    {
        internal const string TableName = "hhea";

        /// <summary>
        /// Gets the OpenType tag identifying this table ("hhea").
        /// </summary>
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        /// <summary>
        /// Gets the version of the horizontal header table.
        /// </summary>
        public FontVersion Version { get; }

        /// <summary>
        /// Gets the maximum advance width value for all glyphs in the font.
        /// </summary>
        public ushort AdvanceWidthMax { get; }

        /// <summary>
        /// Distance from the baseline to the highest ascender.
        /// </summary>
        public short Ascender { get; }

        /// <summary>
        /// Offset of the caret for slanted fonts. Set to 0 for non-slanted fonts.
        /// </summary>
        public short CaretOffset { get; }

        /// <summary>
        /// Rise component used to calculate the slope of the caret (rise/run).
        /// </summary>
        public short CaretSlopeRise { get; }

        /// <summary>
        /// Run component used to calculate the slope of the caret (rise/run).
        /// </summary>
        public short CaretSlopeRun { get; }

        /// <summary>
        /// Distance from the baseline to the lowest descender.
        /// </summary>
        public short Descender { get; }

        /// <summary>
        /// Typographic line gap.
        /// </summary>
        public short LineGap { get; }

        /// <summary>
        /// Minimum left side bearing value. Must be consistent with horizontal metrics.
        /// </summary>
        public short MinLeftSideBearing { get; }

        /// <summary>
        /// Minimum right side bearing value. Must be consistent with horizontal metrics.
        /// </summary>
        public short MinRightSideBearing { get; }

        /// <summary>
        /// Number of advance widths in the horizontal metrics table (numOfLongHorMetrics).
        /// </summary>
        public ushort NumberOfHMetrics { get; }

        /// <summary>
        /// Maximum horizontal extent: max(lsb + (xMax - xMin)).
        /// </summary>
        public short XMaxExtent { get; }

        public HorizontalHeaderTable(
            FontVersion version,
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
            Version = version;
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

        public static bool TryLoad(GlyphTypeface fontFace, out HorizontalHeaderTable horizontalHeaderTable)
        {
            horizontalHeaderTable = default;

            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return false;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            return TryLoad(ref binaryReader, out horizontalHeaderTable);
        }

        private static bool TryLoad(ref BigEndianBinaryReader reader, out HorizontalHeaderTable horizontalHeaderTable)
        {
            horizontalHeaderTable = default;

            // +--------+---------------------+---------------------------------------------------------------------------------+
            // | Type   | Name                | Description                                                                     |
            // +========+=====================+=================================================================================+
            // | Version16Dot16 | version     | 0x00010000 (1.0)                                                                |
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
            FontVersion version = reader.ReadVersion16Dot16();
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
                return false;
            }

            ushort numberOfHMetrics = reader.ReadUInt16();

            horizontalHeaderTable = new HorizontalHeaderTable(
                version,
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

            return true;
        }
    }
}
