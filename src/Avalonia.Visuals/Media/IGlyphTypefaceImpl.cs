// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Media
{  
    public interface IGlyphTypefaceImpl : IDisposable
    {
        double Ascent { get; }
        double Descent { get; }
        double Leading { get; }
        double UnderlinePosition { get; }
        double UnderlineThickness { get; }
        double StrikethroughPosition { get; }
        double StrikethroughThickness { get; }
        ushort CharacterToGlyph(char c);
        ushort CharacterToGlyph(int c);
        double GetHorizontalGlyphAdvance(ushort glyph);
    }
}
