using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        private static readonly Comparer<int> s_compareStart = Comparer<int>.Default;

        private static readonly Comparison<ShapedTextCharacters> s_compareLogicalOrder =
            (a, b) => s_compareStart.Compare(a.Text.Start, b.Text.Start);

        private readonly List<ShapedTextCharacters> _textRuns;
        private readonly double _paragraphWidth;
        private readonly TextParagraphProperties _paragraphProperties;
        private TextLineMetrics _textLineMetrics;
        private readonly FlowDirection _flowDirection;

        public TextLineImpl(List<ShapedTextCharacters> textRuns, TextRange textRange, double paragraphWidth,
            TextParagraphProperties paragraphProperties, FlowDirection flowDirection = FlowDirection.LeftToRight, 
            TextLineBreak? lineBreak = null, bool hasCollapsed = false)
        {
            TextRange = textRange;
            TextLineBreak = lineBreak;
            HasCollapsed = hasCollapsed;

            _textRuns =  textRuns;
            _paragraphWidth = paragraphWidth;
            _paragraphProperties = paragraphProperties;

            _flowDirection = flowDirection;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <inheritdoc/>
        public override TextRange TextRange { get; }

        /// <inheritdoc/>
        public override TextLineBreak? TextLineBreak { get; }

        /// <inheritdoc/>
        public override bool HasCollapsed { get; }

        /// <inheritdoc/>
        public override bool HasOverflowed => _textLineMetrics.HasOverflowed;

        /// <inheritdoc/>
        public override double Baseline => _textLineMetrics.TextBaseline;

        /// <inheritdoc/>
        public override double Extent => _textLineMetrics.Height;

        /// <inheritdoc/>
        public override double Height => _textLineMetrics.Height;

        /// <inheritdoc/>
        public override int NewLineLength => _textLineMetrics.NewLineLength;

        /// <inheritdoc/>
        public override double OverhangAfter => 0;

        /// <inheritdoc/>
        public override double OverhangLeading => 0;

        /// <inheritdoc/>
        public override double OverhangTrailing => 0;

        /// <inheritdoc/>
        public override int TrailingWhitespaceLength => _textLineMetrics.TrailingWhitespaceLength;

        /// <inheritdoc/>
        public override double Start => _textLineMetrics.Start;

        /// <inheritdoc/>
        public override double Width => _textLineMetrics.Width;

        /// <inheritdoc/>
        public override double WidthIncludingTrailingWhitespace => _textLineMetrics.WidthIncludingTrailingWhitespace;

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point lineOrigin)
        {
            var (currentX, currentY) = lineOrigin;

            foreach (var textRun in _textRuns)
            {
                var offsetY = Baseline - textRun.GlyphRun.BaselineOrigin.Y;

                textRun.Draw(drawingContext, new Point(currentX, currentY + offsetY));

                currentX += textRun.Size.Width;
            }
        }

        /// <inheritdoc/>
        public override TextLine Collapse(params TextCollapsingProperties[] collapsingPropertiesList)
        {
            if (collapsingPropertiesList.Length == 0)
            {
                return this;
            }

            var collapsingProperties = collapsingPropertiesList[0];

            var runIndex = 0;
            var currentWidth = 0.0;
            var textRange = TextRange;
            var collapsedLength = 0;

            var shapedSymbol = TextFormatterImpl.CreateSymbol(collapsingProperties.Symbol, _paragraphProperties.FlowDirection);
            
            if (collapsingProperties.Width < shapedSymbol.GlyphRun.Size.Width)
            {
                return new TextLineImpl(new List<ShapedTextCharacters>(0), textRange, _paragraphWidth, _paragraphProperties,
                    _flowDirection, TextLineBreak, true);
            }
            
            var availableWidth = collapsingProperties.Width - shapedSymbol.GlyphRun.Size.Width;

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                currentWidth += currentRun.Size.Width;

                if (currentWidth > availableWidth)
                {
                    if (currentRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                    {
                        if (collapsingProperties.Style == TextCollapsingStyle.TrailingWord &&
                            measuredLength < textRange.End)
                        {
                            var currentBreakPosition = 0;

                            var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                            while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                            {
                                var nextBreakPosition = lineBreaker.Current.PositionMeasure;

                                if (nextBreakPosition == 0)
                                {
                                    break;
                                }

                                if (nextBreakPosition >= measuredLength)
                                {
                                    break;
                                }

                                currentBreakPosition = nextBreakPosition;
                            }

                            measuredLength = currentBreakPosition;
                        }
                    }

                    collapsedLength += measuredLength;

                    var shapedTextCharacters = new List<ShapedTextCharacters>(_textRuns.Count);
                    
                    if (collapsedLength > 0)
                    {
                        var splitResult = TextFormatterImpl.SplitShapedRuns(_textRuns, collapsedLength);
                    
                        shapedTextCharacters.AddRange(splitResult.First);

                        SortRuns(shapedTextCharacters);
                    }

                    shapedTextCharacters.Add(shapedSymbol);

                    var textLine = new TextLineImpl(shapedTextCharacters, textRange, _paragraphWidth, _paragraphProperties,
                        _flowDirection, TextLineBreak, true);

                    return textLine.FinalizeLine();
                }

                availableWidth -= currentRun.Size.Width;

                collapsedLength += currentRun.GlyphRun.Characters.Length;

                runIndex++;
            }

            return this;
        }

        /// <inheritdoc/>
        public override CharacterHit GetCharacterHitFromDistance(double distance)
        {
            distance -= Start;
            
            if (distance <= 0)
            {
                // hit happens before the line, return the first position
                var firstRun = _textRuns[0];

                return firstRun.GlyphRun.GetCharacterHitFromDistance(distance, out _);
            }

            // process hit that happens within the line
            var characterHit = new CharacterHit();

            foreach (var run in _textRuns)
            {
                characterHit = run.GlyphRun.GetCharacterHitFromDistance(distance, out _);

                if (distance <= run.Size.Width)
                {
                    break;
                }

                distance -= run.Size.Width;
            }

            return characterHit;
        }

        /// <inheritdoc/>
        public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var characterIndex = characterHit.FirstCharacterIndex + (characterHit.TrailingLength != 0 ? 1 : 0);

            var currentDistance = Start;

            GlyphRun? lastRun = null;

            for (var index = 0; index < _textRuns.Count; index++)
            {
                var textRun = _textRuns[index];
                var currentRun = textRun.GlyphRun;

                if (lastRun != null)
                {
                    if (!lastRun.IsLeftToRight && currentRun.IsLeftToRight &&
                        currentRun.Characters.Start == characterHit.FirstCharacterIndex &&
                        characterHit.TrailingLength == 0)
                    {
                        return currentDistance;
                    }
                }

                //Look for a hit in within the current run
                if (characterIndex >= textRun.Text.Start && characterIndex <= textRun.Text.End)
                {
                    var distance = currentRun.GetDistanceFromCharacterHit(characterHit);

                    return currentDistance + distance;
                }

                //Look at the left and right edge of the current run
                if (currentRun.IsLeftToRight)
                {
                    if (lastRun == null || lastRun.IsLeftToRight)
                    {
                        if (characterIndex <= textRun.Text.Start)
                        {
                            return currentDistance;
                        }
                    }
                    else
                    {
                        if (characterIndex == textRun.Text.Start)
                        {
                            return currentDistance;
                        }
                    }

                    if (characterIndex == textRun.Text.Start + textRun.Text.Length && characterHit.TrailingLength > 0)
                    {
                        return currentDistance + currentRun.Size.Width;
                    }
                }
                else
                {
                    if (characterIndex == textRun.Text.Start)
                    {
                        return currentDistance + currentRun.Size.Width;
                    }

                    var nextRun = index + 1 < _textRuns.Count ? _textRuns[index + 1] : null;

                    if (nextRun != null)
                    {
                        if (characterHit.FirstCharacterIndex == textRun.Text.End && nextRun.ShapedBuffer.IsLeftToRight)
                        {
                            return currentDistance;
                        }

                        if (characterIndex > textRun.Text.End && nextRun.Text.End < textRun.Text.End)
                        {
                            return currentDistance;
                        }
                    }
                    else
                    {
                        if (characterIndex > textRun.Text.End)
                        {
                            return currentDistance;
                        }
                    }
                }

                //No hit hit found so we add the full width
                currentDistance += currentRun.Size.Width;

                lastRun = currentRun;
            }

            return currentDistance;
        }

        /// <inheritdoc/>
        public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindNextCharacterHit(characterHit, out var nextCharacterHit))
            {
                return nextCharacterHit;
            }

            // Can't move, we're after the last character
            var runIndex = GetRunIndexAtCharacterIndex(TextRange.End, LogicalDirection.Forward);

            var textRun = _textRuns[runIndex];

            characterHit = textRun.GlyphRun.GetNextCaretCharacterHit(characterHit);

            return characterHit; 
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindPreviousCharacterHit(characterHit, out var previousCharacterHit))
            {
                return previousCharacterHit;
            }

            if (characterHit.FirstCharacterIndex <= TextRange.Start)
            {
                characterHit = new CharacterHit(TextRange.Start);
            }

            return characterHit; // Can't move, we're before the first character
        }

        /// <inheritdoc/>
        public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            // same operation as move-to-previous
            return GetPreviousCaretCharacterHit(characterHit);
        }

        public static void SortRuns(List<ShapedTextCharacters> textRuns)
        {
            textRuns.Sort(s_compareLogicalOrder);
        }

        public TextLineImpl FinalizeLine()
        {
            BidiReorder();

            _textLineMetrics = CreateLineMetrics();

            return this;
        }

        private void BidiReorder()
        {
            // Build up the collection of ordered runs.
            var run = _textRuns[0];
            OrderedBidiRun orderedRun = new(run);
            var current = orderedRun;

            for (var i = 1; i < _textRuns.Count; i++)
            {
                run = _textRuns[i];

                current.Next = new OrderedBidiRun(run);

                current = current.Next;
            }

            // Reorder them into visual order.
            orderedRun = LinearReOrder(orderedRun);

            // Now perform a recursive reversal of each run.
            // From the highest level found in the text to the lowest odd level on each line, including intermediate levels
            // not actually present in the text, reverse any contiguous sequence of characters that are at that level or higher.
            // https://unicode.org/reports/tr9/#L2
            sbyte max = 0;
            var min = sbyte.MaxValue;

            for (var i = 0; i < _textRuns.Count; i++)
            {
                var level = _textRuns[i].BidiLevel;

                if (level > max)
                {
                    max = level;
                }

                if ((level & 1) != 0 && level < min)
                {
                    min = level;
                }
            }

            if (min > max)
            {
                min = max;
            }

            if (max == 0 || (min == max && (max & 1) == 0))
            {
                // Nothing to reverse.
                return;
            }

            // Now apply the reversal and replace the original contents.
            var minLevelToReverse = max;

            while (minLevelToReverse >= min)
            {
                current = orderedRun;

                while (current != null)
                {
                    if (current.Level >= minLevelToReverse && current.Level % 2 != 0)
                    {
                        if (!current.Run.IsReversed)
                        {
                            current.Run.Reverse();
                        }
                    }

                    current = current.Next;
                }

                minLevelToReverse--;
            }

            _textRuns.Clear();

            current = orderedRun;

            while (current != null)
            {
                _textRuns.Add(current.Run);

                current = current.Next;
            }
        }

        /// <summary>
        /// Reorders a series of runs from logical to visual order, returning the left most run.
        /// <see href="https://github.com/fribidi/linear-reorder/blob/f2f872257d4d8b8e137fcf831f254d6d4db79d3c/linear-reorder.c"/>
        /// </summary>
        /// <param name="run">The ordered bidi run.</param>
        /// <returns>The <see cref="OrderedBidiRun"/>.</returns>
        private static OrderedBidiRun LinearReOrder(OrderedBidiRun? run)
        {
            BidiRange? range = null;

            while (run != null)
            {
                var next = run.Next;

                while (range != null && range.Level > run.Level
                    && range.Previous != null && range.Previous.Level >= run.Level)
                {
                    range = BidiRange.MergeWithPrevious(range);
                }

                if (range != null && range.Level >= run.Level)
                {
                    // Attach run to the range.
                    if ((run.Level & 1) != 0)
                    {
                        // Odd, range goes to the right of run.
                        run.Next = range.Left;
                        range.Left = run;
                    }
                    else
                    {
                        // Even, range goes to the left of run.
                        range.Right!.Next = run;
                        range.Right = run;
                    }

                    range.Level = run.Level;
                }
                else
                {
                    var r = new BidiRange();

                    r.Left = r.Right = run;
                    r.Level = run.Level;
                    r.Previous = range;

                    range = r;
                }

                run = next;
            }

            while (range?.Previous != null)
            {
                range = BidiRange.MergeWithPrevious(range);
            }

            // Terminate.
            range!.Right!.Next = null;

            return range.Left!;
        }

        /// <summary>
        /// Tries to find the next character hit.
        /// </summary>
        /// <param name="characterHit">The current character hit.</param>
        /// <param name="nextCharacterHit">The next character hit.</param>
        /// <returns></returns>
        private bool TryFindNextCharacterHit(CharacterHit characterHit, out CharacterHit nextCharacterHit)
        {
            nextCharacterHit = characterHit;

            var codepointIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (codepointIndex >= TextRange.End)
            {
                return false; // Cannot go forward anymore
            }

            if (codepointIndex < TextRange.Start)
            {
                codepointIndex = TextRange.Start;
            }

            var runIndex = GetRunIndexAtCharacterIndex(codepointIndex, LogicalDirection.Forward);

            while (runIndex < _textRuns.Count)
            {
                var run = _textRuns[runIndex];

                var foundCharacterHit =
                    run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength,
                        out _);

                var isAtEnd = foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength ==
                              TextRange.Start + TextRange.Length;

                if (isAtEnd && !run.GlyphRun.IsLeftToRight)
                {
                    nextCharacterHit = foundCharacterHit;

                    return true;
                }

                var characterIndex = codepointIndex - run.Text.Start;

                if (characterIndex < 0 && run.ShapedBuffer.IsLeftToRight)
                {
                    foundCharacterHit = new CharacterHit(foundCharacterHit.FirstCharacterIndex);
                }

                nextCharacterHit = isAtEnd || characterHit.TrailingLength != 0 ?
                    foundCharacterHit :
                    new CharacterHit(foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength);

                if (isAtEnd || nextCharacterHit.FirstCharacterIndex > characterHit.FirstCharacterIndex)
                {
                    return true;
                }

                runIndex++;
            }

            return false;
        }

        /// <summary>
        /// Tries to find the previous character hit.
        /// </summary>
        /// <param name="characterHit">The current character hit.</param>
        /// <param name="previousCharacterHit">The previous character hit.</param>
        /// <returns></returns>
        private bool TryFindPreviousCharacterHit(CharacterHit characterHit, out CharacterHit previousCharacterHit)
        {
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (characterIndex == TextRange.Start)
            {
                previousCharacterHit = new CharacterHit(TextRange.Start);

                return true;
            }

            previousCharacterHit = characterHit;

            if (characterIndex < TextRange.Start)
            {
                return false; // Cannot go backward anymore.
            }

            var runIndex = GetRunIndexAtCharacterIndex(characterIndex, LogicalDirection.Backward);

            while (runIndex >= 0)
            {
                var run = _textRuns[runIndex];

                var foundCharacterHit =
                    run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

                if (foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength < characterIndex)
                {
                    previousCharacterHit = foundCharacterHit;
                    
                    return true;
                }
                
                previousCharacterHit = characterHit.TrailingLength != 0 ?
                    foundCharacterHit :
                    new CharacterHit(foundCharacterHit.FirstCharacterIndex);

                if (previousCharacterHit != characterHit)
                {
                    return true;
                }

                runIndex--;
            }

            return false;
        }

        /// <summary>
        /// Gets the run index of the specified codepoint index.
        /// </summary>
        /// <param name="codepointIndex">The codepoint index.</param>
        /// <param name="direction">The logical direction.</param>
        /// <returns>The text run index.</returns>
        private int GetRunIndexAtCharacterIndex(int codepointIndex, LogicalDirection direction)
        {
            var runIndex = 0;
            ShapedTextCharacters? previousRun = null;

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                if (previousRun != null && !previousRun.ShapedBuffer.IsLeftToRight)
                {
                    if (currentRun.ShapedBuffer.IsLeftToRight)
                    {
                        if (currentRun.Text.Start >= codepointIndex)
                        {
                            return --runIndex;
                        }
                    }
                    else
                    {
                        if (codepointIndex > currentRun.Text.Start + currentRun.Text.Length)
                        {
                            return --runIndex; 
                        }
                    }
                }

                if (direction == LogicalDirection.Forward)
                {
                    if (codepointIndex >= currentRun.Text.Start && codepointIndex <= currentRun.Text.End)
                    {
                        return runIndex;
                    }
                }
                else
                {
                    if (codepointIndex > currentRun.Text.Start &&
                        codepointIndex <= currentRun.Text.Start + currentRun.Text.Length)
                    {
                        return runIndex;
                    }
                }

                if (runIndex + 1 < _textRuns.Count)
                {
                    runIndex++;
                    previousRun = currentRun;
                }
                else
                {
                    break;
                }
            }

            return runIndex;
        }

        private TextLineMetrics CreateLineMetrics()
        {
            var width = 0d;
            var widthIncludingWhitespace = 0d;
            var trailingWhitespaceLength = 0;
            var newLineLength = 0;
            var ascent = 0d;
            var descent = 0d;
            var lineGap = 0d;
            var fontRenderingEmSize = 0d;

            for (var index = 0; index < _textRuns.Count; index++)
            {
                var textRun = _textRuns[index];

                var fontMetrics =
                    new FontMetrics(textRun.Properties.Typeface, textRun.Properties.FontRenderingEmSize);

                if (fontRenderingEmSize < textRun.Properties.FontRenderingEmSize)
                {
                    fontRenderingEmSize = textRun.Properties.FontRenderingEmSize;
                    
                    if (ascent > fontMetrics.Ascent)
                    {
                        ascent = fontMetrics.Ascent;
                    }

                    if (descent < fontMetrics.Descent)
                    {
                        descent = fontMetrics.Descent;
                    }

                    if (lineGap < fontMetrics.LineGap)
                    {
                        lineGap = fontMetrics.LineGap;
                    }
                }

                switch (_paragraphProperties.FlowDirection)
                {
                    case FlowDirection.LeftToRight:
                        {
                            if (index == _textRuns.Count - 1)
                            {
                                width = widthIncludingWhitespace + textRun.GlyphRun.Metrics.Width;
                                trailingWhitespaceLength = textRun.GlyphRun.Metrics.TrailingWhitespaceLength;
                                newLineLength = textRun.GlyphRun.Metrics.NewlineLength;
                            }

                            break;
                        }

                    case FlowDirection.RightToLeft:
                        {
                            if (index == _textRuns.Count - 1)
                            {
                                var firstRun = _textRuns[0];

                                var offset = firstRun.GlyphRun.Metrics.WidthIncludingTrailingWhitespace -
                                             firstRun.GlyphRun.Metrics.Width;

                                width = widthIncludingWhitespace +
                                    textRun.GlyphRun.Metrics.WidthIncludingTrailingWhitespace - offset;

                                trailingWhitespaceLength = firstRun.GlyphRun.Metrics.TrailingWhitespaceLength;
                                newLineLength = firstRun.GlyphRun.Metrics.NewlineLength;
                            }

                            break;
                        }
                }

                widthIncludingWhitespace += textRun.GlyphRun.Metrics.WidthIncludingTrailingWhitespace;
            }

            var start = GetParagraphOffsetX(width, widthIncludingWhitespace, _paragraphWidth, 
                _paragraphProperties.TextAlignment, _paragraphProperties.FlowDirection);

            var lineHeight = _paragraphProperties.LineHeight;

            var height = double.IsNaN(lineHeight) || MathUtilities.IsZero(lineHeight) ?
                descent - ascent + lineGap :
                lineHeight;

            return new TextLineMetrics(widthIncludingWhitespace > _paragraphWidth, height, newLineLength, start,
                -ascent, trailingWhitespaceLength, width, widthIncludingWhitespace);
        }

        private sealed class OrderedBidiRun
        {
            public OrderedBidiRun(ShapedTextCharacters run) => Run = run;

            public sbyte Level => Run.BidiLevel;

            public ShapedTextCharacters Run { get; }

            public OrderedBidiRun? Next { get; set; }

            public void Reverse() => Run.ShapedBuffer.GlyphInfos.Span.Reverse();
        }

        private sealed class BidiRange
        {
            public int Level { get; set; }

            public OrderedBidiRun? Left { get; set; }

            public OrderedBidiRun? Right { get; set; }

            public BidiRange? Previous { get; set; }

            public static BidiRange MergeWithPrevious(BidiRange range)
            {
                var previous = range.Previous;

                BidiRange left;
                BidiRange right;

                if ((previous!.Level & 1) != 0)
                {
                    // Odd, previous goes to the right of range.
                    left = range;
                    right = previous;
                }
                else
                {
                    // Even, previous goes to the left of range.
                    left = previous;
                    right = range;
                }

                // Stitch them
                left.Right!.Next = right.Left;
                previous.Left = left.Left;
                previous.Right = right.Right;

                return previous;
            }
        }
    }
}
