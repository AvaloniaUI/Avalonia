namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct VerticalHeaderTable
    {
        internal const string TableName = "vhea";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public FontVersion Version { get; }
        public short Ascender { get; }
        public short Descender { get; }
        public short LineGap { get; }
        public ushort AdvanceHeightMax { get; }
        public short MinTopSideBearing { get; }
        public short MinBottomSideBearing { get; }
        public short YMaxExtent { get; }
        public short CaretSlopeRise { get; }
        public short CaretSlopeRun { get; }
        public short CaretOffset { get; }
        public ushort NumberOfVMetrics { get; }

        public VerticalHeaderTable(
            FontVersion version,
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
            Version = version;
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

        public static bool TryLoad(GlyphTypeface fontFace, out VerticalHeaderTable verticalHeaderTable)
        {
            verticalHeaderTable = default;

            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return false;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            return TryLoad(ref binaryReader, out verticalHeaderTable);
        }

        private static bool TryLoad(ref BigEndianBinaryReader reader, out VerticalHeaderTable verticalHeaderTable)
        {
            verticalHeaderTable = default;

            // See OpenType spec for vhea:
            // | Version16Dot16 | version             | 0x00010000 (1.0) or 0x00011000 (1.1)                                            |
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

            FontVersion version = reader.ReadVersion16Dot16();
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
                return false;
            }

            ushort numberOfVMetrics = reader.ReadUInt16();

            verticalHeaderTable = new VerticalHeaderTable(
                version,
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

            return true;
        }
    }
}
