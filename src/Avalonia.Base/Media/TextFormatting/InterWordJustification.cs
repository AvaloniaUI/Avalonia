using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal class InterWordJustification : JustificationProperties
    {
        public InterWordJustification(double width)
        {
            Width = width;
        }

        public override double Width { get; }

        public override void Justify(TextLine textLine)
        {
            var paragraphWidth = Width;

            if (double.IsInfinity(paragraphWidth))
            {
                return;
            }

            if (textLine.NewLineLength > 0)
            {
                return;
            }

            var textLineBreak = textLine.TextLineBreak;

            if (textLineBreak is not null && textLineBreak.TextEndOfLine is not null)
            {
                if (textLineBreak.RemainingRuns is null || textLineBreak.RemainingRuns.Count == 0)
                {
                    return;
                }
            }

            var breakOportunities = new Queue<int>();

            foreach (var textRun in textLine.TextRuns)
            {
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                var start = text.Start;

                var lineBreakEnumerator = new LineBreakEnumerator(text);

                while (lineBreakEnumerator.MoveNext())
                {
                    var currentBreak = lineBreakEnumerator.Current;

                    if (!currentBreak.Required && currentBreak.PositionWrap != text.Length)
                    {
                        breakOportunities.Enqueue(start + currentBreak.PositionMeasure);
                    }
                }
            }

            if (breakOportunities.Count == 0)
            {
                return;
            }

            var remainingSpace = Math.Max(0, paragraphWidth - textLine.WidthIncludingTrailingWhitespace);
            var spacing = remainingSpace / breakOportunities.Count;

            foreach (var textRun in textLine.TextRuns)
            {
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                if (textRun is ShapedTextCharacters shapedText)
                {
                    var glyphRun = shapedText.GlyphRun;
                    var shapedBuffer = shapedText.ShapedBuffer;
                    var currentPosition = text.Start;

                    while (breakOportunities.Count > 0)
                    {
                        var characterIndex = breakOportunities.Dequeue();

                        if (characterIndex < currentPosition)
                        {
                            continue;
                        }

                        var glyphIndex = glyphRun.FindGlyphIndex(characterIndex);
                        var glyphInfo = shapedBuffer.GlyphInfos[glyphIndex];

                        shapedBuffer.GlyphInfos[glyphIndex] = new GlyphInfo(glyphInfo.GlyphIndex, glyphInfo.GlyphCluster, glyphInfo.GlyphAdvance + spacing);
                    }

                    glyphRun.GlyphAdvances = shapedBuffer.GlyphAdvances;
                }
            }
        }
    }
}
