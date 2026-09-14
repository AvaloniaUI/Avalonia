using System;
using System.Buffers;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Distributes the remaining line width across the break opportunities reported by the line
    /// breaker - inter-word gaps for space-delimited scripts, and inter-character/inter-syllable
    /// gaps for CJK and Korean (which the line breaker treats as break opportunities).
    /// </summary>
    /// <remarks>
    /// Known limitations:
    /// <list type="bullet">
    /// <item>Thai, Lao, Khmer and Myanmar produce no line-break opportunities without dictionary or
    /// ML based word segmentation (character-class line breaking cannot find their word boundaries),
    /// so such lines are left un-justified (start-aligned) rather than spaced incorrectly.</item>
    /// <item>Arabic and Hebrew are justified with inter-word spacing rather than the idiomatic
    /// kashida (tatweel) elongation, which would require shaper-level support.</item>
    /// </list>
    /// </remarks>
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

            CollectBreakOpportunities(lineImpl, breakOportunities);

            if (breakOportunities.Count == 0)
            {
                return;
            }

            // Fill the visible content to the paragraph width, not the width including trailing
            // whitespace. A wrapped line keeps the space at its wrap point as trailing whitespace,
            // which can push WidthIncludingTrailingWhitespace to or past the paragraph width; using
            // it here would leave remainingSpace at zero and the line unjustified. The distributed
            // space only ever lands on visible glyphs (trailing whitespace gets no break), so the
            // visible content reaches the margin and the trailing whitespace hangs past it.
            var remainingSpace = Math.Max(0, paragraphWidth - lineImpl.Width);
            var spacing = remainingSpace / breakOportunities.Count;

            var currentPosition = textLine.FirstTextSourceIndex;

            for (var runIndex = 0; runIndex < lineImpl.TextRuns.Count; runIndex++)
            {
                var textRun = lineImpl.TextRuns[runIndex];
                var runLength = textRun.Length;
                var runEnd = currentPosition + runLength;

                var shapedText = textRun.Text.IsEmpty ? null : textRun as ShapedTextRun;
                var glyphRun = shapedText?.GlyphRun;
                ShapedBuffer? writableBuffer = null;

                // Consume only the break opportunities that fall inside this run's range. The queue
                // is in ascending position order, so once the front break is at or past runEnd it
                // belongs to a later run and must be left for it, instead of draining the whole
                // queue against this run.
                while (breakOportunities.Count > 0)
                {
                    var characterIndex = breakOportunities.Peek();

                    if (characterIndex >= runEnd)
                    {
                        break;
                    }

                    breakOportunities.Dequeue();

                    // Skip stale breaks and breaks in runs we cannot justify (non-shaped).
                    if (characterIndex < currentPosition || shapedText is null)
                    {
                        continue;
                    }

                    // Copy-on-write: the run's own ShapedBuffer may share its pooled glyph storage
                    // with a TextRunCache entry, a Split sibling or a WithBidiLevel alias, so
                    // mutating it in place would corrupt those. Adjust a private clone and swap in a
                    // fresh run below.
                    writableBuffer ??= shapedText.ShapedBuffer.CloneWritable();

                    var offset = Math.Max(0, currentPosition - glyphRun!.Metrics.FirstCluster);
                    var glyphIndex = glyphRun.FindGlyphIndex(characterIndex - offset);
                    var glyphInfo = writableBuffer[glyphIndex];

                    writableBuffer[glyphIndex] = new GlyphInfo(glyphInfo.GlyphIndex,
                        glyphInfo.GlyphCluster, glyphInfo.GlyphAdvance + spacing);
                }

                if (writableBuffer != null)
                {
                    var justifiedRun = new ShapedTextRun(writableBuffer, shapedText!.Properties);

                    lineImpl.ReplaceTextRun(runIndex, justifiedRun);

                    shapedText.Dispose();
                }

                currentPosition += runLength;
            }
        }

        /// <summary>
        /// Collects the break opportunities of the line, in ascending text source position order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The opportunities are searched in the line's text rather than in each run's text: a run
        /// boundary is not a text boundary - a font fallback or a style change splits a run in the
        /// middle of a sentence - while the line breaker reports a break at the end of whatever text
        /// it is given. Searching run by run therefore invents a break at every run boundary, and
        /// filtering those artefacts out drops the real break whenever a word gap falls there.
        /// </para>
        /// <para>
        /// Trailing whitespace needs no special handling. The LineBreakEnumerator does not emit a
        /// non-required break inside trailing whitespace (LB07 forbids breaking before a space, LB06
        /// before a hard break), and the final break is excluded below - so no break opportunity ever
        /// targets a glyph in the trailing whitespace. Verified for ASCII and ideographic (U+3000)
        /// spaces by Justify_Does_Not_Space_Trailing_Whitespace.
        /// </para>
        /// </remarks>
        private static void CollectBreakOpportunities(TextLineImpl lineImpl, Queue<int> breakOportunities)
        {
            var textRuns = lineImpl.TextRuns;
            var currentPosition = lineImpl.FirstTextSourceIndex;
            var runIndex = 0;

            while (runIndex < textRuns.Count)
            {
                // Runs that carry no text (an embedded object, an end of line) interrupt the text, so
                // each stretch of text runs between them is searched on its own.
                var segmentStart = currentPosition;
                var segmentLength = 0;
                var segmentEnd = runIndex;

                while (segmentEnd < textRuns.Count)
                {
                    var textRun = textRuns[segmentEnd];

                    // A run whose length doesn't match its text can't be mapped back to text source
                    // positions by offset, so it ends the segment instead of shifting everything
                    // after it.
                    if (textRun.Text.IsEmpty || textRun.Text.Length != textRun.Length)
                    {
                        break;
                    }

                    segmentLength += textRun.Length;
                    ++segmentEnd;
                }

                if (segmentLength == 0)
                {
                    currentPosition += textRuns[runIndex].Length;
                    ++runIndex;

                    continue;
                }

                var buffer = ArrayPool<char>.Shared.Rent(segmentLength);

                try
                {
                    var offset = 0;

                    for (var i = runIndex; i < segmentEnd; i++)
                    {
                        var runText = textRuns[i].Text;

                        runText.Span.CopyTo(buffer.AsSpan(offset, runText.Length));

                        offset += runText.Length;
                    }

                    var lineBreakEnumerator = new LineBreakEnumerator(buffer.AsSpan(0, segmentLength));

                    while (lineBreakEnumerator.MoveNext(out var currentBreak))
                    {
                        if (currentBreak.Required || currentBreak.PositionWrap == segmentLength)
                        {
                            continue;
                        }

                        // The extra advance must land on the glyph that ENDS at the break boundary
                        // (the last glyph before the break), so the widened gap sits on the break
                        // itself. For whitespace breaks GetLineBreak has already pulled
                        // PositionMeasure back onto the trailing whitespace glyph
                        // (PositionMeasure < PositionWrap), so that position is the target as-is.
                        // For zero-width breaks - CJK/Korean ideograph boundaries, hyphens and other
                        // break-after punctuation - PositionMeasure == PositionWrap and points one
                        // glyph PAST the boundary; step back one so we widen the gap the break
                        // represents rather than the following gap. This also keeps the last visible
                        // glyph of a CJK/Korean line unstretched: its only inbound break is the
                        // segment-final one, already excluded above.
                        var target = segmentStart + currentBreak.PositionMeasure;

                        if (currentBreak.PositionMeasure == currentBreak.PositionWrap)
                        {
                            target -= 1;
                        }

                        breakOportunities.Enqueue(target);
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }

                currentPosition = segmentStart + segmentLength;
                runIndex = segmentEnd;
            }
        }
    }
}
