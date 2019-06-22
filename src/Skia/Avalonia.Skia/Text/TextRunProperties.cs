// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia.Text
{
    internal readonly struct TextRunProperties
    {
        public TextRunProperties(SKTextPointer textPointer, SKTypeface typeface)
        {
            TextPointer = textPointer;

            Typeface = typeface;
        }

        public SKTextPointer TextPointer { get; }

        public SKTypeface Typeface { get; }
    }
}
