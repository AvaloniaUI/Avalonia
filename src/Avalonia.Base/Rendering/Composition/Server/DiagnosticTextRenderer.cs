using System;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// A class used to render diagnostic strings (only!), with caching of ASCII glyph runs.
    /// </summary>
    internal sealed class DiagnosticTextRenderer
    {
        private const char FirstChar = (char)32;
        private const char LastChar = (char)126;

        private double maxCharGlyphWidth = 0.0;
        private double maxCharGlyphHeight = 0.0;
        private double maxNumberCharGlyphWidth = 0.0;

        private readonly GlyphRun[] _runs = new GlyphRun[LastChar - FirstChar + 1];

        public double GetMaxHeight()
        {
            return maxCharGlyphHeight;
        }

        public DiagnosticTextRenderer(GlyphTypeface glyphTypeface, double fontRenderingEmSize)
        {
            var chars = new char[LastChar - FirstChar + 1];
            for (var c = FirstChar; c <= LastChar; c++)
            {
                var index = c - FirstChar;
                chars[index] = c;
                var glyph = glyphTypeface.CharacterToGlyphMap[c];
                _runs[index] = new GlyphRun(glyphTypeface, fontRenderingEmSize, chars.AsMemory(index, 1), new[] { glyph });

                // Calculate max character width and height
                if (_runs[index].Bounds.Width > maxCharGlyphWidth)
                {
                    maxCharGlyphWidth = _runs[index].Bounds.Width;
                }
                if (_runs[index].Bounds.Height > maxCharGlyphHeight)
                {
                    maxCharGlyphHeight = _runs[index].Bounds.Height;
                }
            }

            // Calculate the fixed cell width for digits (0-9)
            for (var c = '0'; c <= '9'; c++)
            {
                var index = c - FirstChar;
                if (_runs[index].Bounds.Width > maxNumberCharGlyphWidth)
                {
                    maxNumberCharGlyphWidth = _runs[index].Bounds.Width;
                }
            }
        }

        public Size MeasureAsciiText(ReadOnlySpan<char> text)
        {
            var width = 0.0;

            foreach (var c in text)
            {
                var effectiveChar = c is >= FirstChar and <= LastChar ? c : ' ';
                var run = _runs[effectiveChar - FirstChar];
                width += (c >= '0' && c <= '9')? maxNumberCharGlyphWidth : run.Bounds.Width;
            }

            return new Size(width, maxCharGlyphHeight);
        }

        public void DrawAsciiText(ImmediateDrawingContext context, ReadOnlySpan<char> text, IImmutableBrush foreground)
        {
            var offset = 0.0;

            foreach (var c in text)
            {
                var effectiveChar = c is >= FirstChar and <= LastChar ? c : ' ';
                var charIsNumber = c >= '0' && c <= '9';
                var run = _runs[effectiveChar - FirstChar];

                // Center any number character inside a fixed-width cell
                var centeringOffset = charIsNumber? (maxCharGlyphWidth - run.Bounds.Width) / 2.0 : 0.0;

                using (context.PushPreTransform(Matrix.CreateTranslation(offset + centeringOffset, 0.0)))
                    context.PlatformImpl.DrawGlyphRun(foreground, run.PlatformImpl.Item);

                offset += charIsNumber? maxNumberCharGlyphWidth : run.Bounds.Width;
            }

        }
    }
}
