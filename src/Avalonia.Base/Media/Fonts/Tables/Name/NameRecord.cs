// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

namespace Avalonia.Media.Fonts.Tables.Name
{
    internal class NameRecord
    {
        private readonly string value;

        public NameRecord(PlatformIDs platform, ushort languageId, KnownNameIds nameId, string value)
        {
            Platform = platform;
            LanguageID = languageId;
            NameID = nameId;
            this.value = value;
        }

        public PlatformIDs Platform { get; }

        public ushort LanguageID { get; }

        public KnownNameIds NameID { get; }

        internal StringLoader? StringReader { get; private set; }

        public string Value => StringReader?.Value ?? value;

        public static NameRecord Read(BigEndianBinaryReader reader)
        {
            var platform = reader.ReadUInt16<PlatformIDs>();
            var encodingId = reader.ReadUInt16<EncodingIDs>();
            var encoding = encodingId.AsEncoding();
            var languageID = reader.ReadUInt16();
            var nameID = reader.ReadUInt16<KnownNameIds>();

            var stringReader = StringLoader.Create(reader, encoding);

            return new NameRecord(platform, languageID, nameID, string.Empty)
            {
                StringReader = stringReader
            };
        }
    }
}
