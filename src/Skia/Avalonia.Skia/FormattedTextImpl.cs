// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

using Avalonia.Skia.Text;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia formatted text implementation.
    /// </summary>
    public class FormattedTextImpl : IFormattedTextImpl
    {
        private readonly List<FormattedTextLine> _lines = new List<FormattedTextLine>();

        public FormattedTextImpl(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping textWrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            Constraint = constraint;

            Text = text ?? string.Empty;

            var skiaTypeface = TypefaceCache.GetSKTypeface(typeface);

            TextLayout = new SKTextLayout(text, skiaTypeface, (float)fontSize, textAlignment, textWrapping, constraint, spans);

            foreach (var textLine in TextLayout.TextLines)
            {
                _lines.Add(new FormattedTextLine(textLine.TextPointer.Length, textLine.LineMetrics.Size.Height));
            }

            Size = new Size(TextLayout.Bounds.Width, TextLayout.Bounds.Height);
        }

        public string Text { get; }

        public Size Constraint { get; }

        public Size Size { get; }

        public SKTextLayout TextLayout { get; }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return _lines;
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            return TextLayout.HitTestPoint(point);
        }

        public Rect HitTestTextPosition(int index)
        {
            return TextLayout.HitTestTextPosition(index);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            return TextLayout.HitTestTextRange(index, length);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
