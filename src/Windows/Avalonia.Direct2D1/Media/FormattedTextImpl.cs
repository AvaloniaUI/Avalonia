using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using DWrite = Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class FormattedTextImpl : IFormattedTextImpl
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

            var font = ((GlyphTypefaceImpl)typeface.GlyphTypeface.PlatformImpl).DWFont;
            var familyName = font.FontFamily.FamilyNames.GetString(0);
            using (var textFormat = Direct2D1Platform.DirectWriteFactory.CreateTextFormat( 
                familyName,
                font.FontFamily.FontCollection,
                (DWrite.FontWeight)typeface.Weight,
                (DWrite.FontStyle)typeface.Style,
                DWrite.FontStretch.Normal,
                (float)fontSize))
            {
                textFormat.WordWrapping =
                    wrapping == TextWrapping.Wrap ? DWrite.WordWrapping.Wrap : DWrite.WordWrapping.NoWrap;

                TextLayout = Direct2D1Platform.DirectWriteFactory.CreateTextLayout(
                    Text ?? string.Empty,
                    textFormat,
                    (float)constraint.Width,
                    (float)constraint.Height);
                TextLayout.TextAlignment = textAlignment.ToDirect2D();
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

        public DWrite.IDWriteTextLayout TextLayout { get; }

        public IEnumerable<FormattedTextLine> GetLines() => from line in TextLayout.LineMetrics
                                                            select new FormattedTextLine(line.Length, line.Height);

        public TextHitTestResult HitTestPoint(Point point)
        {
            TextLayout.HitTestPoint(
                (float)point.X,
                (float)point.Y,
                out var isTrailingHit,
                out var isInside,
                out var result
            );

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = result.TextPosition,
                IsTrailing = isTrailingHit,
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            TextLayout.HitTestTextPosition(index, false, out _, out _, out var result);

            return HitTestMetricsToRect(result);
        }

        private static Rect HitTestMetricsToRect(DWrite.HitTestMetrics result) =>
            new(result.Left, result.Top, result.Width, result.Height);

        public IEnumerable<Rect> HitTestTextRange(int index, int length) =>
            TextLayout.HitTestTextRange(index, length, 0, 0).Select(HitTestMetricsToRect);

        private void ApplySpan(FormattedTextStyleSpan span)
        {
            if (span.Length > 0)
            {
                if (span.ForegroundBrush != null)
                {
                    TextLayout.SetDrawingEffect(
                        new BrushWrapper(span.ForegroundBrush.ToImmutable()),
                        new DWrite.TextRange { StartPosition = span.StartIndex, Length = span.Length });
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
