using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds a shaped glyph run.
    /// </summary>
    public sealed class ShapedTextRun : DrawableTextRun
    {
        public ShapedTextRun(ReadOnlySlice<char> text, TextStyle style) : this(
            TextShaper.Current.ShapeText(text, style.TextFormat), style)
        {
        }

        public ShapedTextRun(GlyphRun glyphRun, TextStyle style)
        {
            Text = glyphRun.Characters;
            Style = style;
            GlyphRun = glyphRun;
        }

        /// <inheritdoc/>
        public override Rect Bounds => GlyphRun.Bounds;

        /// <summary>
        /// Gets the glyph run.
        /// </summary>
        /// <value>
        /// The glyphs.
        /// </value>
        public GlyphRun GlyphRun { get; }

        /// <inheritdoc/>
        public override void Draw(IDrawingContextImpl drawingContext, Point origin)
        {
            if (GlyphRun.GlyphIndices.Length == 0)
            {
                return;
            }

            if (Style.TextFormat.Typeface == null)
            {
                return;
            }

            if (Style.Foreground == null)
            {
                return;
            }

            drawingContext.DrawGlyphRun(Style.Foreground, GlyphRun, origin);

            if (Style.TextDecorations == null)
            {
                return;
            }

            foreach (var textDecoration in Style.TextDecorations)
            {
                DrawTextDecoration(drawingContext, textDecoration, origin);
            }
        }

        /// <summary>
        /// Draws the <see cref="TextDecoration"/> at given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="textDecoration">The text decoration.</param>
        /// <param name="origin">The origin.</param>
        private void DrawTextDecoration(IDrawingContextImpl drawingContext, ImmutableTextDecoration textDecoration, Point origin)
        {
            var textFormat = Style.TextFormat;

            var fontMetrics = Style.TextFormat.FontMetrics;

            var thickness = textDecoration.Pen?.Thickness ?? 1.0;

            switch (textDecoration.PenThicknessUnit)
            {
                case TextDecorationUnit.FontRecommended:
                    switch (textDecoration.Location)
                    {
                        case TextDecorationLocation.Underline:
                            thickness = fontMetrics.UnderlineThickness;
                            break;
                        case TextDecorationLocation.Strikethrough:
                            thickness = fontMetrics.StrikethroughThickness;
                            break;
                    }
                    break;
                case TextDecorationUnit.FontRenderingEmSize:
                    thickness = textFormat.FontRenderingEmSize * thickness;
                    break;
            }

            switch (textDecoration.Location)
            {
                case TextDecorationLocation.Overline:
                    origin += new Point(0, textFormat.FontMetrics.Ascent);
                    break;
                case TextDecorationLocation.Strikethrough:
                    origin += new Point(0, -textFormat.FontMetrics.StrikethroughPosition);
                    break;
                case TextDecorationLocation.Underline:
                    origin += new Point(0, -textFormat.FontMetrics.UnderlinePosition);
                    break;
            }

            switch (textDecoration.PenOffsetUnit)
            {
                case TextDecorationUnit.FontRenderingEmSize:
                    origin += new Point(0, textDecoration.PenOffset * textFormat.FontRenderingEmSize);
                    break;
                case TextDecorationUnit.Pixel:
                    origin += new Point(0, textDecoration.PenOffset);
                    break;
            }

            var pen = new ImmutablePen(
                textDecoration.Pen?.Brush ?? Style.Foreground.ToImmutable(),
                thickness,
                textDecoration.Pen?.DashStyle?.ToImmutable(),
                textDecoration.Pen?.LineCap ?? default,
                textDecoration.Pen?.LineJoin ?? PenLineJoin.Miter,
                textDecoration.Pen?.MiterLimit ?? 10.0);

            drawingContext.DrawLine(pen, origin, origin + new Point(GlyphRun.Bounds.Width, 0));
        }

        /// <summary>
        /// Splits the <see cref="TextRun"/> at specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>The split result.</returns>
        public SplitTextCharactersResult Split(int length)
        {
            var glyphCount = 0;

            var firstCharacters = GlyphRun.Characters.Take(length);

            var codepointEnumerator = new CodepointEnumerator(firstCharacters);

            while (codepointEnumerator.MoveNext())
            {
                glyphCount++;
            }

            if (GlyphRun.Characters.Length == length)
            {
                return new SplitTextCharactersResult(this, null);
            }

            if (GlyphRun.GlyphIndices.Length == glyphCount)
            {
                return new SplitTextCharactersResult(this, null);
            }

            var firstGlyphRun = new GlyphRun(
                Style.TextFormat.Typeface.GlyphTypeface,
                Style.TextFormat.FontRenderingEmSize,
                GlyphRun.GlyphIndices.Take(glyphCount),
                GlyphRun.GlyphAdvances.Take(glyphCount),
                GlyphRun.GlyphOffsets.Take(glyphCount),
                GlyphRun.Characters.Take(length),
                GlyphRun.GlyphClusters.Take(length));

            var firstTextRun = new ShapedTextRun(firstGlyphRun, Style);

            var secondGlyphRun = new GlyphRun(
                Style.TextFormat.Typeface.GlyphTypeface,
                Style.TextFormat.FontRenderingEmSize,
                GlyphRun.GlyphIndices.Skip(glyphCount),
                GlyphRun.GlyphAdvances.Skip(glyphCount),
                GlyphRun.GlyphOffsets.Skip(glyphCount),
                GlyphRun.Characters.Skip(length),
                GlyphRun.GlyphClusters.Skip(length));

            var secondTextRun = new ShapedTextRun(secondGlyphRun, Style);

            return new SplitTextCharactersResult(firstTextRun, secondTextRun);
        }

        public readonly struct SplitTextCharactersResult
        {
            public SplitTextCharactersResult(ShapedTextRun first, ShapedTextRun second)
            {
                First = first;

                Second = second;
            }

            /// <summary>
            /// Gets the first text run.
            /// </summary>
            /// <value>
            /// The first text run.
            /// </value>
            public ShapedTextRun First { get; }

            /// <summary>
            /// Gets the second text run.
            /// </summary>
            /// <value>
            /// The second text run.
            /// </value>
            public ShapedTextRun Second { get; }
        }
    }
}
