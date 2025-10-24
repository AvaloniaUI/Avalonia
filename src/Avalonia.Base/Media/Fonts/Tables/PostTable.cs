namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct PostTable
    {
        internal const string TableName = "post";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public float Version { get; }
        public float ItalicAngle { get; }
        public short UnderlinePosition { get; }
        public short UnderlineThickness { get; }
        public bool IsFixedPitch { get; }

        private PostTable(float version, float italicAngle, short underlinePosition, short underlineThickness, uint isFixedPitch)
        {
            Version = version;
            ItalicAngle = italicAngle;
            UnderlinePosition = underlinePosition;
            UnderlineThickness = underlineThickness;
            IsFixedPitch = isFixedPitch != 0;
        }

        public static PostTable Load(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return default;
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            return Load(binaryReader);
        }

        private static PostTable Load(BigEndianBinaryReader reader)
        {
            float version = reader.ReadFixed();
            float italicAngle = reader.ReadFixed();
            short underlinePosition = reader.ReadFWORD();
            short underlineThickness = reader.ReadFWORD();
            uint isFixedPitch = reader.ReadUInt32();

            return new PostTable(version, italicAngle, underlinePosition, underlineThickness, isFixedPitch);
        }
    }
}
