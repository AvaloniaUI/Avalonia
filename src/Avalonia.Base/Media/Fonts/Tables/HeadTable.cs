using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables
{
    internal sealed class HeadTable
    {
        internal const string TableName = "head";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private static readonly DateTime s_fontEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public FontVersion Version { get; }
        public FontVersion FontRevision { get; }
        public uint CheckSumAdjustment { get; }
        public uint MagicNumber { get; }
        public HeadFlags Flags { get; }
        public ushort UnitsPerEm { get; }
        public DateTime Created { get; }
        public DateTime Modified { get; }
        public short XMin { get; }
        public short YMin { get; }
        public short XMax { get; }
        public short YMax { get; }
        public MacStyleFlags MacStyle { get; }
        public ushort LowestRecPPEM { get; }
        public FontDirectionHint FontDirectionHint { get; }
        public IndexToLocFormat IndexToLocFormat { get; }
        public GlyphDataFormat GlyphDataFormat { get; }

        private HeadTable(
            FontVersion version,
            FontVersion fontRevision,
            uint checkSumAdjustment,
            uint magicNumber,
            HeadFlags flags,
            ushort unitsPerEm,
            DateTime created,
            DateTime modified,
            short xMin,
            short yMin,
            short xMax,
            short yMax,
            MacStyleFlags macStyle,
            ushort lowestRecPPEM,
            FontDirectionHint fontDirectionHint,
            IndexToLocFormat indexToLocFormat,
            GlyphDataFormat glyphDataFormat)
        {
            Version = version;
            FontRevision = fontRevision;
            CheckSumAdjustment = checkSumAdjustment;
            MagicNumber = magicNumber;
            Flags = flags;
            UnitsPerEm = unitsPerEm;
            Created = created;
            Modified = modified;
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
            MacStyle = macStyle;
            LowestRecPPEM = lowestRecPPEM;
            FontDirectionHint = fontDirectionHint;
            IndexToLocFormat = indexToLocFormat;
            GlyphDataFormat = glyphDataFormat;
        }

        public static bool TryLoad(GlyphTypeface glyphTypeface, [NotNullWhen(true)] out HeadTable? headTable)
        {
            headTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(table.Span);
            headTable = Load(ref reader);

            return true;
        }

        private static HeadTable Load(ref BigEndianBinaryReader reader)
        {
            FontVersion version = reader.ReadVersion16Dot16();
            FontVersion fontRevision = reader.ReadVersion16Dot16();
            uint checkSumAdjustment = reader.ReadUInt32();
            uint magicNumber = reader.ReadUInt32();
            HeadFlags flags = (HeadFlags)reader.ReadUInt16();
            ushort unitsPerEm = reader.ReadUInt16();
            long createdRaw = reader.ReadInt64();
            long modifiedRaw = reader.ReadInt64();
            short xMin = reader.ReadInt16();
            short yMin = reader.ReadInt16();
            short xMax = reader.ReadInt16();
            short yMax = reader.ReadInt16();
            MacStyleFlags macStyle = (MacStyleFlags)reader.ReadUInt16();
            ushort lowestRecPPEM = reader.ReadUInt16();
            FontDirectionHint fontDirectionHint = (FontDirectionHint)reader.ReadInt16();
            IndexToLocFormat indexToLocFormat = (IndexToLocFormat)reader.ReadInt16();
            GlyphDataFormat glyphDataFormat = (GlyphDataFormat)reader.ReadInt16();

            DateTime created = SafeAddSeconds(s_fontEpoch, createdRaw);
            DateTime modified = SafeAddSeconds(s_fontEpoch, modifiedRaw);

            return new HeadTable(
                version,
                fontRevision,
                checkSumAdjustment,
                magicNumber,
                flags,
                unitsPerEm,
                created,
                modified,
                xMin,
                yMin,
                xMax,
                yMax,
                macStyle,
                lowestRecPPEM,
                fontDirectionHint,
                indexToLocFormat,
                glyphDataFormat);
        }

        private static DateTime SafeAddSeconds(DateTime epoch, long seconds)
        {
            // Handle invalid/corrupted timestamps gracefully
            // Valid range for font timestamps is roughly 1904-01-01 to ~2040
            // Negative values or extremely large values indicate corrupted data
            
            try
            {
                // Check if the resulting date would be valid before attempting addition
                // DateTime.MinValue is 0001-01-01, DateTime.MaxValue is 9999-12-31
                if (seconds < 0)
                {
                    // Calculate minimum allowed seconds from epoch to DateTime.MinValue
                    var minSeconds = (long)(DateTime.MinValue - epoch).TotalSeconds;
                    if (seconds < minSeconds)
                    {
                        return DateTime.MinValue;
                    }
                }
                else
                {
                    // Calculate maximum allowed seconds from epoch to DateTime.MaxValue
                    var maxSeconds = (long)(DateTime.MaxValue - epoch).TotalSeconds;
                    if (seconds > maxSeconds)
                    {
                        return DateTime.MaxValue;
                    }
                }

                return epoch.AddSeconds(seconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Fallback for any edge cases that slip through
                return seconds < 0 ? DateTime.MinValue : DateTime.MaxValue;
            }
        }
    }

    /// <summary>
    /// Flags for the 'head' table.
    /// </summary>
    [Flags]
    internal enum HeadFlags : ushort
    {
        /// <summary>
        /// Bit 0: Baseline for font at y=0.
        /// </summary>
        BaselineAtY0 = 1 << 0,

        /// <summary>
        /// Bit 1: Left sidebearing point at x=0 (relevant only for TrueType rasterizers).
        /// </summary>
        LeftSidebearingAtX0 = 1 << 1,

        /// <summary>
        /// Bit 2: Instructions may depend on point size.
        /// </summary>
        InstructionsDependOnPointSize = 1 << 2,

        /// <summary>
        /// Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear.
        /// </summary>
        ForcePpemToInteger = 1 << 3,

        /// <summary>
        /// Bit 4: Instructions may alter advance width (the advance widths might not scale linearly).
        /// </summary>
        InstructionsMayAlterAdvanceWidth = 1 << 4,

        /// <summary>
        /// Bit 5: This bit should be set in fonts that are intended to be laid out vertically, and in which the glyphs have been drawn such that an x-coordinate of 0 corresponds to the desired vertical baseline.
        /// </summary>
        VerticalBaseline = 1 << 5,

        /// <summary>
        /// Bit 7: Font data is 'lossless' as a result of having been subjected to optimizing transformation and/or compression.
        /// </summary>
        Lossless = 1 << 7,

        /// <summary>
        /// Bit 8: Font converted (produce compatible metrics).
        /// </summary>
        FontConverted = 1 << 8,

        /// <summary>
        /// Bit 9: Font optimized for ClearType. Note that this implies that instructions may alter advance widths (bit 4 should also be set).
        /// </summary>
        ClearTypeOptimized = 1 << 9,

        /// <summary>
        /// Bit 10: Last Resort font. If set, indicates that the glyphs encoded in the 'cmap' subtables are simply generic symbolic representations of code point ranges and don't truly represent support for those code points.
        /// </summary>
        LastResortFont = 1 << 10,
    }

    /// <summary>
    /// Mac style flags for font styling (used by macOS).
    /// </summary>
    [Flags]
    internal enum MacStyleFlags : ushort
    {
        /// <summary>
        /// Bit 0: Bold (if set to 1).
        /// </summary>
        Bold = 1 << 0,

        /// <summary>
        /// Bit 1: Italic (if set to 1).
        /// </summary>
        Italic = 1 << 1,

        /// <summary>
        /// Bit 2: Underline (if set to 1).
        /// </summary>
        Underline = 1 << 2,

        /// <summary>
        /// Bit 3: Outline (if set to 1).
        /// </summary>
        Outline = 1 << 3,

        /// <summary>
        /// Bit 4: Shadow (if set to 1).
        /// </summary>
        Shadow = 1 << 4,

        /// <summary>
        /// Bit 5: Condensed (if set to 1).
        /// </summary>
        Condensed = 1 << 5,

        /// <summary>
        /// Bit 6: Extended (if set to 1).
        /// </summary>
        Extended = 1 << 6,
    }

    /// <summary>
    /// Specifies the format used for the 'loca' table.
    /// </summary>
    internal enum IndexToLocFormat : short
    {
        /// <summary>
        /// Short offsets (Offset16). The actual local offset divided by 2 is stored.
        /// </summary>
        Short = 0,

        /// <summary>
        /// Long offsets (Offset32). The actual local offset is stored.
        /// </summary>
        Long = 1
    }

    /// <summary>
    /// Specifies the format of glyph data.
    /// </summary>
    internal enum GlyphDataFormat : short
    {
        /// <summary>
        /// Current format (TrueType outlines).
        /// </summary>
        Current = 0
    }

    /// <summary>
    /// Font direction hint for mixed directional text.
    /// </summary>
    internal enum FontDirectionHint : short
    {
        /// <summary>
        /// Fully mixed directional glyphs.
        /// </summary>
        FullyMixed = 0,

        /// <summary>
        /// Only strongly left to right glyphs.
        /// </summary>
        OnlyLeftToRight = 1,

        /// <summary>
        /// Like 1 but also contains neutrals.
        /// </summary>
        LeftToRightWithNeutrals = 2,

        /// <summary>
        /// Only strongly right to left glyphs.
        /// </summary>
        OnlyRightToLeft = -1,

        /// <summary>
        /// Like -1 but also contains neutrals.
        /// </summary>
        RightToLeftWithNeutrals = -2
    }
}
