// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            var factory = AvaloniaLocator.Current.GetService<DWrite.Factory>();

            using (var format = new DWrite.TextFormat(
                factory,
                fontFamily,
                (DWrite.FontWeight)fontWeight,
                (DWrite.FontStyle)fontStyle,
                (float)fontSize))
            {
                TextLayout = new DWrite.TextLayout(
                    factory,
                    text ?? string.Empty,
                    format,
                    float.MaxValue,
                    float.MaxValue);
            }

            TextLayout.TextAlignment = textAlignment.ToDirect2D();
        }

        public Size Constraint
        {
            get
            {
                return new Size(TextLayout.MaxWidth, TextLayout.MaxHeight);
            }

            set
            {
                TextLayout.MaxWidth = (float)value.Width;
                TextLayout.MaxHeight = (float)value.Height;
            }
        }

        public DWrite.TextLayout TextLayout { get; }

        public void Dispose()
        {
            TextLayout.Dispose();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            var result = TextLayout.GetLineMetrics();
            return from line in result select new FormattedTextLine(line.Length, line.Height);
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            SharpDX.Mathematics.Interop.RawBool isTrailingHit;
            SharpDX.Mathematics.Interop.RawBool isInside;

            var result = TextLayout.HitTestPoint(
                (float)point.X,
                (float)point.Y,
                out isTrailingHit,
                out isInside);

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = result.TextPosition,
                IsTrailing = isTrailingHit,
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            float x;
            float y;

            var result = TextLayout.HitTestTextPosition(
                index,
                false,
                out x,
                out y);

            return new Rect(result.Left, result.Top, result.Width, result.Height);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var result = TextLayout.HitTestTextRange(index, length, 0, 0);
            return result.Select(x => new Rect(x.Left, x.Top, x.Width, x.Height));
        }

        public Size Measure()
        {
            var metrics = TextLayout.Metrics;
            var width = metrics.WidthIncludingTrailingWhitespace;

            if (float.IsNaN(width))
            {
                width = metrics.Width;
            }

            return new Size(width, TextLayout.Metrics.Height);
        }

        public void SetForegroundBrush(IBrush brush, int startIndex, int count)
        {
            TextLayout.SetDrawingEffect(
                new BrushWrapper(brush),
                new DWrite.TextRange(startIndex, count));
        }
    }
}
