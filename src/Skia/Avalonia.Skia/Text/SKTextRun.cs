// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia
{
    public class SKTextRun
    {
        public SKTextRun(string text, SKTextFormat textFormat, SKFontMetrics fontMetrics, float width)
        {
            Text = text;
            TextFormat = textFormat;
            FontMetrics = fontMetrics;
            Width = width;
        }

        public string Text { get; }

        public SKTextFormat TextFormat { get; }

        public IBrush DrawingEffect { get; }

        public SKFontMetrics FontMetrics { get; }

        public float Width { get; }
    }
}
