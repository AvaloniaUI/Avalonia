using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Parses the OpenType <c>meta</c> table (script and design language tags).
    /// See <see href="https://learn.microsoft.com/typography/opentype/spec/meta"/>.
    /// </summary>
    internal readonly struct MetaTable
    {
        internal const string TableName = "meta";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        // Tags whose data is a UTF-8 string of comma-separated ScriptLangTags (BCP 47 language identifiers).
        private static readonly OpenTypeTag s_dlngTag = OpenTypeTag.Parse("dlng");
        private static readonly OpenTypeTag s_slngTag = OpenTypeTag.Parse("slng");

        /// <summary>
        /// The BCP-47 language tags declared by the font under the <c>dlng</c> (design languages) data tag.
        /// </summary>
        public string[] DesignLanguages { get; }

        /// <summary>
        /// The BCP-47 language tags declared by the font under the <c>slng</c> (supported languages) data tag.
        /// </summary>
        public string[] SupportedLanguages { get; }

        private MetaTable(string[] designLanguages, string[] supportedLanguages)
        {
            DesignLanguages = designLanguages;
            SupportedLanguages = supportedLanguages;
        }

        public static bool TryLoad(GlyphTypeface glyphTypeface, out MetaTable metaTable)
        {
            metaTable = default;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return false;
            }

            return TryParse(table.Span, out metaTable);
        }

        internal static bool TryParse(ReadOnlySpan<byte> span, out MetaTable metaTable)
        {
            metaTable = default;

            if (span.Length < 16)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(span);

            var version = reader.ReadUInt32();

            if (version != 1)
            {
                return false;
            }

            // flags + reserved
            reader.ReadUInt32();
            reader.ReadUInt32();

            var dataMapsCount = reader.ReadUInt32();

            if (dataMapsCount == 0 || dataMapsCount > (uint)(span.Length / 12))
            {
                metaTable = new MetaTable(Array.Empty<string>(), Array.Empty<string>());
                return true;
            }

            List<string>? designLanguages = null;
            List<string>? supportedLanguages = null;

            for (var i = 0; i < dataMapsCount; i++)
            {
                var entryTag = new OpenTypeTag(reader.ReadUInt32());
                var dataOffset = reader.ReadUInt32();
                var dataLength = reader.ReadUInt32();

                if (entryTag != s_dlngTag && entryTag != s_slngTag)
                {
                    continue;
                }

                if (dataOffset > (uint)span.Length || dataLength > (uint)span.Length - dataOffset)
                {
                    continue;
                }

                var data = span.Slice((int)dataOffset, (int)dataLength);
                var tags = ParseLanguageTags(data);

                if (entryTag == s_dlngTag)
                {
                    designLanguages = tags;
                }
                else
                {
                    supportedLanguages = tags;
                }
            }

            metaTable = new MetaTable(
                designLanguages?.ToArray() ?? Array.Empty<string>(),
                supportedLanguages?.ToArray() ?? Array.Empty<string>());

            return true;
        }

        private static List<string> ParseLanguageTags(ReadOnlySpan<byte> data)
        {
            // The data is UTF-8 text consisting of one or more ScriptLangTags separated by commas,
            // optionally surrounded by ASCII whitespace.
            var text = Encoding.UTF8.GetString(data);
            var tags = new List<string>();
            var start = 0;

            for (var i = 0; i <= text.Length; i++)
            {
                if (i != text.Length && text[i] != ',')
                {
                    continue;
                }

                var slice = text.AsSpan(start, i - start).Trim();

                if (slice.Length > 0)
                {
                    tags.Add(slice.ToString());
                }

                start = i + 1;
            }

            return tags;
        }
    }
}
