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

        private readonly NameRecord[] _names;

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
            => GetNameById(culture, KnownNameIds.FontFamilyName);

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
            var languageId = culture;
            NameRecord? usaVersion = null;
            NameRecord? firstWindows = null;
            NameRecord? first = null;
            foreach (var name in _names)
            {
                if (name.NameID == nameId)
                {
                    // Get just the first one, just in case.
                    first ??= name;
                    if (name.Platform == PlatformID.Windows)
                    {
                        // If us not found return the first windows one.
                        firstWindows ??= name;
                        if (name.LanguageID == 0x0409)
                        {
                            // Grab the us version as its on next best match.
                            usaVersion ??= name;
                        }

                        if (name.LanguageID == languageId)
                        {
                            // Return the most exact first.
                            return name.Value;
                        }
                    }
                }
            }

            return usaVersion?.Value ??
                   firstWindows?.Value ??
                   first?.Value ??
                   string.Empty;
        }

        public string GetNameById(ushort culture, ushort nameId)
            => GetNameById(culture, (KnownNameIds)nameId);

        public static NameTable? Load(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return null;
            }

            var reader = new BigEndianBinaryReader(table.Span);

            reader.ReadUInt16(); // version
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
