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
            var lineImpl = textLine as TextLineImpl;

            if(lineImpl is null)
            {
                return;
            }

            var paragraphWidth = Width;

            if (double.IsInfinity(paragraphWidth))
            {
                return;
            }

            if (lineImpl.NewLineLength > 0)
            {
                return;
            }

            var textLineBreak = lineImpl.TextLineBreak;

            if (textLineBreak is not null && textLineBreak.TextEndOfLine is not null)
            {
                if (textLineBreak.RemainingRuns is null || textLineBreak.RemainingRuns.Count == 0)
                {
                    return;
                }
            }

            var breakOportunities = new Queue<int>();

            var currentPosition = textLine.FirstTextSourceIndex;

            foreach (var textRun in lineImpl.TextRuns)
            {
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                var lineBreakEnumerator = new LineBreakEnumerator(text.Span);

                while (lineBreakEnumerator.MoveNext())
                {
                    var currentBreak = lineBreakEnumerator.Current;

                    if (!currentBreak.Required && currentBreak.PositionWrap != textRun.Length)
                    {
                        breakOportunities.Enqueue(currentPosition + currentBreak.PositionMeasure);
                    }
                }

                currentPosition += textRun.Length;
            }

            if (breakOportunities.Count == 0)
            {
                return;
            }

            var remainingSpace = Math.Max(0, paragraphWidth - lineImpl.WidthIncludingTrailingWhitespace);
            var spacing = remainingSpace / breakOportunities.Count;

            currentPosition = textLine.FirstTextSourceIndex;

            foreach (var textRun in lineImpl.TextRuns)
            {
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                if (textRun is ShapedTextRun shapedText)
                {
                    var glyphRun = shapedText.GlyphRun;
                    var shapedBuffer = shapedText.ShapedBuffer;

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

                currentPosition += textRun.Length;
            }
        }
    }
}
