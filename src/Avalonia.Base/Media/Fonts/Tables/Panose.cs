namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Represents the PANOSE classification for a font.
    /// PANOSE is a font classification system that describes the visual characteristics of a typeface.
    /// </summary>
    /// <remarks>
    /// The interpretation of bytes 1-9 depends on the FamilyKind (byte 0).
    /// This struct represents the Latin Text interpretation (FamilyKind = 2), which is the most common.
    /// For other family kinds, access the raw bytes via the indexer.
    /// </remarks>
    internal readonly struct Panose
    {
        private readonly byte[] _data;

        public Panose(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9)
        {
            _data = new byte[10] { b0, b1, b2, b3, b4, b5, b6, b7, b8, b9 };
        }

        public static Panose Load(ref BigEndianBinaryReader reader)
        {
            return new Panose(
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()
            );
        }

        /// <summary>
        /// Gets the family kind classification (byte 0).
        /// </summary>
        public PanoseFamilyKind FamilyKind => (PanoseFamilyKind)_data[0];

        // Latin Text properties (when FamilyKind == LatinText)

        /// <summary>
        /// Gets the serif style (byte 1) for Latin Text fonts.
        /// </summary>
        public PanoseSerifStyle SerifStyle => (PanoseSerifStyle)_data[1];

        /// <summary>
        /// Gets the weight (byte 2) for Latin Text fonts.
        /// </summary>
        public PanoseWeight Weight => (PanoseWeight)_data[2];

        /// <summary>
        /// Gets the proportion (byte 3) for Latin Text fonts.
        /// </summary>
        public PanoseProportion Proportion => (PanoseProportion)_data[3];

        /// <summary>
        /// Gets the contrast (byte 4) for Latin Text fonts.
        /// </summary>
        public PanoseContrast Contrast => (PanoseContrast)_data[4];

        /// <summary>
        /// Gets the stroke variation (byte 5) for Latin Text fonts.
        /// </summary>
        public PanoseStrokeVariation StrokeVariation => (PanoseStrokeVariation)_data[5];

        /// <summary>
        /// Gets the arm style (byte 6) for Latin Text fonts.
        /// </summary>
        public PanoseArmStyle ArmStyle => (PanoseArmStyle)_data[6];

        /// <summary>
        /// Gets the letterform (byte 7) for Latin Text fonts.
        /// </summary>
        public PanoseLetterform Letterform => (PanoseLetterform)_data[7];

        /// <summary>
        /// Gets the midline (byte 8) for Latin Text fonts.
        /// </summary>
        public PanoseMidline Midline => (PanoseMidline)_data[8];

        /// <summary>
        /// Gets the x-height (byte 9) for Latin Text fonts.
        /// </summary>
        public PanoseXHeight XHeight => (PanoseXHeight)_data[9];
    }

    internal enum PanoseFamilyKind : byte
    {
        Any = 0,
        NoFit = 1,
        LatinText = 2,
        LatinHandWritten = 3,
        LatinDecorative = 4,
        LatinSymbol = 5
    }

    internal enum PanoseSerifStyle : byte
    {
        Any = 0,
        NoFit = 1,
        Cove = 2,
        ObtuseCove = 3,
        SquareCove = 4,
        ObtuseSquareCove = 5,
        Square = 6,
        Thin = 7,
        Oval = 8,
        Exaggerated = 9,
        Triangle = 10,
        NormalSans = 11,
        ObtuseSans = 12,
        PerpendicularSans = 13,
        Flared = 14,
        Rounded = 15
    }

    internal enum PanoseWeight : byte
    {
        Any = 0,
        NoFit = 1,
        VeryLight = 2,
        Light = 3,
        Thin = 4,
        Book = 5,
        Medium = 6,
        Demi = 7,
        Bold = 8,
        Heavy = 9,
        Black = 10,
        ExtraBlack = 11
    }

    internal enum PanoseProportion : byte
    {
        Any = 0,
        NoFit = 1,
        OldStyle = 2,
        Modern = 3,
        EvenWidth = 4,
        Extended = 5,
        Condensed = 6,
        VeryExtended = 7,
        VeryCondensed = 8,
        Monospaced = 9
    }

    internal enum PanoseContrast : byte
    {
        Any = 0,
        NoFit = 1,
        None = 2,
        VeryLow = 3,
        Low = 4,
        MediumLow = 5,
        Medium = 6,
        MediumHigh = 7,
        High = 8,
        VeryHigh = 9,
        HorizontalLow = 10,
        HorizontalMedium = 11,
        HorizontalHigh = 12,
        Broken = 13
    }

    internal enum PanoseStrokeVariation : byte
    {
        Any = 0,
        NoFit = 1,
        NoVariation = 2,
        GradualDiagonal = 3,
        GradualTransitional = 4,
        GradualVertical = 5,
        GradualHorizontal = 6,
        RapidVertical = 7,
        RapidHorizontal = 8,
        InstantVertical = 9,
        InstantHorizontal = 10
    }

    internal enum PanoseArmStyle : byte
    {
        Any = 0,
        NoFit = 1,
        StraightArmsHorizontal = 2,
        StraightArmsWedge = 3,
        StraightArmsVertical = 4,
        StraightArmsSingleSerif = 5,
        StraightArmsDoubleSerif = 6,
        NonStraightArmsHorizontal = 7,
        NonStraightArmsWedge = 8,
        NonStraightArmsVertical = 9,
        NonStraightArmsSingleSerif = 10,
        NonStraightArmsDoubleSerif = 11
    }

    internal enum PanoseLetterform : byte
    {
        Any = 0,
        NoFit = 1,
        NormalContact = 2,
        NormalWeighted = 3,
        NormalBoxed = 4,
        NormalFlattened = 5,
        NormalRounded = 6,
        NormalOffCenter = 7,
        NormalSquare = 8,
        ObliqueContact = 9,
        ObliqueWeighted = 10,
        ObliqueBoxed = 11,
        ObliqueFlattened = 12,
        ObliqueRounded = 13,
        ObliqueOffCenter = 14,
        ObliqueSquare = 15
    }

    internal enum PanoseMidline : byte
    {
        Any = 0,
        NoFit = 1,
        StandardTrimmed = 2,
        StandardPointed = 3,
        StandardSerifed = 4,
        HighTrimmed = 5,
        HighPointed = 6,
        HighSerifed = 7,
        ConstantTrimmed = 8,
        ConstantPointed = 9,
        ConstantSerifed = 10,
        LowTrimmed = 11,
        LowPointed = 12,
        LowSerifed = 13
    }

    internal enum PanoseXHeight : byte
    {
        Any = 0,
        NoFit = 1,
        ConstantSmall = 2,
        ConstantStandard = 3,
        ConstantLarge = 4,
        DuckingSmall = 5,
        DuckingStandard = 6,
        DuckingLarge = 7
    }
}
