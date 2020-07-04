using System;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Skia
{
    internal class TextShaperImpl : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, TextFormat textFormat)
        {
            using (var buffer = new Buffer())
            {
                buffer.ContentType = ContentType.Unicode;

                var breakCharPosition = text.Length - 1;

                var codepoint = Codepoint.ReadAt(text, breakCharPosition, out var count);

                if (codepoint.IsBreakChar)
                {
                    var breakCharCount = 1;

                    if (text.Length > 1)
                    {
                        var previousCodepoint = Codepoint.ReadAt(text, breakCharPosition - count, out _);

                        if (codepoint == '\r' && previousCodepoint == '\n'
                            || codepoint == '\n' && previousCodepoint == '\r')
                        {
                            breakCharCount = 2;
                        }
                    }

                    if (breakCharPosition != text.Start)
                    {
                        buffer.AddUtf16(text.Buffer.Span.Slice(0, text.Length - breakCharCount));
                    }

                    var cluster = buffer.GlyphInfos.Length > 0 ?
                        buffer.GlyphInfos[buffer.Length - 1].Cluster + 1 :
                        (uint)text.Start;

                    switch (breakCharCount)
                    {
                        case 1:
                            buffer.Add('\u200C', cluster);
                            break;
                        case 2:
                            buffer.Add('\u200C', cluster);
                            buffer.Add('\u200D', cluster);
                            break;
                    }
                }
                else
                {
                    buffer.AddUtf16(text.Buffer.Span);
                }

                buffer.GuessSegmentProperties();

                var glyphTypeface = textFormat.Typeface.GlyphTypeface;

                var font = ((GlyphTypefaceImpl)glyphTypeface.PlatformImpl).Font;

                font.Shape(buffer);

                font.GetScale(out var scaleX, out _);

                var textScale = textFormat.FontRenderingEmSize / scaleX;

                var bufferLength = buffer.Length;

                var glyphInfos = buffer.GetGlyphInfoSpan();

                var glyphPositions = buffer.GetGlyphPositionSpan();

                var glyphIndices = new ushort[bufferLength];

                var clusters = new ushort[bufferLength];

                double[] glyphAdvances = null;

                Vector[] glyphOffsets = null;

                for (var i = 0; i < bufferLength; i++)
                {
                    glyphIndices[i] = (ushort)glyphInfos[i].Codepoint;

                    clusters[i] = (ushort)(text.Start + glyphInfos[i].Cluster);

                    if (!glyphTypeface.IsFixedPitch)
                    {
                        SetAdvance(glyphPositions, i, textScale, ref glyphAdvances);
                    }

                    SetOffset(glyphPositions, i, textScale, ref glyphOffsets);
                }

                return new GlyphRun(glyphTypeface, textFormat.FontRenderingEmSize,
                    new ReadOnlySlice<ushort>(glyphIndices),
                    new ReadOnlySlice<double>(glyphAdvances),
                    new ReadOnlySlice<Vector>(glyphOffsets),
                    text,
                    new ReadOnlySlice<ushort>(clusters));
            }
        }

        private static void SetOffset(ReadOnlySpan<GlyphPosition> glyphPositions, int index, double textScale,
            ref Vector[] offsetBuffer)
        {
            var position = glyphPositions[index];

            if (position.XOffset == 0 && position.YOffset == 0)
            {
                return;
            }

            if (offsetBuffer == null)
            {
                offsetBuffer = new Vector[glyphPositions.Length];
            }

            var offsetX = position.XOffset * textScale;

            var offsetY = position.YOffset * textScale;

            offsetBuffer[index] = new Vector(offsetX, offsetY);
        }

        private static void SetAdvance(ReadOnlySpan<GlyphPosition> glyphPositions, int index, double textScale,
            ref double[] advanceBuffer)
        {
            if (advanceBuffer == null)
            {
                advanceBuffer = new double[glyphPositions.Length];
            }

            // Depends on direction of layout
            // advanceBuffer[index] = buffer.GlyphPositions[index].YAdvance * textScale;
            advanceBuffer[index] = glyphPositions[index].XAdvance * textScale;
        }
    }
}
