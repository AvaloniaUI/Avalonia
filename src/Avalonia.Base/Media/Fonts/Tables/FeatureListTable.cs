// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;
using System.Collections.Generic;

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
        private static OpenTypeTag GSubTag { get; } = OpenTypeTag.Parse("GSUB");
        private static OpenTypeTag GPosTag { get; } = OpenTypeTag.Parse("GPOS");

        private FeatureListTable(IReadOnlyList<OpenTypeTag> features)
        {
            Features = features;
        }

        public IReadOnlyList<OpenTypeTag> Features { get; }

        public static FeatureListTable? LoadGSub(GlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(GSubTag, out var gPosTable))
            {
                return null;
            }

            var reader = new BigEndianBinaryReader(gPosTable.Span);

            return Load(ref reader);
        }

        public static FeatureListTable? LoadGPos(GlyphTypeface glyphTypeface)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(GPosTag, out var gSubTable))
            {
                return null;
            }

            var reader = new BigEndianBinaryReader(gSubTable.Span);

            return Load(ref reader);
        }

        private static FeatureListTable Load(ref BigEndianBinaryReader reader)
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

            return Load(ref reader, featureListOffset);
        }

        private static FeatureListTable Load(ref BigEndianBinaryReader reader, int offset)
        {
            // FeatureList
            // +---------------+------------------------------+-----------------------------------------------------------------------------------------------------------------+
            // | Type          | Name                         | Description                                                                                                     |
            // +===============+==============================+=================================================================================================================+
            // | uint16        | featureCount                 | Number of FeatureRecords in this table                                                                          |
            // +---------------+------------------------------+-----------------------------------------------------------------------------------------------------------------+
            // | FeatureRecord | featureRecords[featureCount] | Array of FeatureRecords — zero-based (first feature has FeatureIndex = 0), listed alphabetically by feature tag |
            // +---------------+------------------------------+-----------------------------------------------------------------------------------------------------------------+
            reader.Seek(offset);

            var featureCount = reader.ReadUInt16();

            if (featureCount == 0)
            {
                return new FeatureListTable(Array.Empty<OpenTypeTag>());
            }

            // Use stackalloc for small counts, array for larger
            Span<OpenTypeTag> tempFeatures = featureCount <= 64 
                ? stackalloc OpenTypeTag[featureCount] 
                : new OpenTypeTag[featureCount];

            int uniqueCount = 0;

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

                // Check for duplicates in already added features
                bool isDuplicate = tempFeatures.Contains(tag);

                if (!isDuplicate)
                {
                    tempFeatures[uniqueCount++] = tag;
                }
            }

            // Create array with only unique features
            var features = new OpenTypeTag[uniqueCount];
            tempFeatures.Slice(0, uniqueCount).CopyTo(features);

            return new FeatureListTable(features);
        }
    }
}
