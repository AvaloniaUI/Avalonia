// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Media
{
    public interface IGlyphTypefaceImpl : IDisposable
    {
        short DesignEmHeight { get; }
        int Ascent { get; }
        int Descent { get; }
        int LineGap { get; }
        int UnderlinePosition { get; }
        int UnderlineThickness { get; }
        int StrikethroughPosition { get; }
        int StrikethroughThickness { get; }
        ushort[] GetGlyphs(ReadOnlySpan<int> codePoints);
        int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs);
        IGlyphRunImpl CreateGlyphRun(float fontRenderingEmSize, Point baselineOrigin, IReadOnlyList<ushort> glyphIndices,
            IReadOnlyList<float> glyphAdvances, IReadOnlyList<Vector> glyphOffsets);
    }
}
