﻿using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        private readonly List<DrawableTextRun> _textRuns;
        private readonly double _paragraphWidth;
        private readonly TextParagraphProperties _paragraphProperties;
        private TextLineMetrics _textLineMetrics;
        private readonly FlowDirection _resolvedFlowDirection;

        public TextLineImpl(List<DrawableTextRun> textRuns, int firstTextSourceIndex, int length, double paragraphWidth,
            TextParagraphProperties paragraphProperties, FlowDirection resolvedFlowDirection = FlowDirection.LeftToRight,
            TextLineBreak? lineBreak = null, bool hasCollapsed = false)
        {
            FirstTextSourceIndex = firstTextSourceIndex;
            Length = length;
            TextLineBreak = lineBreak;
            HasCollapsed = hasCollapsed;

            _textRuns = textRuns;
            _paragraphWidth = paragraphWidth;
            _paragraphProperties = paragraphProperties;

            _resolvedFlowDirection = resolvedFlowDirection;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <inheritdoc/>
        public override int FirstTextSourceIndex { get; }

        /// <inheritdoc/>
        public override int Length { get; }

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
                var offsetY = GetBaselineOffset(this, textRun);

                textRun.Draw(drawingContext, new Point(currentX, currentY + offsetY));

                currentX += textRun.Size.Width;
            }
        }

        private static double GetBaselineOffset(TextLine textLine, DrawableTextRun textRun)
        {
            var baseline = textRun.Baseline;
            var baselineAlignment = textRun.Properties?.BaselineAlignment;

            switch (baselineAlignment)
            {
                case BaselineAlignment.Top:
                    return 0;
                case BaselineAlignment.Center:
                    return textLine.Height / 2 - textRun.Size.Height / 2;
                case BaselineAlignment.Bottom:
                    return textLine.Height - textRun.Size.Height;
                case BaselineAlignment.Baseline:
                case BaselineAlignment.TextTop:
                case BaselineAlignment.TextBottom:
                case BaselineAlignment.Subscript:
                case BaselineAlignment.Superscript:
                    return textLine.Baseline - baseline;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baselineAlignment), baselineAlignment, null);
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

            var collapsedRuns = collapsingProperties.Collapse(this);

            if (collapsedRuns is null)
            {
                return this;
            }

            var collapsedLine = new TextLineImpl(collapsedRuns, FirstTextSourceIndex, Length, _paragraphWidth, _paragraphProperties,
                _resolvedFlowDirection, TextLineBreak, true);

            if (collapsedRuns.Count > 0)
            {
                collapsedLine.FinalizeLine();
            }

            return collapsedLine;
        }

        /// <inheritdoc/>
        public override void Justify(JustificationProperties justificationProperties)
        {
            justificationProperties.Justify(this);

            _textLineMetrics = CreateLineMetrics();
        }

        /// <inheritdoc/>
        public override CharacterHit GetCharacterHitFromDistance(double distance)
        {
            if (_textRuns.Count == 0)
            {
                return new CharacterHit();
            }

            distance -= Start;

            if (distance <= 0)
            {
                // hit happens before the line, return the first position
                var firstRun = _textRuns[0];

                if (firstRun is ShapedTextCharacters shapedTextCharacters)
                {
                    return shapedTextCharacters.GlyphRun.GetCharacterHitFromDistance(distance, out _);
                }

                return _resolvedFlowDirection == FlowDirection.LeftToRight ?
                    new CharacterHit(FirstTextSourceIndex) :
                    new CharacterHit(FirstTextSourceIndex + Length);
            }

            // process hit that happens within the line
            var characterHit = new CharacterHit();
            var currentPosition = FirstTextSourceIndex;

            foreach (var currentRun in _textRuns)
            {
                switch (currentRun)
                {
                    case ShapedTextCharacters shapedRun:
                        {
                            characterHit = shapedRun.GlyphRun.GetCharacterHitFromDistance(distance, out _);

                            var offset = Math.Max(0, currentPosition - shapedRun.Text.Start);

                            characterHit = new CharacterHit(characterHit.FirstCharacterIndex + offset, characterHit.TrailingLength);

                            break;
                        }
                    default:
                        {
                            if (distance < currentRun.Size.Width / 2)
                            {
                                characterHit = new CharacterHit(currentPosition);
                            }
                            else
                            {
                                characterHit = new CharacterHit(currentPosition, currentRun.TextSourceLength);
                            }
                            break;
                        }
                }

                if (distance <= currentRun.Size.Width)
                {
                    break;
                }

                distance -= currentRun.Size.Width;
                currentPosition += currentRun.TextSourceLength;
            }

            return characterHit;
        }

        /// <inheritdoc/>
        public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var isTrailingHit = characterHit.TrailingLength > 0;
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
            var currentDistance = Start;
            var currentPosition = FirstTextSourceIndex;
            var remainingLength = characterIndex - FirstTextSourceIndex;

            GlyphRun? lastRun = null;

            for (var index = 0; index < _textRuns.Count; index++)
            {
                var textRun = _textRuns[index];

                switch (textRun)
                {
                    case ShapedTextCharacters shapedTextCharacters:
                        {
                            var currentRun = shapedTextCharacters.GlyphRun;

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
                            if (currentPosition + remainingLength <= currentPosition + textRun.Text.Length)
                            {
                                characterHit = new CharacterHit(textRun.Text.Start + remainingLength);

                                var distance = currentRun.GetDistanceFromCharacterHit(characterHit);

                                return currentDistance + distance;
                            }

                            //Look at the left and right edge of the current run
                            if (currentRun.IsLeftToRight)
                            {
                                if (_resolvedFlowDirection == FlowDirection.LeftToRight && (lastRun == null || lastRun.IsLeftToRight))
                                {
                                    if (characterIndex <= currentPosition)
                                    {
                                        return currentDistance;
                                    }
                                }
                                else
                                {
                                    if (characterIndex == currentPosition)
                                    {
                                        return currentDistance;
                                    }
                                }

                                if (characterIndex == currentPosition + textRun.Text.Length && isTrailingHit)
                                {
                                    return currentDistance + currentRun.Size.Width;
                                }
                            }
                            else
                            {
                                if (characterIndex == currentPosition)
                                {
                                    return currentDistance + currentRun.Size.Width;
                                }

                                var nextRun = index + 1 < _textRuns.Count ?
                                    _textRuns[index + 1] as ShapedTextCharacters :
                                    null;

                                if (nextRun != null)
                                {
                                    if (nextRun.ShapedBuffer.IsLeftToRight)
                                    {
                                        if (characterIndex == currentPosition + textRun.Text.Length)
                                        {
                                            return currentDistance;
                                        }
                                    }
                                    else
                                    {
                                        if (currentPosition + nextRun.Text.Length == characterIndex)
                                        {
                                            return currentDistance;
                                        }
                                    }
                                }
                                else
                                {
                                    if (characterIndex > currentPosition + textRun.Text.Length)
                                    {
                                        return currentDistance;
                                    }
                                }
                            }

                            lastRun = currentRun;

                            break;
                        }
                    default:
                        {
                            if (characterIndex == currentPosition)
                            {
                                return currentDistance;
                            }

                            if (characterIndex == currentPosition + textRun.TextSourceLength)
                            {
                                return currentDistance + textRun.Size.Width;
                            }

                            break;
                        }
                }

                //No hit hit found so we add the full width
                currentDistance += textRun.Size.Width;
                currentPosition += textRun.TextSourceLength;
                remainingLength -= textRun.TextSourceLength;

                if (remainingLength <= 0)
                {
                    break;
                }
            }

            return currentDistance;
        }

        /// <inheritdoc/>
        public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (_textRuns.Count == 0)
            {
                return new CharacterHit();
            }

            if (TryFindNextCharacterHit(characterHit, out var nextCharacterHit))
            {
                return nextCharacterHit;
            }

            var lastTextPosition = FirstTextSourceIndex + Length;

            // Can't move, we're after the last character
            var runIndex = GetRunIndexAtCharacterIndex(lastTextPosition, LogicalDirection.Forward, out var currentPosition);

            var currentRun = _textRuns[runIndex];

            switch (currentRun)
            {
                case ShapedTextCharacters shapedRun:
                    {
                        characterHit = shapedRun.GlyphRun.GetNextCaretCharacterHit(characterHit);
                        break;
                    }
                default:
                    {
                        characterHit = new CharacterHit(currentPosition + currentRun.TextSourceLength);
                        break;
                    }
            }

            return characterHit;
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindPreviousCharacterHit(characterHit, out var previousCharacterHit))
            {
                return previousCharacterHit;
            }

            if (characterHit.FirstCharacterIndex <= FirstTextSourceIndex)
            {
                characterHit = new CharacterHit(FirstTextSourceIndex);
            }

            return characterHit; // Can't move, we're before the first character
        }

        /// <inheritdoc/>
        public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            // same operation as move-to-previous
            return GetPreviousCaretCharacterHit(characterHit);
        }

        private IReadOnlyList<TextBounds> GetTextBoundsLeftToRight(int firstTextSourceIndex, int textLength)
        {
            var characterIndex = firstTextSourceIndex + textLength;

            var result = new List<TextBounds>(TextRuns.Count);
            var lastDirection = FlowDirection.LeftToRight;
            var currentDirection = lastDirection;

            var currentPosition = FirstTextSourceIndex;
            var remainingLength = textLength;

            var startX = Start;
            double currentWidth = 0;
            var currentRect = Rect.Empty;

            for (var index = 0; index < TextRuns.Count; index++)
            {
                if (TextRuns[index] is not DrawableTextRun currentRun)
                {
                    continue;
                }

                if (currentPosition + currentRun.TextSourceLength <= firstTextSourceIndex)
                {
                    startX += currentRun.Size.Width;

                    currentPosition += currentRun.TextSourceLength;

                    continue;
                }

                var characterLength = 0;
                var endX = startX;

                if (currentRun is ShapedTextCharacters currentShapedRun)
                {
                    var offset = Math.Max(0, firstTextSourceIndex - currentPosition);

                    currentPosition += offset;

                    var startIndex = currentRun.Text.Start + offset;

                    var endOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(
                       currentShapedRun.ShapedBuffer.IsLeftToRight ?
                            new CharacterHit(startIndex + remainingLength) :
                            new CharacterHit(startIndex));

                    endX += endOffset;

                    var startOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(
                        currentShapedRun.ShapedBuffer.IsLeftToRight ?
                            new CharacterHit(startIndex) :
                            new CharacterHit(startIndex + remainingLength));

                    startX += startOffset;

                    var endHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(endOffset, out _);
                    var startHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(startOffset, out _);

                    characterLength = Math.Abs(endHit.FirstCharacterIndex + endHit.TrailingLength - startHit.FirstCharacterIndex - startHit.TrailingLength);

                    currentDirection = currentShapedRun.ShapedBuffer.IsLeftToRight ?
                        FlowDirection.LeftToRight :
                        FlowDirection.RightToLeft;
                }
                else
                {
                    if (currentPosition < firstTextSourceIndex)
                    {
                        startX += currentRun.Size.Width;
                    }

                    if (currentPosition + currentRun.TextSourceLength <= characterIndex)
                    {
                        endX += currentRun.Size.Width;

                        characterLength = currentRun.TextSourceLength;
                    }
                }

                if (endX < startX)
                {
                    (endX, startX) = (startX, endX);
                }

                //Lines that only contain a linebreak need to be covered here
                if(characterLength == 0)
                {
                    characterLength = NewLineLength;
                }

                var runwidth = endX - startX;
                var currentRunBounds = new TextRunBounds(new Rect(startX, 0, runwidth, Height), currentPosition, characterLength, currentRun);

                if (lastDirection == currentDirection && result.Count > 0 && MathUtilities.AreClose(currentRect.Right, startX))
                {
                    currentRect = currentRect.WithWidth(currentWidth + runwidth);

                    var textBounds = result[result.Count - 1];

                    textBounds.Rectangle = currentRect;

                    textBounds.TextRunBounds.Add(currentRunBounds);
                }
                else
                {
                    currentRect = currentRunBounds.Rectangle;

                    result.Add(new TextBounds(currentRect, currentDirection, new List<TextRunBounds> { currentRunBounds }));
                }

                currentWidth += runwidth;
                currentPosition += characterLength;

                if (currentDirection == FlowDirection.LeftToRight)
                {
                    if (currentPosition > characterIndex)
                    {
                        break;
                    }
                }
                else
                {
                    if (currentPosition <= firstTextSourceIndex)
                    {
                        break;
                    }
                }

                startX = endX;
                lastDirection = currentDirection;
                remainingLength -= characterLength;

                if (remainingLength <= 0)
                {
                    break;
                }
            }

            return result;
        }

        private IReadOnlyList<TextBounds> GetTextBoundsRightToLeft(int firstTextSourceIndex, int textLength)
        {
            var characterIndex = firstTextSourceIndex + textLength;

            var result = new List<TextBounds>(TextRuns.Count);
            var lastDirection = FlowDirection.LeftToRight;
            var currentDirection = lastDirection;

            var currentPosition = FirstTextSourceIndex;
            var remainingLength = textLength;

            var startX = Start + WidthIncludingTrailingWhitespace;
            double currentWidth = 0;
            var currentRect = Rect.Empty;

            for (var index = TextRuns.Count - 1; index >= 0; index--)
            {
                if (TextRuns[index] is not DrawableTextRun currentRun)
                {
                    continue;
                }

                if (currentPosition + currentRun.TextSourceLength <= firstTextSourceIndex)
                {
                    startX -= currentRun.Size.Width;

                    currentPosition += currentRun.TextSourceLength;

                    continue;
                }

                var characterLength = 0;
                var endX = startX;

                if (currentRun is ShapedTextCharacters currentShapedRun)
                {
                    var offset = Math.Max(0, firstTextSourceIndex - currentPosition);

                    currentPosition += offset;

                    var startIndex = currentRun.Text.Start + offset;

                    var endOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(
                        currentShapedRun.ShapedBuffer.IsLeftToRight ?
                            new CharacterHit(startIndex + remainingLength) :
                            new CharacterHit(startIndex));

                    endX += endOffset - currentShapedRun.Size.Width;

                    var startOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(
                        currentShapedRun.ShapedBuffer.IsLeftToRight ?
                            new CharacterHit(startIndex) :
                            new CharacterHit(startIndex + remainingLength));

                    startX += startOffset - currentShapedRun.Size.Width;

                    var endHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(endOffset, out _);
                    var startHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(startOffset, out _);

                    characterLength = Math.Abs(startHit.FirstCharacterIndex + startHit.TrailingLength - endHit.FirstCharacterIndex - endHit.TrailingLength);

                    currentDirection = currentShapedRun.ShapedBuffer.IsLeftToRight ?
                        FlowDirection.LeftToRight :
                        FlowDirection.RightToLeft;
                }
                else
                {
                    if (currentPosition + currentRun.TextSourceLength <= characterIndex)
                    {
                        endX -= currentRun.Size.Width;
                    }

                    if (currentPosition < firstTextSourceIndex)
                    {
                        startX -= currentRun.Size.Width;

                        characterLength = currentRun.TextSourceLength;
                    }
                }

                if (endX < startX)
                {
                    (endX, startX) = (startX, endX);
                }

                //Lines that only contain a linebreak need to be covered here
                if (characterLength == 0)
                {
                    characterLength = NewLineLength;
                }

                var runWidth = endX - startX;
                var currentRunBounds = new TextRunBounds(new Rect(startX, 0, runWidth, Height), currentPosition, characterLength, currentRun);

                if (lastDirection == currentDirection && result.Count > 0 && MathUtilities.AreClose(currentRect.Right, startX))
                {
                    currentRect = currentRect.WithWidth(currentWidth + runWidth);

                    var textBounds = result[result.Count - 1];

                    textBounds.Rectangle = currentRect;

                    textBounds.TextRunBounds.Add(currentRunBounds);
                }
                else
                {
                    currentRect = currentRunBounds.Rectangle;

                    result.Add(new TextBounds(currentRect, currentDirection, new List<TextRunBounds> { currentRunBounds }));
                }

                currentWidth += runWidth;
                currentPosition += characterLength;

                if (currentDirection == FlowDirection.LeftToRight)
                {
                    if (currentPosition > characterIndex)
                    {
                        break;
                    }
                }
                else
                {
                    if (currentPosition <= firstTextSourceIndex)
                    {
                        break;
                    }
                }

                lastDirection = currentDirection;
                remainingLength -= characterLength;

                if (remainingLength <= 0)
                {
                    break;
                }
            }

            return result;
        }

        public override IReadOnlyList<TextBounds> GetTextBounds(int firstTextSourceIndex, int textLength)
        {
            if (_paragraphProperties.FlowDirection == FlowDirection.LeftToRight)
            {
                return GetTextBoundsLeftToRight(firstTextSourceIndex, textLength);
            }

            return GetTextBoundsRightToLeft(firstTextSourceIndex, textLength);
        }

        public TextLineImpl FinalizeLine()
        {
            _textLineMetrics = CreateLineMetrics();

            BidiReorder();

            return this;
        }

        private static sbyte GetRunBidiLevel(DrawableTextRun run, FlowDirection flowDirection)
        {
            if (run is ShapedTextCharacters shapedTextCharacters)
            {
                return shapedTextCharacters.BidiLevel;
            }

            var defaultLevel = flowDirection == FlowDirection.LeftToRight ? 0 : 1;

            return (sbyte)defaultLevel;
        }

        private void BidiReorder()
        {
            if (_textRuns.Count == 0)
            {
                return;
            }

            // Build up the collection of ordered runs.
            var run = _textRuns[0];

            OrderedBidiRun orderedRun = new(run, GetRunBidiLevel(run, _resolvedFlowDirection));

            var current = orderedRun;

            for (var i = 1; i < _textRuns.Count; i++)
            {
                run = _textRuns[i];

                current.Next = new OrderedBidiRun(run, GetRunBidiLevel(run, _resolvedFlowDirection));

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
                var currentRun = _textRuns[i];

                var level = GetRunBidiLevel(currentRun, _resolvedFlowDirection);

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
                        if (current.Run is ShapedTextCharacters { IsReversed: false } shapedTextCharacters)
                        {
                            shapedTextCharacters.Reverse();
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
            var lastCodepointIndex = FirstTextSourceIndex + Length;

            if (codepointIndex >= lastCodepointIndex)
            {
                return false; // Cannot go forward anymore
            }

            if (codepointIndex < FirstTextSourceIndex)
            {
                codepointIndex = FirstTextSourceIndex;
            }

            var runIndex = GetRunIndexAtCharacterIndex(codepointIndex, LogicalDirection.Forward, out var currentPosition);

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextCharacters shapedRun:
                        {
                            var foundCharacterHit = shapedRun.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

                            var isAtEnd = foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength == FirstTextSourceIndex + Length;

                            if (isAtEnd && !shapedRun.GlyphRun.IsLeftToRight)
                            {
                                nextCharacterHit = foundCharacterHit;

                                return true;
                            }

                            var characterIndex = codepointIndex - shapedRun.Text.Start;

                            if (characterIndex < 0 && shapedRun.ShapedBuffer.IsLeftToRight)
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

                            break;
                        }
                    default:
                        {
                            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                            if (textPosition == currentPosition)
                            {
                                nextCharacterHit = new CharacterHit(currentPosition + currentRun.TextSourceLength);

                                return true;
                            }

                            break;
                        }
                }

                currentPosition += currentRun.TextSourceLength;
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

            if (characterIndex == FirstTextSourceIndex)
            {
                previousCharacterHit = new CharacterHit(FirstTextSourceIndex);

                return true;
            }

            previousCharacterHit = characterHit;

            if (characterIndex < FirstTextSourceIndex)
            {
                return false; // Cannot go backward anymore.
            }

            var runIndex = GetRunIndexAtCharacterIndex(characterIndex, LogicalDirection.Backward, out var currentPosition);

            while (runIndex >= 0)
            {
                var currentRun = _textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextCharacters shapedRun:
                        {
                            var foundCharacterHit = shapedRun.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

                            if (foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength < characterIndex)
                            {
                                previousCharacterHit = foundCharacterHit;

                                return true;
                            }

                            var previousPosition = foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength;

                            if (foundCharacterHit.TrailingLength > 0 && previousPosition == characterIndex)
                            {
                                previousCharacterHit = new CharacterHit(foundCharacterHit.FirstCharacterIndex);
                            }

                            if (previousCharacterHit != characterHit)
                            {
                                return true;
                            }

                            break;
                        }
                    default:
                        {
                            if (characterIndex == currentPosition + currentRun.TextSourceLength)
                            {
                                previousCharacterHit = new CharacterHit(currentPosition);

                                return true;
                            }

                            break;
                        }
                }

                currentPosition -= currentRun.TextSourceLength;
                runIndex--;
            }

            return false;
        }

        /// <summary>
        /// Gets the run index of the specified codepoint index.
        /// </summary>
        /// <param name="codepointIndex">The codepoint index.</param>
        /// <param name="direction">The logical direction.</param>
        /// <param name="textPosition">The text position of the found run index.</param>
        /// <returns>The text run index.</returns>
        private int GetRunIndexAtCharacterIndex(int codepointIndex, LogicalDirection direction, out int textPosition)
        {
            var runIndex = 0;
            textPosition = FirstTextSourceIndex;
            DrawableTextRun? previousRun = null;

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextCharacters shapedRun:
                        {
                            if (previousRun is ShapedTextCharacters previousShaped && !previousShaped.ShapedBuffer.IsLeftToRight)
                            {
                                if (shapedRun.ShapedBuffer.IsLeftToRight)
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

                            if (runIndex + 1 >= _textRuns.Count)
                            {
                                return runIndex;
                            }

                            break;
                        }

                    default:
                        {
                            if (codepointIndex == textPosition)
                            {
                                return runIndex;
                            }

                            if (runIndex + 1 >= _textRuns.Count)
                            {
                                return runIndex;
                            }

                            break;
                        }
                }

                runIndex++;
                previousRun = currentRun;
                textPosition += currentRun.TextSourceLength;
            }

            return runIndex;
        }

        private TextLineMetrics CreateLineMetrics()
        {
            var glyphTypeface = _paragraphProperties.DefaultTextRunProperties.Typeface.GlyphTypeface;
            var fontRenderingEmSize = _paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize;
            var scale = fontRenderingEmSize / glyphTypeface.DesignEmHeight;

            var width = 0d;
            var widthIncludingWhitespace = 0d;
            var trailingWhitespaceLength = 0;
            var newLineLength = 0;
            var ascent = glyphTypeface.Ascent * scale;
            var descent = glyphTypeface.Descent * scale;
            var lineGap = glyphTypeface.LineGap * scale;

            var height = descent - ascent + lineGap;

            var lineHeight = _paragraphProperties.LineHeight;

            for (var index = 0; index < _textRuns.Count; index++)
            {
                switch (_textRuns[index])
                {
                    case ShapedTextCharacters textRun:
                        {
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

                                if (descent - ascent + lineGap > height)
                                {
                                    height = descent - ascent + lineGap;
                                }
                            }

                            if (index == _textRuns.Count - 1)
                            {
                                width = widthIncludingWhitespace + textRun.GlyphRun.Metrics.Width;
                                trailingWhitespaceLength = textRun.GlyphRun.Metrics.TrailingWhitespaceLength;
                                newLineLength = textRun.GlyphRun.Metrics.NewlineLength;
                            }

                            widthIncludingWhitespace += textRun.GlyphRun.Metrics.WidthIncludingTrailingWhitespace;

                            break;
                        }

                    case { } drawableTextRun:
                        {
                            widthIncludingWhitespace += drawableTextRun.Size.Width;

                            switch (_paragraphProperties.FlowDirection)
                            {
                                case FlowDirection.LeftToRight:
                                    {
                                        if (index == _textRuns.Count - 1)
                                        {
                                            width = widthIncludingWhitespace;
                                            trailingWhitespaceLength = 0;
                                            newLineLength = 0;
                                        }

                                        break;
                                    }

                                case FlowDirection.RightToLeft:
                                    {
                                        if (index == _textRuns.Count - 1)
                                        {
                                            width = widthIncludingWhitespace;
                                            trailingWhitespaceLength = 0;
                                            newLineLength = 0;
                                        }

                                        break;
                                    }
                            }

                            if (drawableTextRun.Size.Height > height)
                            {
                                height = drawableTextRun.Size.Height;
                            }

                            if (ascent > -drawableTextRun.Baseline)
                            {
                                ascent = -drawableTextRun.Baseline;
                            }

                            break;
                        }
                }
            }

            var start = GetParagraphOffsetX(width, widthIncludingWhitespace);

            if (!double.IsNaN(lineHeight) && !MathUtilities.IsZero(lineHeight))
            {
                if (lineHeight > height)
                {
                    height = lineHeight;
                }
            }

            return new TextLineMetrics(widthIncludingWhitespace > _paragraphWidth, height, newLineLength, start,
                -ascent, trailingWhitespaceLength, width, widthIncludingWhitespace);
        }

        /// <summary>
        /// Gets the text line offset x.
        /// </summary>
        /// <param name="width">The line width.</param>
        /// <param name="widthIncludingTrailingWhitespace">The paragraph width including whitespace.</param>

        /// <returns>The paragraph offset.</returns>
        private double GetParagraphOffsetX(double width, double widthIncludingTrailingWhitespace)
        {
            if (double.IsPositiveInfinity(_paragraphWidth))
            {
                return 0;
            }

            var textAlignment = _paragraphProperties.TextAlignment;
            var paragraphFlowDirection = _paragraphProperties.FlowDirection;

            switch (textAlignment)
            {
                case TextAlignment.Start:
                    {
                        textAlignment = paragraphFlowDirection == FlowDirection.LeftToRight ? TextAlignment.Left : TextAlignment.Right;
                        break;
                    }
                case TextAlignment.End:
                    {
                        textAlignment = paragraphFlowDirection == FlowDirection.RightToLeft ? TextAlignment.Left : TextAlignment.Right;
                        break;
                    }
                case TextAlignment.DetectFromContent:
                    {
                        textAlignment = _resolvedFlowDirection == FlowDirection.LeftToRight ? TextAlignment.Left : TextAlignment.Right;
                        break;
                    }
            }

            switch (textAlignment)
            {
                case TextAlignment.Center:
                    return Math.Max(0, (_paragraphWidth - width) / 2);

                case TextAlignment.Right:
                    return Math.Max(0, _paragraphWidth - widthIncludingTrailingWhitespace);

                default:
                    return 0;
            }
        }

        private sealed class OrderedBidiRun
        {
            public OrderedBidiRun(DrawableTextRun run, sbyte level)
            {
                Run = run;
                Level = level;
            }

            public sbyte Level { get; }

            public DrawableTextRun Run { get; }

            public OrderedBidiRun? Next { get; set; }
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
