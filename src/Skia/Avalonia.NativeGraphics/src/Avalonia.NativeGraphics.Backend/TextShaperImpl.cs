using System;
using System.Globalization;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Utilities;
using HarfBuzzSharp;

namespace Avalonia.NativeGraphics.Backend
{
    internal unsafe class TextShaperImpl : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlySlice<char> text, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;
            var culture = options.Culture;
            GlyphTypefaceImpl avgGlyphTypeface = (GlyphTypefaceImpl)typeface.PlatformImpl;

            var buffer = new AvgFontShapeBuffer(avgGlyphTypeface);

            buffer.AddUtf16(text.Buffer.Span, text.BufferOffset, text.Length);

            MergeBreakPair(buffer);

            buffer.GuessSegmentProperties();

            buffer.SetDirection((int)Direction.LeftToRight); //Always shape LeftToRight

            buffer.SetLanguage(new Language(culture ?? CultureInfo.CurrentCulture).Handle);

            buffer.Shape();

            var scaleX = buffer.GetScale();

            var textScale = fontRenderingEmSize / scaleX;

            var bufferLength = buffer.Length;

            var shapedBuffer = new ShapedBuffer(text, bufferLength, typeface, fontRenderingEmSize, bidiLevel);

            var glyphInfos = buffer.GetGlyphInfoSpan();

            var glyphPositions = buffer.GetGlyphPositionSpan();

            for (var i = 0; i < bufferLength; i++)
            {
                var sourceInfo = glyphInfos[i];

                var glyphIndex = (ushort)sourceInfo.codepoint;

                var glyphCluster = (int)(sourceInfo.cluster);

                var glyphAdvance = GetGlyphAdvance(glyphPositions, i, textScale);

                var glyphOffset = GetGlyphOffset(glyphPositions, i, textScale);

                if (glyphIndex == 0 && text.Buffer.Span[glyphCluster] == '\t')
                {
                    glyphIndex = typeface.GetGlyph(' ');

                    glyphAdvance = options.IncrementalTabWidth > 0 ?
                        options.IncrementalTabWidth :
                        4 * typeface.GetGlyphAdvance(glyphIndex) * textScale;
                }

                var targetInfo = new Media.TextFormatting.GlyphInfo(glyphIndex, glyphCluster, glyphAdvance, glyphOffset);

                shapedBuffer[i] = targetInfo;
            }

            return shapedBuffer;

        }

        private static void MergeBreakPair(AvgFontShapeBuffer buffer)
        {
            var length = buffer.Length;

            var glyphInfos = buffer.GetGlyphInfoSpan();

            var second = glyphInfos[length - 1];

            if (!new Codepoint(second.codepoint).IsBreakChar)
            {
                return;
            }

            if (length > 1 && glyphInfos[length - 2].codepoint == '\r' && second.codepoint == '\n')
            {
                var first = glyphInfos[length - 2];

                first.codepoint = '\u200C';
                second.codepoint = '\u200C';
                second.cluster = first.cluster;

                unsafe
                {
                    fixed (AvgGlyphInfo* p = &glyphInfos[length - 2])
                    {
                        *p = first;
                    }

                    fixed (AvgGlyphInfo* p = &glyphInfos[length - 1])
                    {
                        *p = second;
                    }
                }
            }
            else
            {
                second.codepoint = '\u200C';

                unsafe
                {
                    fixed (AvgGlyphInfo* p = &glyphInfos[length - 1])
                    {
                        *p = second;
                    }
                }
            }
        }

        private static Vector GetGlyphOffset(ReadOnlySpan<AvgGlphPosition> glyphPositions, int index, double textScale)
        {
            var position = glyphPositions[index];

            var offsetX = position.x_offset * textScale;

            var offsetY = position.y_offset * textScale;

            return new Vector(offsetX, offsetY);
        }

        private static double GetGlyphAdvance(ReadOnlySpan<AvgGlphPosition> glyphPositions, int index, double textScale)
        {
            // Depends on direction of layout
            // glyphPositions[index].YAdvance * textScale;
            return glyphPositions[index].x_advance * textScale;
        }
    }
}
