// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Media
{
    public interface IGlyphTypefaceImpl : IDisposable
    {
        int Ascent { get; }
        int Descent { get; }
        int LineGap { get; }
        int UnderlinePosition { get; }
        int UnderlineThickness { get; }
        int StrikethroughPosition { get; }
        int StrikethroughThickness { get; }
        ReadOnlySpan<short> GetGlyphs(ReadOnlySpan<int> text);
        ReadOnlySpan<int> GetGlyphAdvances(ReadOnlySpan<short> glyphs);
    }
}
