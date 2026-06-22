// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System.Collections;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts.Tables.Name
{
    internal class NameTable : IEnumerable<NameRecord>
    {
        internal const string TableName = "name";
        internal static readonly OpenTypeTag Tag = OpenTypeTag.Parse(TableName);

        private const ushort USEnglishLanguageId = 0x0409;

        private readonly NameRecord[] _names;
        private string? _cachedFamilyName;
        private string? _cachedTypographicFamilyName;

        internal NameTable(NameRecord[] names)
        {
            _names = names;
        }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string Id(ushort culture)
            => GetNameById(culture, KnownNameIds.UniqueFontID);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontName(ushort culture)
            => GetNameById(culture, KnownNameIds.FullFontName);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontFamilyName(ushort culture)
        {
            if (culture == USEnglishLanguageId && _cachedFamilyName is not null)
            {
                return _cachedFamilyName;
            }

            var value = GetNameById(culture, KnownNameIds.FontFamilyName);

            if (culture == USEnglishLanguageId)
            {
                _cachedFamilyName = value;
            }

            return value;
        }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontSubFamilyName(ushort culture)
            => GetNameById(culture, KnownNameIds.FontSubfamilyName);

        public string GetNameById(ushort culture, KnownNameIds nameId)
        {
            if (nameId == KnownNameIds.TypographicFamilyName && culture == USEnglishLanguageId && _cachedTypographicFamilyName is not null)
            {
                return _cachedTypographicFamilyName;
            }

            var languageId = culture;
            NameRecord? usaVersion = null;
            NameRecord? firstWindows = null;
            NameRecord? first = null;

            foreach (var name in _names)
            {
                if (name.NameID == nameId)
                {
                    first ??= name;
                    if (name.Platform == PlatformID.Windows)
                    {
                        firstWindows ??= name;
                        if (name.LanguageID == USEnglishLanguageId)
                        {
                            usaVersion ??= name;
                        }

                        if (name.LanguageID == languageId)
                        {
                            return name.GetValue();
                        }
                    }
                }
            }

            var value = usaVersion?.GetValue() ??
                       firstWindows?.GetValue() ??
                       first?.GetValue() ??
                       string.Empty;

            if (nameId == KnownNameIds.TypographicFamilyName && culture == USEnglishLanguageId)
            {
                _cachedTypographicFamilyName = value;
            }

            return value;
        }

        public string GetNameById(ushort culture, ushort nameId)
            => GetNameById(culture, (KnownNameIds)nameId);

        public static NameTable? Load(GlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return null;
            }

            var reader = new BigEndianBinaryReader(table.Span);

            reader.ReadUInt16();
            var count = reader.ReadUInt16();
            var storageOffset = reader.ReadUInt16();

            var names = new NameRecord[count];

            for (var i = 0; i < count; i++)
            {
                var platform = reader.ReadUInt16<PlatformID>();
                var encodingId = reader.ReadUInt16<EncodingIDs>();
                var encoding = encodingId.AsEncoding();
                var languageID = reader.ReadUInt16();
                var nameID = reader.ReadUInt16<KnownNameIds>();
                var length = reader.ReadUInt16();
                var offset = reader.ReadUInt16();

                names[i] = new NameRecord(table.Slice(storageOffset), platform, languageID, nameID, offset, length, encoding);
            }

            return new NameTable(names);
        }

        public IEnumerator<NameRecord> GetEnumerator()
        {
            return new ImmutableReadOnlyListStructEnumerator<NameRecord>(_names);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
