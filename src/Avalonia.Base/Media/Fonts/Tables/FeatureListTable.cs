// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System.Collections.Generic;
using System.IO;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Features provide information about how to use the glyphs in a font to render a script or language.
    /// For example, an Arabic font might have a feature for substituting initial glyph forms, and a Kanji font
    /// might have a feature for positioning glyphs vertically. All OpenType Layout features define data for
    /// glyph substitution, glyph positioning, or both.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/featurelist"/>
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table"/>
    /// </summary>
    internal class FeatureListTable
    {
        private static OpenTypeTag GSubTag = OpenTypeTag.Parse("GSUB");
        private static OpenTypeTag GPosTag = OpenTypeTag.Parse("GPOS");

        private FeatureListTable(IReadOnlyList<OpenTypeTag> features)
        {
            Features = features;
        }

        public IReadOnlyList<OpenTypeTag> Features { get; }

        public static FeatureListTable? LoadGSub(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.TryGetTable(GSubTag, out var gPosTable))
            {
                return null;
            }

            using var stream = new MemoryStream(gPosTable);
            using var reader = new BigEndianBinaryReader(stream, false);

            return Load(reader);

        }
        public static FeatureListTable? LoadGPos(IGlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.TryGetTable(GPosTag, out var gSubTable))
            {
                return null;
            }

            using var stream = new MemoryStream(gSubTable);
            using var reader = new BigEndianBinaryReader(stream, false);

            return Load(reader);

        }

        private static FeatureListTable Load(BigEndianBinaryReader reader)
        {
            // GPOS/GSUB Header, Version 1.0
            // +----------+-------------------+-----------------------------------------------------------+
            // | Type     | Name              | Description                                               |
            // +==========+===================+===========================================================+
            // | uint16   | majorVersion      | Major version of the GPOS table, = 1                      |
            // +----------+-------------------+-----------------------------------------------------------+
            // | uint16   | minorVersion      | Minor version of the GPOS table, = 0                      |
            // +----------+-------------------+-----------------------------------------------------------+
            // | Offset16 | scriptListOffset  | Offset to ScriptList table, from beginning of GPOS table  |
            // +----------+-------------------+-----------------------------------------------------------+
            // | Offset16 | featureListOffset | Offset to FeatureList table, from beginning of GPOS table |
            // +----------+-------------------+-----------------------------------------------------------+
            // | Offset16 | lookupListOffset  | Offset to LookupList table, from beginning of GPOS table  |
            // +----------+-------------------+-----------------------------------------------------------+

            reader.ReadUInt16();
            reader.ReadUInt16();

            reader.ReadOffset16();
            var featureListOffset = reader.ReadOffset16();

            return Load(reader, featureListOffset);
        }

        private static FeatureListTable Load(BigEndianBinaryReader reader, long offset)
        {
            // FeatureList
            // +---------------+------------------------------+-----------------------------------------------------------------------------------------------------------------+
            // | Type          | Name                         | Description                                                                                                     |
            // +===============+==============================+=================================================================================================================+
            // | uint16        | featureCount                 | Number of FeatureRecords in this table                                                                          |
            // +---------------+------------------------------+-----------------------------------------------------------------------------------------------------------------+
            // | FeatureRecord | featureRecords[featureCount] | Array of FeatureRecords — zero-based (first feature has FeatureIndex = 0), listed alphabetically by feature tag |
            // +---------------+------------------------------+-----------------------------------------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            var featureCount = reader.ReadUInt16();

            var features = new List<OpenTypeTag>(featureCount);

            for (var i = 0; i < featureCount; i++)
            {
                // FeatureRecord
                // +----------+---------------+--------------------------------------------------------+
                // | Type     | Name          | Description                                            |
                // +==========+===============+========================================================+
                // | Tag      | featureTag    | 4-byte feature identification tag                      |
                // +----------+---------------+--------------------------------------------------------+
                // | Offset16 | featureOffset | Offset to Feature table, from beginning of FeatureList |
                // +----------+---------------+--------------------------------------------------------+
                var featureTag = reader.ReadUInt32();

                reader.ReadOffset16();

                var tag = new OpenTypeTag(featureTag);

                if (!features.Contains(tag))
                {
                    features.Add(tag);
                }
            }

            return new FeatureListTable(features /*featureTables*/);
        }

    }
}
