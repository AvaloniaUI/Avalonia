using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Direct2D1.Media
{
    internal class TextShaperImpl : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize, CultureInfo culture)
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

                var glyphTypeface = typeface.GlyphTypeface;

                var font = ((GlyphTypefaceImpl)glyphTypeface.PlatformImpl).Font;

                buffer.Language = new Language(culture ?? CultureInfo.CurrentCulture);

                font.Shape(buffer);

                font.GetScale(out var scaleX, out _);

                var textScale = fontRenderingEmSize / scaleX;

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

                    clusters[i] = (ushort)(text.Start + info[i].Cluster);

                    var advanceX = pos[i].XAdvance * textScale;
                    // Depends on direction of layout
                    //var advanceY = pos[i].YAdvance * textScale;

                    glyphAdvances[i] = advanceX;

                    var offsetX = pos[i].XOffset * textScale;
                    var offsetY = pos[i].YOffset * textScale;

                    glyphOffsets[i] = new Vector(offsetX, offsetY);
                }

                return new GlyphRun(glyphTypeface, fontRenderingEmSize,
                    new ReadOnlySlice<ushort>(glyphIndices),
                    new ReadOnlySlice<double>(glyphAdvances),
                    new ReadOnlySlice<Vector>(glyphOffsets),
                    text,
                    new ReadOnlySlice<ushort>(clusters));
            }
        }
    }
}
