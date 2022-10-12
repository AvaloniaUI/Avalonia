using System;
using System.Globalization;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;
using GlyphInfo = HarfBuzzSharp.GlyphInfo;

namespace Avalonia.Direct2D1.Media
{
    internal class TextShaperImpl : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlySlice<char> text, TextShaperOptions options)
        {
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;
            var culture = options.Culture;

            using (var buffer = new Buffer())
            {
                buffer.AddUtf16(text.Buffer.Span, text.BufferOffset, text.Length);

                MergeBreakPair(buffer);

                buffer.GuessSegmentProperties();

                buffer.Direction = (bidiLevel & 1) == 0 ? Direction.LeftToRight : Direction.RightToLeft;

                buffer.Language = new Language(culture ?? CultureInfo.CurrentCulture);

                var font = ((GlyphTypefaceImpl)typeface).Font;

                font.Shape(buffer);

                if(buffer.Direction == Direction.RightToLeft)
                {
                    buffer.Reverse();
                }

                font.GetScale(out var scaleX, out _);

                var textScale = fontRenderingEmSize / scaleX;

                var bufferLength = buffer.Length;

                var shapedBuffer = new ShapedBuffer(text, bufferLength, typeface, fontRenderingEmSize, bidiLevel);

                var glyphInfos = buffer.GetGlyphInfoSpan();

                var glyphPositions = buffer.GetGlyphPositionSpan();

                for (var i = 0; i < bufferLength; i++)
                {
                    var sourceInfo = glyphInfos[i];

                    var glyphIndex = (ushort)sourceInfo.Codepoint;

                    var glyphCluster = (int)(sourceInfo.Cluster);

                    var glyphAdvance = GetGlyphAdvance(glyphPositions, i, textScale);

                    var glyphOffset = GetGlyphOffset(glyphPositions, i, textScale);

                    if (glyphIndex == 0 && text.Buffer.Span[glyphCluster] == '\t')
                    {
                        glyphIndex = typeface.GetGlyph(' ');

                        glyphAdvance = options.IncrementalTabWidth > 0 ?
                            options.IncrementalTabWidth :
                            4 * typeface.GetGlyphAdvance(glyphIndex) * textScale;
                    }

                    var targetInfo = new Avalonia.Media.TextFormatting.GlyphInfo(glyphIndex, glyphCluster, glyphAdvance, glyphOffset);

                    shapedBuffer[i] = targetInfo;
                }

                return shapedBuffer;
            }
        }

        private static void MergeBreakPair(Buffer buffer)
        {
            var length = buffer.Length;

            var glyphInfos = buffer.GetGlyphInfoSpan();

            var second = glyphInfos[length - 1];

            if (!new Codepoint(second.Codepoint).IsBreakChar)
            {
                return;
            }

            if (length > 1 && glyphInfos[length - 2].Codepoint == '\r' && second.Codepoint == '\n')
            {
                var first = glyphInfos[length - 2];

                first.Codepoint = '\u200C';
                second.Codepoint = '\u200C';
                second.Cluster = first.Cluster;

                unsafe
                {
                    fixed (GlyphInfo* p = &glyphInfos[length - 2])
                    {
                        *p = first;
                    }

                    fixed (GlyphInfo* p = &glyphInfos[length - 1])
                    {
                        *p = second;
                    }
                }
            }
            else
            {
                second.Codepoint = '\u200C';

                unsafe
                {
                    fixed (GlyphInfo* p = &glyphInfos[length - 1])
                    {
                        *p = second;
                    }
                }
            }
        }

        private static Vector GetGlyphOffset(ReadOnlySpan<GlyphPosition> glyphPositions, int index, double textScale)
        {
            var position = glyphPositions[index];

            var offsetX = position.XOffset * textScale;

            var offsetY = position.YOffset * textScale;

            return new Vector(offsetX, offsetY);
        }

        private static double GetGlyphAdvance(ReadOnlySpan<GlyphPosition> glyphPositions, int index, double textScale)
        {
            // Depends on direction of layout
            // glyphPositions[index].YAdvance * textScale;
            return glyphPositions[index].XAdvance * textScale;
        }
    }
}
