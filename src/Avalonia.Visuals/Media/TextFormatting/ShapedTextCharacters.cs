﻿using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds shaped characters.
    /// </summary>
    public sealed class ShapedTextCharacters : DrawableTextRun
    {
        public ShapedTextCharacters(GlyphRun glyphRun, TextRunProperties properties)
        {
            Text = glyphRun.Characters;
            Properties = properties;
            TextSourceLength = Text.Length;
            FontMetrics = new FontMetrics(Properties.Typeface, Properties.FontRenderingEmSize);
            GlyphRun = glyphRun;
        }

        /// <inheritdoc/>
        public override ReadOnlySlice<char> Text { get; }

        /// <inheritdoc/>
        public override TextRunProperties Properties { get; }

        /// <inheritdoc/>
        public override int TextSourceLength { get; }

        /// <inheritdoc/>
        public override Rect Bounds => GlyphRun.Bounds;

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        /// <value>
        /// The font metrics.
        /// </value>
        public FontMetrics FontMetrics { get; }

        /// <summary>
        /// Gets the glyph run.
        /// </summary>
        /// <value>
        /// The glyphs.
        /// </value>
        public GlyphRun GlyphRun { get; }

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            if (GlyphRun.GlyphIndices.Length == 0)
            {
                return;
            }

            if (Properties.Typeface == null)
            {
                return;
            }

            if (Properties.ForegroundBrush == null)
            {
                return;
            }

            if (Properties.BackgroundBrush != null)
            {
                drawingContext.DrawRectangle(Properties.BackgroundBrush, null,
                new Rect(origin.X, origin.Y + FontMetrics.Ascent, Bounds.Width, Bounds.Height));
            }

            drawingContext.DrawGlyphRun(Properties.ForegroundBrush, GlyphRun, origin);

            if (Properties.TextDecorations == null)
            {
                return;
            }

            foreach (var textDecoration in Properties.TextDecorations)
            {
                textDecoration.Draw(drawingContext, this, origin);
            }
        }

        /// <summary>
        /// Splits the <see cref="TextRun"/> at specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>The split result.</returns>
        public SplitTextCharactersResult Split(int length)
        {
            var glyphCount = GlyphRun.FindGlyphIndex(GlyphRun.Characters.Start + length);

            if (GlyphRun.Characters.Length == length)
            {
                return new SplitTextCharactersResult(this, null);
            }

            if (GlyphRun.GlyphIndices.Length == glyphCount)
            {
                return new SplitTextCharactersResult(this, null);
            }

            var firstGlyphRun = new GlyphRun(
                Properties.Typeface.GlyphTypeface,
                Properties.FontRenderingEmSize,
                GlyphRun.GlyphIndices.Take(glyphCount),
                GlyphRun.GlyphAdvances.Take(glyphCount),
                GlyphRun.GlyphOffsets.Take(glyphCount),
                GlyphRun.Characters.Take(length),
                GlyphRun.GlyphClusters.Take(glyphCount));

            var firstTextRun = new ShapedTextCharacters(firstGlyphRun, Properties);

            var secondGlyphRun = new GlyphRun(
                Properties.Typeface.GlyphTypeface,
                Properties.FontRenderingEmSize,
                GlyphRun.GlyphIndices.Skip(glyphCount),
                GlyphRun.GlyphAdvances.Skip(glyphCount),
                GlyphRun.GlyphOffsets.Skip(glyphCount),
                GlyphRun.Characters.Skip(length),
                GlyphRun.GlyphClusters.Skip(glyphCount));

            var secondTextRun = new ShapedTextCharacters(secondGlyphRun, Properties);

            return new SplitTextCharactersResult(firstTextRun, secondTextRun);
        }

        public readonly struct SplitTextCharactersResult
        {
            public SplitTextCharactersResult(ShapedTextCharacters first, ShapedTextCharacters second)
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
            public ShapedTextCharacters First { get; }

            /// <summary>
            /// Gets the second text run.
            /// </summary>
            /// <value>
            /// The second text run.
            /// </value>
            public ShapedTextCharacters Second { get; }
        }
    }
}
