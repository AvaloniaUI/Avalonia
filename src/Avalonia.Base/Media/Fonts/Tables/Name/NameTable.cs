// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Avalonia.Media.Fonts.Tables.Name
{
    internal class NameTable
    {
        internal const string TableName = "name";
        internal static OpenTypeTag Tag = OpenTypeTag.Parse(TableName);

        private readonly NameRecord[] _names;

        internal NameTable(NameRecord[] names, IReadOnlyList<CultureInfo> languages)
        {
            _names = names;
            Languages = languages;
        }

        public IReadOnlyList<CultureInfo> Languages { get; }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string Id(CultureInfo culture)
            => GetNameById(culture, KnownNameIds.UniqueFontID);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontName(CultureInfo culture)
            => GetNameById(culture, KnownNameIds.FullFontName);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontFamilyName(CultureInfo culture)
            => GetNameById(culture, KnownNameIds.FontFamilyName);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontSubFamilyName(CultureInfo culture)
            => GetNameById(culture, KnownNameIds.FontSubfamilyName);

        public string GetNameById(CultureInfo culture, KnownNameIds nameId)
        {
            var languageId = culture.LCID;
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

        public string GetNameById(CultureInfo culture, ushort nameId)
            => GetNameById(culture, (KnownNameIds)nameId);

        public static NameTable Load(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.TryGetTable(Tag, out var table))
            {
                throw new MissingFontTableException("Could not load table", "name");
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

            var cultures = new List<CultureInfo>();

            foreach (var nameRecord in names)
            {
                if (nameRecord.NameID != KnownNameIds.FontFamilyName || nameRecord.Platform != PlatformIDs.Windows || nameRecord.LanguageID == 0)
                {
                    continue;
                }

                var culture = new CultureInfo(nameRecord.LanguageID);

                if (!cultures.Contains(culture))
                {
                    cultures.Add(culture);
                }
            }

            //var languages = languageNames.Select(x => x.Value).ToArray();

            return new NameTable(names, cultures);
        }
    }
}
