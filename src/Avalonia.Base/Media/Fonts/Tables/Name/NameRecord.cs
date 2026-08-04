// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;

namespace Avalonia.Media.Fonts.Tables.Name
{
    internal readonly struct NameRecord
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

        public string GetValue()
        {
            if (Length == 0)
            {
                return string.Empty;
            }

            // Offset/Length come straight from the untrusted 'name' record. NameTable.Load validates
            // the record array but not each record's storage slice, and GetValue runs later during
            // typeface construction, so a record pointing past the string storage must degrade to an
            // empty value rather than throw out of the GlyphTypeface constructor and deny the font.
            // Offset and Length are both ushort, so the sum cannot overflow uint.
            if ((uint)Offset + Length > (uint)_stringStorage.Length)
            {
                return string.Empty;
            }

            var span = _stringStorage.Span.Slice(Offset, Length);

            return Encoding.GetString(span);
        }
    }
}
