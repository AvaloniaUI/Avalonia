// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;

namespace Avalonia.Media.Fonts.Tables.Name
{
    internal class NameRecord
    {
        private readonly ReadOnlyMemory<byte> _stringStorage;

        public NameRecord(
            ReadOnlyMemory<byte> stringStorage,
            PlatformID platform,
            ushort languageId,
            KnownNameIds nameId,
            ushort offset,
            ushort length,
            System.Text.Encoding encoding)
        {
            _stringStorage = stringStorage;

            Platform = platform;
            LanguageID = languageId;
            NameID = nameId;
            Offset = offset;
            Length = length;
            Encoding = encoding;
        }

        public PlatformID Platform { get; }

        public ushort LanguageID { get; }

        public KnownNameIds NameID { get; }

        public ushort Offset { get; }

        public ushort Length { get; }

        public System.Text.Encoding Encoding { get; }

        public string Value
        {
            get
            {
                if (Length == 0)
                {
                    return string.Empty;
                }

                var reader = new BigEndianBinaryReader(_stringStorage.Span);

                reader.Seek(Offset);

                byte[] data = reader.ReadBytes(Length);

                var value = Encoding.GetString(data);

                return value;
            }
        }
    }
}
