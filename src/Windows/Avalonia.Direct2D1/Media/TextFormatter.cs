using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Media.Text.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Direct2D1.Media
{
    internal class TextFormatter : ITextFormatter
    {
        public List<TextRunProperties> CreateTextRuns(ReadOnlySlice<char> text, Typeface defaultTypeface, double defaultFontSize)
        {
            return TextRunIterator.Create(text, defaultTypeface, defaultFontSize);
        }

        public List<TextRun> FormatTextRuns(ReadOnlySlice<char> text, List<TextRunProperties> textRunProperties)
        {
            var currentLength = 0;

            var textRuns = new List<TextRun>();

            while (currentLength < text.Length && textRunProperties.Count > 0)
            {
                var run = textRunProperties[0];

                var textLength = run.Text.Length;

                if (currentLength + textLength > text.Length)
                {
                    var split = run.Split(text.Length - currentLength);

                    run = split.First;

                    textRunProperties.RemoveAt(0);

                    textRunProperties.Insert(0, split.Second);

                    textRunProperties.Insert(0, run);
                }

                var runCount = 1;

                if (textLength < text.Length)
                {
                    for (; runCount < textRunProperties.Count; runCount++)
                    {
                        var props = textRunProperties[runCount];

                        if (props.TextFormat != run.TextFormat)
                        {
                            break;
                        }

                        if (textLength + props.Text.Length > text.Length)
                        {
                            var split = props.Split(text.Length - textLength);

                            textRunProperties.RemoveAt(runCount);

                            textRunProperties.Insert(runCount, split.Second);

                            textRunProperties.Insert(runCount, split.First);

                            textLength = text.Length;

                            runCount++;

                            break;
                        }

                        textLength += props.Text.Length;
                    }
                }

                if (runCount > 1)
                {
                    var runText = new ReadOnlySlice<char>(run.Text.Buffer, run.Text.Start, textLength);

                    var glyphRun = CreateShapedGlyphRun(runText, run.TextFormat);

                    while (runCount > 0)
                    {
                        run = textRunProperties[0];

                        var splitGlyphRun = SplitGlyphRun(runText, ref glyphRun, run.Text.Length);

                        var textRun = new TextRun(splitGlyphRun, run.TextFormat, run.Foreground);

                        textRuns.Add(textRun);

                        textRunProperties.RemoveAt(0);

                        runCount--;
                    }
                }
                else
                {
                    var glyphRun = CreateShapedGlyphRun(run.Text, run.TextFormat);

                    var textRun = new TextRun(glyphRun, run.TextFormat, run.Foreground);

                    textRuns.Add(textRun);

                    textRunProperties.RemoveAt(0);
                }

                currentLength += textLength;
            }

            return textRuns;
        }

        private static GlyphRun SplitGlyphRun(ReadOnlySlice<char> text, ref GlyphRun glyphRun, int textLength)
        {
            var glyphCount = 0;

            for (var i = 0; i < textLength;)
            {
                CodepointReader.Read(text, ref i);

                glyphCount++;
            }

            if (glyphRun.GlyphIndices.Length == glyphCount)
            {
                return glyphRun;
            }

            var result = new GlyphRun(glyphRun.GlyphTypeface, glyphRun.FontRenderingEmSize,
                glyphRun.GlyphIndices.Take(glyphCount),
                glyphRun.GlyphAdvances.Take(glyphCount),
                glyphRun.GlyphOffsets.Take(glyphCount),
                glyphRun.Characters.Take(textLength),
                glyphRun.GlyphClusters.Take(textLength),
                glyphRun.BidiLevel);

            glyphRun = new GlyphRun(glyphRun.GlyphTypeface, glyphRun.FontRenderingEmSize,
                glyphRun.GlyphIndices.Skip(glyphCount),
                glyphRun.GlyphAdvances.Skip(glyphCount),
                glyphRun.GlyphOffsets.Skip(glyphCount),
                glyphRun.Characters.Skip(textLength),
                glyphRun.GlyphClusters.Skip(textLength),
                glyphRun.BidiLevel);

            return result;
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

                var textScale = textFormat.FontSize / scaleX;

                var len = buffer.Length;

                var info = buffer.GetGlyphInfoSpan();

                var pos = buffer.GetGlyphPositionSpan();

                var glyphIndices = new ushort[len];

                var clusters = new ushort[text.Length];

                var glyphAdvances = new double[len];

                var glyphOffsets = new Vector[len];

                var currentClusterIndex = 0;

                for (var i = 0; i < len; i++)
                {
                    glyphIndices[i] = (ushort)info[i].Codepoint;

                    var currentCluster = (ushort)info[i].Cluster;

                    clusters[currentClusterIndex] = currentCluster;

                    CodepointReader.Peek(text, currentClusterIndex, out var count);

                    currentClusterIndex++;

                    if (count > 1)
                    {
                        clusters[currentClusterIndex++] = currentCluster;
                    }

                    var advanceX = pos[i].XAdvance * textScale;
                    // Depends on direction of layout
                    //var advanceY = pos[i].YAdvance * textScale;

                    glyphAdvances[i] = advanceX;

                    var offsetX = pos[i].XOffset * textScale;
                    var offsetY = pos[i].YOffset * textScale;

                    glyphOffsets[i] = new Vector(offsetX, offsetY);
                }

                return new GlyphRun(glyphTypeface, textFormat.FontSize,
                    new ReadOnlySlice<ushort>(glyphIndices),
                    new ReadOnlySlice<double>(glyphAdvances),
                    new ReadOnlySlice<Vector>(glyphOffsets),
                    text,
                    new ReadOnlySlice<ushort>(clusters));
            }
        }
    }
}
