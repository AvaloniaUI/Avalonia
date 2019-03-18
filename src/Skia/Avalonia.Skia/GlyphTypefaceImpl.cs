// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

using Avalonia.Media;

namespace Avalonia.Skia
{
    internal class GlyphTypefaceImpl : IGlyphTypefaceImpl
    {
        public int GlyphCount { get; }

        public IDictionary<int, ushort> CharacterToGlyphMap { get; }

        public double Baseline { get; }

        public double UnderlinePosition { get; }

        public double UnderlineThickness { get; }

        public double StrikethroughPosition { get; }

        public double StrikethroughThickness { get; }
    }
}
