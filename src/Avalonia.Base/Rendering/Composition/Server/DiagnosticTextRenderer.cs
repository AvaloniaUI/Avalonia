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

        private readonly GlyphRun[] _runs = new GlyphRun[LastChar - FirstChar + 1];

        public double GetMaxHeight()
        {
            var maxHeight = 0.0;

            for (var c = FirstChar; c <= LastChar; c++)
            {
                var height = _runs[c - FirstChar].Bounds.Height;
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }

            return maxHeight;
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
            }
        }

        public Size MeasureAsciiText(ReadOnlySpan<char> text)
        {
            var width = 0.0;
            var height = 0.0;

            foreach (var c in text)
            {
                var effectiveChar = c is >= FirstChar and <= LastChar ? c : ' ';
                var run = _runs[effectiveChar - FirstChar];
                width += run.Bounds.Width;
                height = Math.Max(height, run.Bounds.Height);
            }

            return new Size(width, height);
        }

        public void DrawAsciiText(ImmediateDrawingContext context, ReadOnlySpan<char> text, IImmutableBrush foreground)
        {
            var offset = 0.0;

            foreach (var c in text)
            {
                var effectiveChar = c is >= FirstChar and <= LastChar ? c : ' ';
                var run = _runs[effectiveChar - FirstChar];
                using (context.PushPreTransform(Matrix.CreateTranslation(offset, 0.0)))
                    context.PlatformImpl.DrawGlyphRun(foreground, run.PlatformImpl.Item);
                offset += run.Bounds.Width;
            }

        }
    }
}
