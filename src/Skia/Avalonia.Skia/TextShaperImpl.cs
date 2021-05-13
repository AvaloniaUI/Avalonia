using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Skia
{
    internal class TextShaperImpl : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize, CultureInfo culture)
        {
            using (var buffer = new Buffer())
            {
                FillBuffer(buffer, text);

                buffer.Language = new Language(culture ?? CultureInfo.CurrentCulture);

                buffer.GuessSegmentProperties();

                var glyphTypeface = typeface.GlyphTypeface;

                var font = ((GlyphTypefaceImpl)glyphTypeface.PlatformImpl).Font;

                font.Shape(buffer);

                font.GetScale(out var scaleX, out _);

                var textScale = fontRenderingEmSize / scaleX;

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

                    clusters[i] = (ushort)glyphInfos[i].Cluster;

                    if (!glyphTypeface.IsFixedPitch)
                    {
                        SetAdvance(glyphPositions, i, textScale, ref glyphAdvances);
                    }

                    SetOffset(glyphPositions, i, textScale, ref glyphOffsets);
                }

                return new GlyphRun(glyphTypeface, fontRenderingEmSize,
                    new ReadOnlySlice<ushort>(glyphIndices),
                    new ReadOnlySlice<double>(glyphAdvances),
                    new ReadOnlySlice<Vector>(glyphOffsets),
                    text,
                    new ReadOnlySlice<ushort>(clusters),
                    buffer.Direction == Direction.LeftToRight ? 0 : 1);
            }
        }

        private static void FillBuffer(Buffer buffer, ReadOnlySlice<char> text)
        {
            buffer.ContentType = ContentType.Unicode;

            var i = 0;

            while (i < text.Length)
            {
                var codepoint = Codepoint.ReadAt(text, i, out var count);

                var cluster = (uint)(text.Start + i);

                if (codepoint.IsBreakChar)
                {
                    if (i + 1 < text.Length)
                    {
                        var nextCodepoint = Codepoint.ReadAt(text, i + 1, out _);

                        if (nextCodepoint == '\n' && codepoint == '\r')
                        {
                            count++;

                            buffer.Add('\u200C', cluster);

                            buffer.Add('\u200D', cluster);
                        }
                        else
                        {
                            buffer.Add('\u200C', cluster);
                        }
                    }
                    else
                    {
                        buffer.Add('\u200C', cluster);
                    }
                }
                else
                {
                    buffer.Add(codepoint, cluster);
                }

                i += count;
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

            offsetBuffer ??= new Vector[glyphPositions.Length];

            var offsetX = position.XOffset * textScale;

            var offsetY = position.YOffset * textScale;

            offsetBuffer[index] = new Vector(offsetX, offsetY);
        }

        private static void SetAdvance(ReadOnlySpan<GlyphPosition> glyphPositions, int index, double textScale,
            ref double[] advanceBuffer)
        {
            advanceBuffer ??= new double[glyphPositions.Length];

            // Depends on direction of layout
            // advanceBuffer[index] = buffer.GlyphPositions[index].YAdvance * textScale;
            advanceBuffer[index] = glyphPositions[index].XAdvance * textScale;
        }
    }
}
