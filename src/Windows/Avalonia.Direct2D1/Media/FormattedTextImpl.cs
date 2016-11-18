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
            FontWeight fontWeight,
            TextWrapping wrapping,
            Size constraint)
        {
            Text = text;
            TextLayout = Create(
                text,
                fontFamily,
                fontSize,
                (DWrite.FontStyle)fontStyle,
                (DWrite.TextAlignment)textAlignment,
                (DWrite.FontWeight)fontWeight,
                wrapping == TextWrapping.Wrap ? DWrite.WordWrapping.Wrap : DWrite.WordWrapping.NoWrap,
                (float)constraint.Width,
                (float)constraint.Height);
            Size = Measure();
        }

        public FormattedTextImpl(string text, DWrite.TextLayout textLayout)
        {
            Text = text;
            TextLayout = textLayout;
            Size = Measure();
        }

        public Size Constraint => new Size(TextLayout.MaxWidth, TextLayout.MaxHeight);

        public Size Size { get; }

        public string Text { get; }

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

        public void SetForegroundBrush(IBrush brush, int startIndex, int count)
        {
            TextLayout.SetDrawingEffect(
                new BrushWrapper(brush),
                new DWrite.TextRange(startIndex, count));
        }

        public IFormattedTextImpl WithConstraint(Size constraint)
        {
            var factory = AvaloniaLocator.Current.GetService<DWrite.Factory>();
            return new FormattedTextImpl(Text, Create(
                Text,
                TextLayout.FontFamilyName,
                TextLayout.FontSize,
                TextLayout.FontStyle,
                TextLayout.TextAlignment,
                TextLayout.FontWeight,
                TextLayout.WordWrapping,
                (float)constraint.Width,
                (float)constraint.Height));
        }

        private static DWrite.TextLayout Create(
            string text,
            string fontFamily,
            double fontSize,
            DWrite.FontStyle fontStyle,
            DWrite.TextAlignment textAlignment,
            DWrite.FontWeight fontWeight,
            DWrite.WordWrapping wrapping,
            float constraintX,
            float constraintY)
        {
            var factory = AvaloniaLocator.Current.GetService<DWrite.Factory>();

            using (var format = new DWrite.TextFormat(
                factory,
                fontFamily,
                fontWeight,
                fontStyle,
                (float)fontSize))
            {
                format.WordWrapping = wrapping;

                var result = new DWrite.TextLayout(
                    factory,
                    text ?? string.Empty,
                    format,
                    constraintX,
                    constraintY);
                result.TextAlignment = textAlignment;
                return result;
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
    }
}
