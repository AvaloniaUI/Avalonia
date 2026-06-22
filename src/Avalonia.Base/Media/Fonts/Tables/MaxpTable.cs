using System;

namespace Avalonia.Media.Fonts.Tables
{
    internal readonly struct MaxpTable
    {
        internal const string TableName = "maxp";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        public FontVersion Version { get; }
        public ushort NumGlyphs { get; }        
        public ushort MaxPoints { get; }
        public ushort MaxContours { get; }
        public ushort MaxCompositePoints { get; }
        public ushort MaxCompositeContours { get; }
        public ushort MaxZones { get; }
        public ushort MaxTwilightPoints { get; }
        public ushort MaxStorage { get; }
        public ushort MaxFunctionDefs { get; }
        public ushort MaxInstructionDefs { get; }
        public ushort MaxStackElements { get; }
        public ushort MaxSizeOfInstructions { get; }
        public ushort MaxComponentElements { get; }
        public ushort MaxComponentDepth { get; }

        private MaxpTable(
            FontVersion version,
            ushort numGlyphs,
            ushort maxPoints,
            ushort maxContours,
            ushort maxCompositePoints,
            ushort maxCompositeContours,
            ushort maxZones,
            ushort maxTwilightPoints,
            ushort maxStorage,
            ushort maxFunctionDefs,
            ushort maxInstructionDefs,
            ushort maxStackElements,
            ushort maxSizeOfInstructions,
            ushort maxComponentElements,
            ushort maxComponentDepth)
        {
            Version = version;
            NumGlyphs = numGlyphs;
            MaxPoints = maxPoints;
            MaxContours = maxContours;
            MaxCompositePoints = maxCompositePoints;
            MaxCompositeContours = maxCompositeContours;
            MaxZones = maxZones;
            MaxTwilightPoints = maxTwilightPoints;
            MaxStorage = maxStorage;
            MaxFunctionDefs = maxFunctionDefs;
            MaxInstructionDefs = maxInstructionDefs;
            MaxStackElements = maxStackElements;
            MaxSizeOfInstructions = maxSizeOfInstructions;
            MaxComponentElements = maxComponentElements;
            MaxComponentDepth = maxComponentDepth;
        }

        public static MaxpTable Load(GlyphTypeface fontFace)
        {
            if (!fontFace.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                throw new InvalidOperationException($"Could not load the '{TableName}' table.");
            }

            var binaryReader = new BigEndianBinaryReader(table.Span);

            return Load(ref binaryReader);
        }

        private static MaxpTable Load(ref BigEndianBinaryReader reader)
        {
            // Version 0.5 (CFF/CFF2 fonts):
            // | Version16Dot16 | version   | 0x00005000 for version 0.5      |
            // | uint16         | numGlyphs | The number of glyphs in the font|
            
            // Version 1.0 (TrueType fonts):
            // | Version16Dot16 | version                | 0x00010000 for version 1.0                          |
            // | uint16         | numGlyphs              | The number of glyphs in the font                    |
            // | uint16         | maxPoints              | Maximum points in a non-composite glyph             |
            // | uint16         | maxContours            | Maximum contours in a non-composite glyph           |
            // | uint16         | maxCompositePoints     | Maximum points in a composite glyph                 |
            // | uint16         | maxCompositeContours   | Maximum contours in a composite glyph               |
            // | uint16         | maxZones               | 1 or 2; should be set to 2 in most cases            |
            // | uint16         | maxTwilightPoints      | Maximum points used in Z0                           |
            // | uint16         | maxStorage             | Number of Storage Area locations                    |
            // | uint16         | maxFunctionDefs        | Number of FDEFs                                     |
            // | uint16         | maxInstructionDefs     | Number of IDEFs                                     |
            // | uint16         | maxStackElements       | Maximum stack depth                                 |
            // | uint16         | maxSizeOfInstructions  | Maximum byte count for glyph instructions           |
            // | uint16         | maxComponentElements   | Maximum number of components at top level           |
            // | uint16         | maxComponentDepth      | Maximum levels of recursion                         |

            FontVersion version = reader.ReadVersion16Dot16();
            ushort numGlyphs = reader.ReadUInt16();

            if (version.Major < 1)
            {
                return new MaxpTable(
                    version,
                    numGlyphs,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            ushort maxPoints = reader.ReadUInt16();
            ushort maxContours = reader.ReadUInt16();
            ushort maxCompositePoints = reader.ReadUInt16();
            ushort maxCompositeContours = reader.ReadUInt16();
            ushort maxZones = reader.ReadUInt16();
            ushort maxTwilightPoints = reader.ReadUInt16();
            ushort maxStorage = reader.ReadUInt16();
            ushort maxFunctionDefs = reader.ReadUInt16();
            ushort maxInstructionDefs = reader.ReadUInt16();
            ushort maxStackElements = reader.ReadUInt16();
            ushort maxSizeOfInstructions = reader.ReadUInt16();
            ushort maxComponentElements = reader.ReadUInt16();
            ushort maxComponentDepth = reader.ReadUInt16();

            return new MaxpTable(
                version,
                numGlyphs,
                maxPoints,
                maxContours,
                maxCompositePoints,
                maxCompositeContours,
                maxZones,
                maxTwilightPoints,
                maxStorage,
                maxFunctionDefs,
                maxInstructionDefs,
                maxStackElements,
                maxSizeOfInstructions,
                maxComponentElements,
                maxComponentDepth);
        }
    }
}
