using System;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// A class used to render diagnostic strings (only!), with caching of ASCII glyph runs.
    /// Digits are laid out in equal-width cells so rapidly changing values do not make the text jump around.
    /// </summary>
    internal sealed class DiagnosticTextRenderer
    {
        private const char FirstChar = (char)32;
        private const char LastChar = (char)126;

        private readonly GlyphTypeface _glyphTypeface;
        private readonly double _fontRenderingEmSize;
        private readonly GlyphRun[] _runs = new GlyphRun[LastChar - FirstChar + 1];
        private readonly double[] _digitOffsets = new double[10];
        private readonly double _lineHeight;
        private readonly double _digitCellWidth;

        public double GetMaxHeight()
        {
            return _lineHeight;
        }

        /// <summary>
        /// The width of the cell every digit is centered in.
        /// </summary>
        public double DigitCellWidth => _digitCellWidth;

        public DiagnosticTextRenderer(GlyphTypeface glyphTypeface, double fontRenderingEmSize)
        {
            _glyphTypeface = glyphTypeface;
            _fontRenderingEmSize = fontRenderingEmSize;
            _lineHeight = glyphTypeface.Metrics.LineSpacing * fontRenderingEmSize / glyphTypeface.Metrics.DesignEmHeight;

            var chars = new char[LastChar - FirstChar + 1];
            for (var c = FirstChar; c <= LastChar; c++)
            {
                var index = c - FirstChar;
                chars[index] = c;
                var glyph = glyphTypeface.CharacterToGlyphMap[c];
                _runs[index] = new GlyphRun(glyphTypeface, fontRenderingEmSize, chars.AsMemory(index, 1), new[] { glyph });
            }

            // Digits share the width of the widest one so changing values do not shift the text that follows
            for (var c = '0'; c <= '9'; c++)
            {
                _digitCellWidth = Math.Max(_digitCellWidth, _runs[c - FirstChar].Bounds.Width);
            }

            for (var digit = 0; digit < _digitOffsets.Length; digit++)
            {
                _digitOffsets[digit] = (_digitCellWidth - _runs['0' + digit - FirstChar].Bounds.Width) / 2.0;
            }
        }

        /// <summary>
        /// Shapes <paramref name="text"/> into a single glyph run that can be drawn with
        /// <see cref="DrawShapedText"/> without any per-frame measuring or shaping.
        /// Glyphs advance by their natural width, so digits are not placed in cells; use
        /// <see cref="DrawDigits"/> for values that change.
        /// </summary>
        public ShapedText Shape(string text)
        {
            var chars = new char[text.Length];
            var glyphs = new ushort[text.Length];

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i] is >= FirstChar and <= LastChar ? text[i] : ' ';
                chars[i] = c;
                glyphs[i] = _glyphTypeface.CharacterToGlyphMap[c];
            }

            return new ShapedText(new GlyphRun(_glyphTypeface, _fontRenderingEmSize, chars, glyphs));
        }

        public void DrawShapedText(ImmediateDrawingContext context, ShapedText text, IImmutableBrush foreground)
        {
            if (text.GlyphRun.GlyphInfos.Count > 0)
                context.PlatformImpl.DrawGlyphRun(foreground, text.GlyphRun.PlatformImpl.Item);
        }

        /// <summary>
        /// Draws a non-negative <paramref name="value"/> zero padded to <paramref name="digitCount"/> digits,
        /// each in its own <see cref="DigitCellWidth"/> wide cell, without formatting a string.
        /// Values that do not fit are clamped to the largest representable value.
        /// </summary>
        public void DrawDigits(ImmediateDrawingContext context, int value, int digitCount, IImmutableBrush foreground)
        {
            var divisor = 1L;
            for (var i = 1; i < digitCount; i++)
                divisor *= 10;

            var remaining = Math.Clamp(value, 0, divisor * 10 - 1);

            for (var i = 0; i < digitCount; i++)
            {
                var digit = (int)(remaining / divisor);
                remaining %= divisor;
                divisor /= 10;

                using (context.PushPreTransform(Matrix.CreateTranslation(i * _digitCellWidth + _digitOffsets[digit], 0.0)))
                    context.PlatformImpl.DrawGlyphRun(foreground, _runs['0' + digit - FirstChar].PlatformImpl.Item);
            }
        }

        public Size MeasureAsciiText(ReadOnlySpan<char> text)
        {
            var width = 0.0;

            foreach (var c in text)
            {
                var effectiveChar = c is >= FirstChar and <= LastChar ? c : ' ';
                var run = _runs[effectiveChar - FirstChar];
                width += c is >= '0' and <= '9' ? _digitCellWidth : run.Bounds.Width;
            }

            return new Size(width, _lineHeight);
        }

        public void DrawAsciiText(ImmediateDrawingContext context, ReadOnlySpan<char> text, IImmutableBrush foreground)
        {
            var offset = 0.0;

            foreach (var c in text)
            {
                var effectiveChar = c is >= FirstChar and <= LastChar ? c : ' ';
                var charIsNumber = c is >= '0' and <= '9';
                var run = _runs[effectiveChar - FirstChar];

                // Center digits inside the fixed-width digit cell
                var centeringOffset = charIsNumber ? (_digitCellWidth - run.Bounds.Width) / 2.0 : 0.0;

                using (context.PushPreTransform(Matrix.CreateTranslation(offset + centeringOffset, 0.0)))
                    context.PlatformImpl.DrawGlyphRun(foreground, run.PlatformImpl.Item);

                offset += charIsNumber ? _digitCellWidth : run.Bounds.Width;
            }

        }

        /// <summary>
        /// A piece of text shaped once by <see cref="Shape"/>.
        /// </summary>
        public sealed class ShapedText : IDisposable
        {
            internal ShapedText(GlyphRun glyphRun)
            {
                GlyphRun = glyphRun;
                Width = glyphRun.Bounds.Width;
            }

            internal GlyphRun GlyphRun { get; }

            public double Width { get; }

            public void Dispose() => GlyphRun.Dispose();
        }
    }
}
