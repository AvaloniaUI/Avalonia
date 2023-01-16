﻿using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;
using GlyphInfo = HarfBuzzSharp.GlyphInfo;

namespace Avalonia.Direct2D1.Media
{
    internal class TextShaperImpl : ITextShaperImpl
    {
        public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options)
        {
            var textSpan = text.Span;
            var typeface = options.Typeface;
            var fontRenderingEmSize = options.FontRenderingEmSize;
            var bidiLevel = options.BidiLevel;
            var culture = options.Culture;

            using (var buffer = new Buffer())
            {
                // HarfBuzz needs the surrounding characters to correctly shape the text
                var containingText = GetContainingMemory(text, out var start, out var length);
                buffer.AddUtf16(containingText.Span, start, length);

                MergeBreakPair(buffer);

                buffer.GuessSegmentProperties();

                buffer.Direction = (bidiLevel & 1) == 0 ? Direction.LeftToRight : Direction.RightToLeft;

                buffer.Language = new Language(culture ?? CultureInfo.CurrentCulture);

                var font = ((GlyphTypefaceImpl)typeface).Font;

                font.Shape(buffer);

                if (buffer.Direction == Direction.RightToLeft)
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

                    var glyphAdvance = GetGlyphAdvance(glyphPositions, i, textScale) + options.LetterSpacing;

                    var glyphOffset = GetGlyphOffset(glyphPositions, i, textScale);

                    if (textSpan[i] == '\t')
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

        private static ReadOnlyMemory<char> GetContainingMemory(ReadOnlyMemory<char> memory, out int start, out int length)
        {
            if (MemoryMarshal.TryGetString(memory, out var containingString, out start, out length))
            {
                return containingString.AsMemory();
            }

            if (MemoryMarshal.TryGetArray(memory, out var segment))
            {
                return segment.Array.AsMemory();
            }

            if (MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<char> memoryManager, out start, out length))
            {
                return memoryManager.Memory;
            }

            // should never happen
            throw new InvalidOperationException("Memory not backed by string, array or manager");
        }
    }
}
