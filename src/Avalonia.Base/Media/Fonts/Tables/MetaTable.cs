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
        private const int HeaderSize = 16;
        private const int DataMapRecordSize = 12;

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

            // OpenType 'meta' layout:
            //   uint32 version
            //   uint32 flags
            //   uint32 reserved
            //   uint32 dataMapsCount
            //   DataMap[dataMapsCount]
            //
            // DataMap layout:
            //   Tag tag (4-byte packed value)
            //   Offset32 dataOffset (from start of this table)
            //   uint32 dataLength
            if (span.Length < HeaderSize)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(span);

            // ReadUInt32 reads big-endian bytes and reassembles them into a uint value.
            var version = reader.ReadUInt32();

            if (version != 1)
            {
                return false;
            }

            // flags + reserved
            reader.ReadUInt32();
            reader.ReadUInt32();

            var dataMapsCount = reader.ReadUInt32();

            if (dataMapsCount == 0)
            {
                metaTable = new MetaTable([], []);
                return true;
            }

            // Validate that the declared record array fully fits after the header.
            var maxDataMapsByLength = (uint)((span.Length - HeaderSize) / DataMapRecordSize);

            if (dataMapsCount > maxDataMapsByLength)
            {
                return false;
            }

            string[]? designLanguages = null;
            string[]? supportedLanguages = null;

            for (var i = 0; i < dataMapsCount; i++)
            {
                // Tag values are 4-byte OpenType identifiers (packed into uint).
                var entryTag = new OpenTypeTag(reader.ReadUInt32());
                var dataOffset = reader.ReadUInt32();
                var dataLength = reader.ReadUInt32();

                if (entryTag != s_dlngTag && entryTag != s_slngTag)
                {
                    continue;
                }

                // Each data payload is referenced by (offset, length) from the start of this table.
                if (dataOffset > (uint)span.Length || dataLength > (uint)span.Length - dataOffset)
                {
                    continue;
                }

                var data = span.Slice((int)dataOffset, (int)dataLength);
                var tags = ParseLanguageTags(data);

                if (entryTag == s_dlngTag)
                {
                    // Spec says only one instance is used; ignore subsequent duplicates.
                    designLanguages ??= tags;
                }
                else
                {
                    // Spec says only one instance is used; ignore subsequent duplicates.
                    supportedLanguages ??= tags;
                }
            }

            metaTable = new MetaTable(
                designLanguages ?? [],
                supportedLanguages ?? []);

            return true;
        }

        private static string[] ParseLanguageTags(ReadOnlySpan<byte> data)
        {
            // The data is UTF-8 text consisting of one or more ScriptLangTags separated by commas,
            // and spaces around separators are ignored.
            var text = Encoding.UTF8.GetString(data);
            var tags = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return tags;
        }
    }
}
