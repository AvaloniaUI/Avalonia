using System;

namespace Avalonia.Media.Fonts.Tables.Glyf
{
    [Flags]
    internal enum CompositeFlags : ushort
    {
        ArgsAreWords = 0x0001,
        ArgsAreXYValues = 0x0002,
        RoundXYToGrid = 0x0004,
        WeHaveAScale = 0x0008,
        MoreComponents = 0x0020,
        WeHaveAnXAndYScale = 0x0040,
        WeHaveATwoByTwo = 0x0080,
        WeHaveInstructions = 0x0100,
        UseMyMetrics = 0x0200,
        OverlapCompound = 0x0400,
        Reserved = 0x1000, // must be ignored
        ScaledComponentOffset = 0x2000,
        UnscaledComponentOffset = 0x4000
    }
}
