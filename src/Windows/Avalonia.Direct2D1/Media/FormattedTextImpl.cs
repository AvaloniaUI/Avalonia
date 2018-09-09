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
        private readonly List<DrawingEffect> _drawingEffects;

        public FormattedTextImpl(
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            Text = text;

            _drawingEffects = new List<DrawingEffect>(spans?.Count ?? 0);

            var factory = AvaloniaLocator.Current.GetService<DWrite.Factory>();

            var textFormat = Direct2D1FontCollectionCache.GetTextFormat(typeface);

            textFormat.WordWrapping =
                wrapping == TextWrapping.Wrap ? DWrite.WordWrapping.Wrap : DWrite.WordWrapping.NoWrap;

            TextLayout = new DWrite.TextLayout(
                             factory,
                             Text ?? string.Empty,
                             textFormat,
                             (float)constraint.Width,
                             (float)constraint.Height)
            {
                TextAlignment = textAlignment.ToDirect2D()
            };

            textFormat.Dispose();

            if (spans != null)
            {
                foreach (var span in spans)
                {
                    ApplySpan(span);
                }
            }

            Size = Measure();
        }

        public Size Constraint => new Size(TextLayout.MaxWidth, TextLayout.MaxHeight);

        public Size Size { get; }

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

        public void ApplyDrawingEffects(DrawingContextImpl drawingContextImpl)
        {
            foreach (var spanDrawingEffect in _drawingEffects)
            {
                spanDrawingEffect.BrushImpl = drawingContextImpl.CreateBrush(spanDrawingEffect.Brush, new Size());

                TextLayout.SetDrawingEffect(spanDrawingEffect.BrushImpl.PlatformBrush.NativePointer, spanDrawingEffect.Range);
            }
        }

        public void CleanUpDrawingEffects()
        {
            foreach (var drawingEffect in _drawingEffects)
            {
                drawingEffect.BrushImpl?.Dispose();
            }
        }

        private void ApplySpan(FormattedTextStyleSpan span)
        {
            if (span.Length > 0)
            {
                if (span.ForegroundBrush != null)
                {
                    _drawingEffects.Add(new DrawingEffect { Brush = span.ForegroundBrush, Range = new DWrite.TextRange(span.StartIndex, span.Length) });
                }
            }
        }

        private Size Measure()
        {
            var metrics = TextLayout.Metrics;
            var width = metrics.WidthIncludingTrailingWhitespace;

            if (float.IsNaN(width))
            {
                width = metrics.Width;
            }

            return new Size(width, TextLayout.Metrics.Height);
        }

        private class DrawingEffect
        {
            public DWrite.TextRange Range { get; set; }

            public IBrush Brush { get; set; }

            public BrushImpl BrushImpl { get; set; }
        }
    }
}
