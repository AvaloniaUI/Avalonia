// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class TypefaceCollectionEntry
    {
        public TypefaceCollectionEntry(Typeface typeface, SKTypeface skTypeface)
        {
            Typeface = typeface;
            SKTypeface = skTypeface;
        }
        public Typeface Typeface { get; }
        public SKTypeface SKTypeface { get; }
    }
}
