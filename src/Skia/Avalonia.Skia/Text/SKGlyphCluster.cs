// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia
{
    public class SKGlyphCluster
    {
        public SKGlyphCluster(int textPosition, int length, SKRect bounds)
        {
            TextPosition = textPosition;
            Length = length;
            Bounds = bounds;
        }

        public int TextPosition { get; }

        public int Length { get; }

        public SKRect Bounds { get; }
    }
}
