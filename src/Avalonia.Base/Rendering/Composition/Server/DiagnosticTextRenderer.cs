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

        private readonly GlyphRun[] _runs = new GlyphRun[LastChar - FirstChar + 1];
        private readonly double _lineHeight;
        private readonly double _digitCellWidth;

        public double GetMaxHeight()
        {
            return _lineHeight;
        }

        public DiagnosticTextRenderer(GlyphTypeface glyphTypeface, double fontRenderingEmSize)
        {
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
    }
}
