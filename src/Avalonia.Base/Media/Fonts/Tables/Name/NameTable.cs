// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System.Collections.Generic;
using System.IO;

namespace Avalonia.Media.Fonts.Tables.Name
{
    internal class NameTable
    {
        internal const string TableName = "name";
        internal static readonly OpenTypeTag Tag = OpenTypeTag.Parse(TableName);

        private readonly NameRecord[] _names;

        internal NameTable(NameRecord[] names, IReadOnlyList<ushort> languages)
        {
            _names = names;
            Languages = languages;
        }

        public IReadOnlyList<ushort> Languages { get; }

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
                    if (name.Platform == PlatformIDs.Windows)
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
            if (!glyphTypeface.TryGetTable(Tag, out var table))
            {
                return null;
            }

            using var stream = new MemoryStream(table);
            using var binaryReader = new BigEndianBinaryReader(stream, false);

            // Move to start of table.
            return Load(binaryReader);
        }

        public static NameTable Load(BigEndianBinaryReader reader)
        {
            var strings = new List<StringLoader>();
            var format = reader.ReadUInt16();
            var nameCount = reader.ReadUInt16();
            var stringOffset = reader.ReadUInt16();

            var names = new NameRecord[nameCount];

            for (var i = 0; i < nameCount; i++)
            {
                names[i] = NameRecord.Read(reader);

                var sr = names[i].StringReader;

                if (sr is not null)
                {
                    strings.Add(sr);
                }
            }

            //var languageNames = Array.Empty<StringLoader>();

            //if (format == 1)
            //{
            //    // Format 1 adds language data.
            //    var langCount = reader.ReadUInt16();
            //    languageNames = new StringLoader[langCount];

            //    for (var i = 0; i < langCount; i++)
            //    {
            //        languageNames[i] = StringLoader.Create(reader);

            //        strings.Add(languageNames[i]);
            //    }
            //}

            foreach (var readable in strings)
            {
                var readableStartOffset = stringOffset + readable.Offset;

                reader.Seek(readableStartOffset, SeekOrigin.Begin);

                readable.LoadValue(reader);
            }

            var cultures = new List<ushort>();

            foreach (var nameRecord in names)
            {
                if (nameRecord.NameID != KnownNameIds.FontFamilyName || nameRecord.Platform != PlatformIDs.Windows || nameRecord.LanguageID == 0)
                {
                    continue;
                }

                if (!cultures.Contains(nameRecord.LanguageID))
                {
                    cultures.Add(nameRecord.LanguageID);
                }
            }

            return new NameTable(names, cultures);
        }
    }
}
