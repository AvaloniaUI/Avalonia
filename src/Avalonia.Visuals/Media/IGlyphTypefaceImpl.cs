// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Media
{
    public interface IGlyphTypefaceImpl
    {
        short DesignEmHeight { get; }
        int Ascent { get; }
        int Descent { get; }
        int LineGap { get; }
        int UnderlinePosition { get; }
        int UnderlineThickness { get; }
        int StrikethroughPosition { get; }
        int StrikethroughThickness { get; }
        ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints);
        ReadOnlySpan<int> GetGlyphAdvances(ReadOnlySpan<ushort> glyphs);
    }
}
