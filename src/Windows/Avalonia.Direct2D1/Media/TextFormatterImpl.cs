using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Platform;
using Avalonia.Utility;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Direct2D1.Media
{
    internal class TextFormatterImpl : ITextFormatterImpl
    {
        public List<TextRunProperties> CreateTextRuns(ReadOnlySlice<char> text, TextRunStyle defaultStyle)
        {
            return TextRunIterator.Create(text, defaultStyle);
        }

        public GlyphRun CreateShapedGlyphRun(ReadOnlySlice<char> text, TextFormat textFormat)
        {
            using (var buffer = new Buffer())
            {
                buffer.ContentType = ContentType.Unicode;

                var breakCharPosition = text.Length - 1;

                if (UnicodeUtility.IsBreakChar(text[breakCharPosition]))
                {
                    var breakCharCount = 1;

                    if (text.Length > 1)
                    {
                        if (text[breakCharPosition] == '\r' && text[breakCharPosition - 1] == '\n'
                            || text[breakCharPosition] == '\n' && text[breakCharPosition - 1] == '\r')
                        {
                            breakCharCount = 2;
                        }
                    }

                    if (breakCharPosition != text.Start)
                    {
                        buffer.AddUtf16(text.Buffer.Span, text.Start, text.Length - breakCharCount);
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
                    buffer.AddUtf16(text.Buffer.Span, text.Start, text.Length);
                }

                buffer.GuessSegmentProperties();

                var glyphTypeface = textFormat.Typeface.GlyphTypeface;

                var font = ((GlyphTypefaceImpl)glyphTypeface.PlatformImpl).Font;

                font.Shape(buffer);

                font.GetScale(out var scaleX, out _);

                var textScale = textFormat.FontRenderingEmSize / scaleX;

                var len = buffer.Length;

                var info = buffer.GetGlyphInfoSpan();

                var pos = buffer.GetGlyphPositionSpan();

                var glyphIndices = new ushort[len];

                var clusters = new ushort[len];

                var glyphAdvances = new double[len];

                var glyphOffsets = new Vector[len];

                for (var i = 0; i < len; i++)
                {
                    glyphIndices[i] = (ushort)info[i].Codepoint;

                    clusters[i] = (ushort)info[i].Cluster;

                    var advanceX = pos[i].XAdvance * textScale;
                    // Depends on direction of layout
                    //var advanceY = pos[i].YAdvance * textScale;

                    glyphAdvances[i] = advanceX;

                    var offsetX = pos[i].XOffset * textScale;
                    var offsetY = pos[i].YOffset * textScale;

                    glyphOffsets[i] = new Vector(offsetX, offsetY);
                }

                return new GlyphRun(glyphTypeface, textFormat.FontRenderingEmSize,
                    new ReadOnlySlice<ushort>(glyphIndices),
                    new ReadOnlySlice<double>(glyphAdvances),
                    new ReadOnlySlice<Vector>(glyphOffsets),
                    text,
                    new ReadOnlySlice<ushort>(clusters));
            }
        }
    }
}
