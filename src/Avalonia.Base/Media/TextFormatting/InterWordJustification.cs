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
            if (textLine is not TextLineImpl lineImpl)
            {
                return;
            }

            var paragraphWidth = Width;

            if (double.IsInfinity(paragraphWidth))
            {
                return;
            }

            var breakOportunities = new Queue<int>();

            var currentPosition = textLine.FirstTextSourceIndex;

            for (var i = 0; i < lineImpl.TextRuns.Count; ++i)
            {
                var textRun = lineImpl.TextRuns[i];
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                var lineBreakEnumerator = new LineBreakEnumerator(text.Span);

                while (lineBreakEnumerator.MoveNext(out var currentBreak))
                {
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
            var remainingSpace = 0d;
            var spacing = 0d;
            if (textLine.TextLineBreak is not null &&
                textLine.TextLineBreak?.TextEndOfLine is not TextEndOfParagraph)
            {
                remainingSpace = Math.Max(0, paragraphWidth - lineImpl.WidthIncludingTrailingWhitespaceWithoutSpacing);
                spacing = remainingSpace / breakOportunities.Count;
            }

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

                    if (!shapedText.HasShapedBufferWithoutSpacing)
                        shapedText.CreateShapedBufferWithoutSpacing();

                    var shapedBufferWithoutSpacing = shapedText.ShapedBufferWithoutSpacing;

                    while (breakOportunities.Count > 0)
                    {
                        var characterIndex = breakOportunities.Dequeue();

                        if (characterIndex < currentPosition)
                        {
                            continue;
                        }

                        var offset = Math.Max(0, currentPosition - glyphRun.Metrics.FirstCluster);
                        var glyphIndex = glyphRun.FindGlyphIndex(characterIndex - offset);
                        var glyphInfo = shapedBuffer[glyphIndex];
                        var originalGlyphInfo = shapedBufferWithoutSpacing[glyphIndex];

                        if (spacing > 0 ||
                            originalGlyphInfo.GlyphAdvance != glyphInfo.GlyphAdvance)
                        {
                            shapedBuffer[glyphIndex] = new GlyphInfo(glyphInfo.GlyphIndex,
                            glyphInfo.GlyphCluster, originalGlyphInfo.GlyphAdvance + spacing);
                        }
                    }

                    glyphRun.GlyphInfos = shapedBuffer;
                }

                currentPosition += textRun.Length;
            }
        }
    }
}
