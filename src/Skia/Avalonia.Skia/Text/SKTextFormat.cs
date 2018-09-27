// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Skia
{
    using SkiaSharp;

    public class SKTextFormat
    {
        public SKTextFormat(SKTypeface typeface, float fontSize)
        {
            Typeface = typeface;
            FontSize = fontSize;
        }

        public SKTypeface Typeface { get; }

        public float FontSize { get; }
    }
}
