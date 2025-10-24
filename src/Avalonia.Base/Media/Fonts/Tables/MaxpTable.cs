namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct MaxpTable
    {
        internal const string TableName = "maxp";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public ushort NumGlyphs { get; }

        private MaxpTable(ushort numGlyphs)
        {
            NumGlyphs = numGlyphs;
        }

        public static MaxpTable? Load(IGlyphTypeface fontFace)
        {
            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return null;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            return Load(binaryReader);
        }

        private static MaxpTable Load(BigEndianBinaryReader reader)
        {
            // Skip version (4 bytes)
            reader.ReadUInt32();

            var numGlyphs = reader.ReadUInt16();

            return new MaxpTable(numGlyphs);
        }
    }
}
