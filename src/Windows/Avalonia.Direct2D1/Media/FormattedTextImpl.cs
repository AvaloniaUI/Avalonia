// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using DWrite = SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        public FormattedTextImpl(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            Text = text;

            using (var textFormat = Direct2D1FontCollectionCache.GetTextFormat(typeface, fontSize))
            {
                textFormat.WordWrapping =
                    wrapping == TextWrapping.Wrap ? DWrite.WordWrapping.Wrap : DWrite.WordWrapping.NoWrap;

                TextLayout = new DWrite.TextLayout(
                                 Direct2D1Platform.DirectWriteFactory,
                                 Text ?? string.Empty,
                                 textFormat,
                                 (float)constraint.Width,
                                 (float)constraint.Height)
                {
                    TextAlignment = textAlignment.ToDirect2D()
                };
            }

            if (spans != null)
            {
                foreach (var span in spans)
                {
                    ApplySpan(span);
                }
            }

            Bounds = Measure();
        }

        public Size Constraint => new Size(TextLayout.MaxWidth, TextLayout.MaxHeight);

        public Rect Bounds { get; }

        public string Text { get; }

        public DWrite.TextLayout TextLayout { get; }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            var result = TextLayout.GetLineMetrics();
            return from line in result select new FormattedTextLine(line.Length, line.Height);
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            var result = TextLayout.HitTestPoint(
                (float)point.X,
                (float)point.Y,
                out var isTrailingHit,
                out var isInside);

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = result.TextPosition,
                Length = result.Length,
                Bounds = new Rect(result.Left, result.Top, result.Width, result.Height),
                IsTrailing = isTrailingHit,
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            var result = TextLayout.HitTestTextPosition(index, false, out _, out _);

            return new Rect(result.Left, result.Top, result.Width, result.Height);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var result = TextLayout.HitTestTextRange(index, length, 0, 0);
            return result.Select(x => new Rect(x.Left, x.Top, x.Width, x.Height));
        }

        private void ApplySpan(FormattedTextStyleSpan span)
        {
            if (span.Length > 0)
            {
                var range = new DWrite.TextRange(span.StartIndex, span.Length);

                if (span.Foreground != null)
                {
                    TextLayout.SetDrawingEffect(
                        new BrushWrapper(span.Foreground.ToImmutable()),
                        range);
                }

                if (span.Typeface != null)
                {
                    TextLayout.SetFontFamilyName(span.Typeface.FontFamily.Name, range);
                    TextLayout.SetFontStyle((DWrite.FontStyle)span.Typeface.Style, range);
                    TextLayout.SetFontWeight((DWrite.FontWeight)span.Typeface.Weight, range);
                }

                if (span.FontSize != null)
                {
                    TextLayout.SetFontSize((float)span.FontSize, range);
                }
            }
        }

        private Rect Measure()
        {
            var metrics = TextLayout.Metrics;

            var width = metrics.WidthIncludingTrailingWhitespace;

            if (float.IsNaN(width))
            {
                width = metrics.Width;
            }

            return new Rect(
                TextLayout.Metrics.Left,
                TextLayout.Metrics.Top,
                width,
                TextLayout.Metrics.Height);
        }
    }
}
