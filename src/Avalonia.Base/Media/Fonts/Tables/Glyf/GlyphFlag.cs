using System;

namespace Avalonia.Media.Fonts.Tables.Glyf
{
    [Flags]
    internal enum GlyphFlag : byte
    {
        None = 0x00,
        OnCurvePoint = 0x01,
        XShortVector = 0x02,
        YShortVector = 0x04,
        Repeat = 0x08,
        XIsSameOrPositiveXShortVector = 0x10,
        YIsSameOrPositiveYShortVector = 0x20,
        OverlapSimple = 0x40,
        Reserved = 0x80
    }
}
