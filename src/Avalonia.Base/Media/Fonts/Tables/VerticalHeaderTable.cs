namespace Avalonia.Media.Fonts.Tables
{
    internal class VerticalHeaderTable
    {
        internal const string TableName = "vhea";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public VerticalHeaderTable(
            short ascender,
            short descender,
            short lineGap,
            ushort advanceHeightMax,
            short minTopSideBearing,
            short minBottomSideBearing,
            short yMaxExtent,
            short caretSlopeRise,
            short caretSlopeRun,
            short caretOffset,
            ushort numberOfVMetrics)
        {
            Ascender = ascender;
            Descender = descender;
            LineGap = lineGap;
            AdvanceHeightMax = advanceHeightMax;
            MinTopSideBearing = minTopSideBearing;
            MinBottomSideBearing = minBottomSideBearing;
            YMaxExtent = yMaxExtent;
            CaretSlopeRise = caretSlopeRise;
            CaretSlopeRun = caretSlopeRun;
            CaretOffset = caretOffset;
            NumberOfVMetrics = numberOfVMetrics;
        }

        public ushort AdvanceHeightMax { get; }

        public short Ascender { get; }

        public short CaretOffset { get; }

        public short CaretSlopeRise { get; }

        public short CaretSlopeRun { get; }

        public short Descender { get; }

        public short LineGap { get; }

        public short MinTopSideBearing { get; }

        public short MinBottomSideBearing { get; }

        public ushort NumberOfVMetrics { get; }

        public short YMaxExtent { get; }

        public static VerticalHeaderTable? Load(IGlyphTypeface fontFace)
        {
            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return null;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            // Move to start of table.
            return Load(binaryReader);
        }

        private static VerticalHeaderTable Load(BigEndianBinaryReader reader)
        {
            // See OpenType spec for vhea:
            // | Fixed  | version             | 0x00010000 (1.0)                                                                |
            // | FWord  | ascender            | Distance from baseline of highest ascender (vertical)                            |
            // | FWord  | descender           | Distance from baseline of lowest descender (vertical)                            |
            // | FWord  | lineGap             | typographic line gap (vertical)                                                  |
            // | uFWord | advanceHeightMax    | must be consistent with vertical metrics                                         |
            // | FWord  | minTopSideBearing   | must be consistent with vertical metrics                                         |
            // | FWord  | minBottomSideBearing| must be consistent with vertical metrics                                         |
            // | FWord  | yMaxExtent          | max(tsb + (yMax-yMin))                                                           |
            // | int16  | caretSlopeRise      | used to calculate the slope of the caret (rise/run) set to 1 for vertical caret  |
            // | int16  | caretSlopeRun       | 0 for vertical                                                                   |
            // | FWord  | caretOffset         | set value to 0 for non-slanted fonts                                             |
            // | int16  | reserved            | set value to 0                                                                   |
            // | int16  | reserved            | set value to 0                                                                   |
            // | int16  | reserved            | set value to 0                                                                   |
            // | int16  | reserved            | set value to 0                                                                   |
            // | int16  | metricDataFormat    | 0 for current format                                                             |
            // | uint16 | numOfLongVerMetrics | number of advance heights in vertical metrics table                              |

            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            short ascender = reader.ReadFWORD();
            short descender = reader.ReadFWORD();
            short lineGap = reader.ReadFWORD();
            ushort advanceHeightMax = reader.ReadUFWORD();
            short minTopSideBearing = reader.ReadFWORD();
            short minBottomSideBearing = reader.ReadFWORD();
            short yMaxExtent = reader.ReadFWORD();
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

            ushort numberOfVMetrics = reader.ReadUInt16();

            return new VerticalHeaderTable(
                ascender,
                descender,
                lineGap,
                advanceHeightMax,
                minTopSideBearing,
                minBottomSideBearing,
                yMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                numberOfVMetrics);
        }
    }
}
