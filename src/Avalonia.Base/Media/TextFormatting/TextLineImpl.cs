using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        internal static Comparer<TextBounds> TextBoundsComparer { get; } =
            Comparer<TextBounds>.Create((x, y) => x.Rectangle.Left.CompareTo(y.Rectangle.Left));

        internal IReadOnlyList<IndexedTextRun>? _indexedTextRuns;
        private readonly TextRun[] _textRuns;
        private readonly double _paragraphWidth;
        private readonly TextParagraphProperties _paragraphProperties;
        private TextLineMetrics _textLineMetrics;
        private TextLineBreak? _textLineBreak;
        private readonly FlowDirection _resolvedFlowDirection;

        private Rect _inkBounds;
        private Rect _bounds;

        public TextLineImpl(TextRun[] textRuns, int firstTextSourceIndex, int length, double paragraphWidth,
            TextParagraphProperties paragraphProperties, FlowDirection resolvedFlowDirection = FlowDirection.LeftToRight,
            TextLineBreak? lineBreak = null, bool hasCollapsed = false)
        {
            FirstTextSourceIndex = firstTextSourceIndex;
            Length = length;
            _textLineBreak = lineBreak;
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
        public override TextLineBreak? TextLineBreak => _textLineBreak;

        /// <inheritdoc/>
        public override bool HasCollapsed { get; }

        /// <inheritdoc/>
        public override bool HasOverflowed => _textLineMetrics.HasOverflowed;

        /// <inheritdoc/>
        public override double Baseline => _textLineMetrics.TextBaseline;

        /// <inheritdoc/>
        public override double Extent => _textLineMetrics.Extent;

        /// <inheritdoc/>
        public override double Height => _textLineMetrics.Height;

        /// <inheritdoc/>
        public override int NewLineLength => _textLineMetrics.NewlineLength;

        /// <inheritdoc/>
        public override double OverhangAfter => _textLineMetrics.OverhangAfter;

        /// <inheritdoc/>
        public override double OverhangLeading => _textLineMetrics.OverhangLeading;

        /// <inheritdoc/>
        public override double OverhangTrailing => _textLineMetrics.OverhangTrailing;

        /// <inheritdoc/>
        public override int TrailingWhitespaceLength => _textLineMetrics.TrailingWhitespaceLength;

        /// <inheritdoc/>
        public override double Start => _textLineMetrics.Start;

        /// <inheritdoc/>
        public override double Width => _textLineMetrics.Width;

        /// <inheritdoc/>
        public override double WidthIncludingTrailingWhitespace => _textLineMetrics.WidthIncludingTrailingWhitespace;

        /// <summary>
        /// Get the logical text bounds.
        /// </summary>
        internal Rect Bounds => _bounds;

        /// <summary>
        /// Get the bounding box that is covered with black pixels.
        /// </summary>
        internal Rect InkBounds => _inkBounds;

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point lineOrigin)
        {
            var (currentX, currentY) = lineOrigin + new Point(Start, 0);

            foreach (var textRun in _textRuns)
            {
                switch (textRun)
                {
                    case DrawableTextRun drawableTextRun:
                        {
                            var offsetY = GetBaselineOffset(this, drawableTextRun);

                            drawableTextRun.Draw(drawingContext, new Point(currentX, currentY + offsetY));

                            currentX += drawableTextRun.Size.Width;

                            break;
                        }
                }
            }
        }

        public static double GetBaselineOffset(TextLine textLine, DrawableTextRun textRun)
        {
            var baseline = textRun.Baseline;
            var baselineAlignment = textRun.Properties?.BaselineAlignment;

            var baselineOffset = -baseline;

            switch (baselineAlignment)
            {
                case BaselineAlignment.Baseline:
                    baselineOffset += textLine.Baseline;
                    break;
                case BaselineAlignment.Top:
                case BaselineAlignment.TextTop:
                    baselineOffset += textLine.Height - textLine.Extent + textRun.Size.Height / 2;
                    break;
                case BaselineAlignment.Center:
                    baselineOffset += textLine.Height / 2 + baseline - textRun.Size.Height / 2;
                    break;
                case BaselineAlignment.Subscript:
                case BaselineAlignment.Bottom:
                case BaselineAlignment.TextBottom:
                    baselineOffset += textLine.Height - textRun.Size.Height + baseline;
                    break;
                case BaselineAlignment.Superscript:
                    baselineOffset += baseline;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baselineAlignment), baselineAlignment, null);
            }

            return baselineOffset;
        }

        /// <inheritdoc/>
        public override TextLine Collapse(params TextCollapsingProperties?[] collapsingPropertiesList)
        {
            if (collapsingPropertiesList.Length == 0)
            {
                return this;
            }

            var collapsingProperties = collapsingPropertiesList[0];

            if (collapsingProperties is null)
            {
                return this;
            }

            var collapsedRuns = collapsingProperties.Collapse(this);

            if (collapsedRuns is null)
            {
                return this;
            }

            var collapsedLine = new TextLineImpl(collapsedRuns, FirstTextSourceIndex, Length, _paragraphWidth, _paragraphProperties,
                _resolvedFlowDirection, TextLineBreak, true);

            if (collapsedRuns.Length > 0)
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
            if (_textRuns.Length == 0)
            {
                return new CharacterHit(FirstTextSourceIndex);
            }

            distance -= Start;

            var lastIndex = _textRuns.Length - 1;
            var lineLength = Length;

            if (_textRuns[lastIndex] is TextEndOfLine textEndOfLine)
            {
                lastIndex--;
                lineLength -= textEndOfLine.Length;
            }

            var currentPosition = FirstTextSourceIndex;

            if (lastIndex < 0)
            {
                return new CharacterHit(currentPosition);
            }

            if (distance <= 0)
            {
                var firstRun = _textRuns[0];

                if (_paragraphProperties.FlowDirection == FlowDirection.RightToLeft)
                {
                    currentPosition += lineLength - firstRun.Length;
                }

                return GetRunCharacterHit(firstRun, currentPosition, 0);
            }

            if (distance >= WidthIncludingTrailingWhitespace)
            {
                var lastRun = _textRuns[lastIndex];

                if (_paragraphProperties.FlowDirection == FlowDirection.LeftToRight)
                {
                    currentPosition += lineLength - lastRun.Length;
                }

                return GetRunCharacterHit(lastRun, currentPosition, distance);
            }

            // process hit that happens within the line
            var characterHit = new CharacterHit();
            var currentDistance = 0.0;

            for (var i = 0; i <= lastIndex; i++)
            {
                var currentRun = _textRuns[i];

                if (currentRun is ShapedTextRun shapedRun && !shapedRun.ShapedBuffer.IsLeftToRight)
                {
                    var rightToLeftIndex = i;
                    currentPosition += currentRun.Length;

                    while (rightToLeftIndex + 1 <= _textRuns.Length - 1)
                    {
                        var nextShaped = _textRuns[++rightToLeftIndex] as ShapedTextRun;

                        if (nextShaped == null || nextShaped.ShapedBuffer.IsLeftToRight)
                        {
                            break;
                        }

                        currentPosition += nextShaped.Length;

                        rightToLeftIndex++;
                    }

                    for (var j = i; i <= rightToLeftIndex; j++)
                    {
                        if (j > _textRuns.Length - 1)
                        {
                            break;
                        }

                        currentRun = _textRuns[j];

                        if (currentRun is not ShapedTextRun)
                        {
                            continue;
                        }

                        shapedRun = (ShapedTextRun)currentRun;

                        if (currentDistance + shapedRun.Size.Width <= distance)
                        {
                            currentDistance += shapedRun.Size.Width;
                            currentPosition -= currentRun.Length;

                            continue;
                        }

                        return GetRunCharacterHit(currentRun, currentPosition, distance - currentDistance);
                    }
                }

                characterHit = GetRunCharacterHit(currentRun, currentPosition, distance - currentDistance);

                if (currentRun is DrawableTextRun drawableTextRun)
                {
                    if (i < _textRuns.Length - 1 && currentDistance + drawableTextRun.Size.Width < distance)
                    {
                        currentDistance += drawableTextRun.Size.Width;

                        currentPosition += currentRun.Length;

                        continue;
                    }
                }
                else
                {
                    currentPosition += currentRun.Length;

                    continue;
                }

                break;
            }

            return characterHit;
        }

        private static CharacterHit GetRunCharacterHit(TextRun run, int currentPosition, double distance)
        {
            CharacterHit characterHit;

            switch (run)
            {
                case ShapedTextRun shapedRun:
                    {
                        characterHit = shapedRun.GlyphRun.GetCharacterHitFromDistance(distance, out _);

                        var offset = 0;

                        if (shapedRun.GlyphRun.IsLeftToRight)
                        {
                            offset = Math.Max(0, currentPosition - shapedRun.GlyphRun.Metrics.FirstCluster);
                        }

                        characterHit = new CharacterHit(offset + characterHit.FirstCharacterIndex, characterHit.TrailingLength);

                        break;
                    }
                case DrawableTextRun drawableTextRun:
                    {
                        if (distance < drawableTextRun.Size.Width / 2)
                        {
                            characterHit = new CharacterHit(currentPosition);
                        }
                        else
                        {
                            characterHit = new CharacterHit(currentPosition, run.Length);
                        }
                        break;
                    }
                default:
                    characterHit = new CharacterHit(currentPosition, run.Length);

                    break;
            }

            return characterHit;
        }

        /// <inheritdoc/>
        public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            if (_indexedTextRuns is null || _indexedTextRuns.Count == 0)
            {
                return Start;
            }

            var characterIndex = Math.Min(
            characterHit.FirstCharacterIndex + characterHit.TrailingLength,
            FirstTextSourceIndex + Length);

            var currentPosition = FirstTextSourceIndex;

            static FlowDirection GetDirection(TextRun textRun, FlowDirection currentDirection)
            {
                if (textRun is ShapedTextRun shapedTextRun)
                {
                    return shapedTextRun.ShapedBuffer.IsLeftToRight ?
                        FlowDirection.LeftToRight :
                        FlowDirection.RightToLeft;
                }

                return currentDirection;
            }

            IndexedTextRun FindIndexedRun(out int index)
            {
                index = 0;

                IndexedTextRun currentIndexedRun = _indexedTextRuns[index];

                while (currentIndexedRun.TextSourceCharacterIndex != currentPosition)
                {
                    if (index + 1 == _indexedTextRuns.Count)
                    {
                        break;
                    }

                    index++;

                    currentIndexedRun = _indexedTextRuns[index];
                }

                return currentIndexedRun;
            }

            double GetPreceedingDistance(int firstIndex)
            {
                var distance = 0.0;

                for (var i = 0; i < firstIndex; i++)
                {
                    var currentRun = _textRuns[i];

                    if (currentRun is DrawableTextRun drawableTextRun)
                    {
                        distance += drawableTextRun.Size.Width;
                    }
                }

                return distance;
            }

            TextRun? currentTextRun = null;

            var currentIndexedRun = FindIndexedRun(out var indexedRunIndex);

            while (currentPosition < FirstTextSourceIndex + Length)
            {
                currentTextRun = currentIndexedRun.TextRun;

                if (currentTextRun == null)
                {
                    break;
                }

                if (currentIndexedRun.TextSourceCharacterIndex + currentTextRun.Length <= characterHit.FirstCharacterIndex)
                {
                    if (currentPosition + currentTextRun.Length < FirstTextSourceIndex + Length)
                    {
                        currentPosition += currentTextRun.Length;

                        currentIndexedRun = FindIndexedRun(out indexedRunIndex);

                        continue;
                    }
                }

                break;
            }

            if (currentTextRun == null)
            {
                return Start;
            }

            var directionalWidth = 0.0;
            var firstRunIndex = currentIndexedRun.RunIndex;

            var currentDirection = GetDirection(currentTextRun, _resolvedFlowDirection);

            var currentX = Start + GetPreceedingDistance(currentIndexedRun.RunIndex);

            if (currentTextRun is DrawableTextRun currentDrawable)
            {
                directionalWidth = currentDrawable.Size.Width;
            }

            var lastRunIndex = GetLastDirectionalRunIndex(indexedRunIndex, currentDirection, ref directionalWidth);

            switch (currentDirection)
            {
                case FlowDirection.RightToLeft:
                    {
                        return GetTextRunBoundsRightToLeft(firstRunIndex, lastRunIndex, currentX + directionalWidth, characterIndex,
                                currentPosition, 1, out _, out _).Rectangle.Right;
                    }
                default:
                    {
                        return GetTextBoundsLeftToRight(firstRunIndex, lastRunIndex, currentX, characterIndex,
                                currentPosition, 1, out _, out _).Rectangle.Left;
                    }
            }
        }

        /// <inheritdoc/>
        public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (_textRuns.Length == 0 || _indexedTextRuns is null)
            {
                return new CharacterHit();
            }

            var currentCharacterrHit = characterHit;
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var currentRun = GetRunAtCharacterIndex(characterIndex, LogicalDirection.Forward, out var currentPosition);

            var nextCharacterHit = characterHit;

            switch (currentRun)
            {
                case ShapedTextRun shapedRun:
                    {
                        var offset = Math.Max(0, currentPosition - shapedRun.GlyphRun.Metrics.FirstCluster - characterHit.TrailingLength);

                        if (offset > 0)
                        {
                            currentCharacterrHit = new CharacterHit(Math.Max(0, characterHit.FirstCharacterIndex - offset), characterHit.TrailingLength);
                        }

                        nextCharacterHit = shapedRun.GlyphRun.GetNextCaretCharacterHit(currentCharacterrHit);

                        if (offset > 0)
                        {
                            nextCharacterHit = new CharacterHit(nextCharacterHit.FirstCharacterIndex + offset, nextCharacterHit.TrailingLength);
                        }
                        break;
                    }
                case TextRun:
                    {
                        nextCharacterHit = new CharacterHit(currentPosition + currentRun.Length);
                        break;
                    }
            }

            if (characterIndex == nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength)
            {
                return characterHit;
            }

            return nextCharacterHit;
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            return GetPreviousCharacterHit(characterHit, false);
        }

        /// <inheritdoc/>
        public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            return GetPreviousCharacterHit(characterHit, true);
        }

        private static FlowDirection GetRunDirection(TextRun? textRun, FlowDirection currentDirection)
        {
            if (textRun is ShapedTextRun shapedTextRun)
            {
                return shapedTextRun.ShapedBuffer.IsLeftToRight ?
                    FlowDirection.LeftToRight :
                    FlowDirection.RightToLeft;
            }

            return currentDirection;
        }

        /// <summary>
        /// Get the last consecutive visual run index that shares the same direction as the current direction.
        /// </summary>
        /// <param name="indexedRunIndex">The current logical run's index.</param>
        /// <param name="flowDirection">The current flow direction.</param>
        /// <param name="directionalWidth">The current directional width.</param>
        /// <returns>
        /// The last consecutive visual run index that shares the same direction as the current direction.
        /// </returns>
        private int GetLastDirectionalRunIndex(int indexedRunIndex, FlowDirection flowDirection, ref double directionalWidth)
        {
            if (_indexedTextRuns is null)
            {
                return -1;
            }

            var lastRunIndex = _indexedTextRuns[indexedRunIndex].RunIndex;

            // Find consecutive runs of same direction
            while (indexedRunIndex + 1 < _indexedTextRuns.Count)
            {
                var nextIndexedRun = _indexedTextRuns[++indexedRunIndex];

                if (nextIndexedRun.RunIndex != lastRunIndex + 1)
                {
                    break;
                }

                var nextRun = nextIndexedRun.TextRun;

                if (nextRun is null)
                {
                    break;
                }

                var nextDirection = GetRunDirection(nextRun, flowDirection);

                if (nextDirection != flowDirection)
                {
                    break;
                }

                if (nextRun is DrawableTextRun nextDrawable)
                {
                    directionalWidth += nextDrawable.Size.Width;
                }

                lastRunIndex = nextIndexedRun.RunIndex;
            }

            return lastRunIndex;
        }

        public override IReadOnlyList<TextBounds> GetTextBounds(int firstTextSourceIndex, int textLength)
        {
            if (textLength == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(textLength), textLength, $"{nameof(textLength)} ('0') must be a non-zero value. ");
            }

            if (_indexedTextRuns is null || _indexedTextRuns.Count == 0)
            {
                return [];
            }

            var currentPosition = FirstTextSourceIndex;
            var remainingLength = textLength;

            //We can return early if the requested text range is before the line's text range.
            if (firstTextSourceIndex + textLength < FirstTextSourceIndex)
            {
                var indexedTextRun = _indexedTextRuns[0];
                var currentDirection = GetRunDirection(indexedTextRun.TextRun, _resolvedFlowDirection);

                return [new TextBounds(new Rect(0, 0, 0, Height), currentDirection, [])];
            }

            //We can return early if the requested text range is after the line's text range.
            if (firstTextSourceIndex >= FirstTextSourceIndex + Length)
            {
                var indexedTextRun = _indexedTextRuns[_indexedTextRuns.Count - 1];
                var currentDirection = GetRunDirection(indexedTextRun.TextRun, _resolvedFlowDirection);

                return [new TextBounds(new Rect(WidthIncludingTrailingWhitespace, 0, 0, Height), currentDirection, [])];
            }

            var result = new List<TextBounds>();

            TextBounds? lastBounds = null;

            while (remainingLength > 0 && currentPosition < FirstTextSourceIndex + Length)
            {
                var currentIndexedRun = FindIndexedRun(out var indexedRunIndex);

                if (currentIndexedRun == null)
                {
                    break;
                }

                var currentTextRun = currentIndexedRun.TextRun;

                if (currentTextRun == null)
                {
                    break;
                }

                var currentDirection = GetRunDirection(currentTextRun, _resolvedFlowDirection);

                if (currentIndexedRun.TextSourceCharacterIndex + currentTextRun.Length <= firstTextSourceIndex)
                {
                    currentPosition += currentTextRun.Length;

                    continue;
                }

                var currentX = Start + GetPreceedingDistance(currentIndexedRun.RunIndex);
                var directionalWidth = 0.0;

                if (currentTextRun is DrawableTextRun currentDrawable)
                {
                    directionalWidth = currentDrawable.Size.Width;
                }

                var firstRunIndex = currentIndexedRun.RunIndex;
                var lastRunIndex = GetLastDirectionalRunIndex(indexedRunIndex, currentDirection, ref directionalWidth);

                TextBounds currentBounds;
                int coveredLength;

                switch (currentDirection)
                {
                    case FlowDirection.RightToLeft:
                        {
                            currentBounds = GetTextRunBoundsRightToLeft(firstRunIndex, lastRunIndex, currentX + directionalWidth, firstTextSourceIndex,
                                    currentPosition, remainingLength, out coveredLength, out currentPosition);

                            break;
                        }
                    default:
                        {
                            currentBounds = GetTextBoundsLeftToRight(firstRunIndex, lastRunIndex, currentX, firstTextSourceIndex,
                                   currentPosition, remainingLength, out coveredLength, out currentPosition);

                            break;
                        }
                }

                if (lastBounds != null && TryMergeWithLastBounds(currentBounds, lastBounds))
                {
                    currentBounds = lastBounds;

                    result[result.Count - 1] = currentBounds;
                }
                else
                {
                    result.Add(currentBounds);
                }

                lastBounds = currentBounds;

                if (coveredLength <= 0)
                {
                    throw new InvalidOperationException("Covered length must be greater than zero.");
                }

                remainingLength -= coveredLength;
            }

            result.Sort(TextBoundsComparer);

            return result;

            IndexedTextRun FindIndexedRun(out int index)
            {
                index = 0;

                var currentIndexedRun = _indexedTextRuns[index];

                while (currentIndexedRun.TextSourceCharacterIndex != currentPosition)
                {
                    if (index + 1 == _indexedTextRuns.Count)
                    {
                        break;
                    }

                    index++;

                    currentIndexedRun = _indexedTextRuns[index];
                }

                return currentIndexedRun;
            }

            double GetPreceedingDistance(int firstIndex)
            {
                var distance = 0.0;

                for (var i = 0; i < firstIndex; i++)
                {
                    var currentRun = _textRuns[i];

                    if (currentRun is DrawableTextRun drawableTextRun)
                    {
                        distance += drawableTextRun.Size.Width;
                    }
                }

                return distance;
            }

            bool TryMergeWithLastBounds(TextBounds currentBounds, TextBounds lastBounds)
            {
                if (currentBounds.FlowDirection != lastBounds.FlowDirection)
                {
                    return false;
                }

                if (currentBounds.Rectangle.Left == lastBounds.Rectangle.Right)
                {
                    foreach (var runBounds in currentBounds.TextRunBounds)
                    {
                        lastBounds.TextRunBounds.Add(runBounds);
                    }

                    lastBounds.Rectangle = lastBounds.Rectangle.Union(currentBounds.Rectangle);

                    return true;
                }

                if (currentBounds.Rectangle.Right == lastBounds.Rectangle.Left)
                {
                    for (int i = 0; i < currentBounds.TextRunBounds.Count; i++)
                    {
                        lastBounds.TextRunBounds.Insert(i, currentBounds.TextRunBounds[i]);
                    }

                    lastBounds.Rectangle = lastBounds.Rectangle.Union(currentBounds.Rectangle);

                    return true;
                }

                return false;
            }
        }

        private CharacterHit GetPreviousCharacterHit(CharacterHit characterHit, bool isBackspaceDelete)
        {
            if (_textRuns.Length == 0 || _indexedTextRuns is null)
            {
                return new CharacterHit();
            }

            if (characterHit.TrailingLength > 0 && characterHit.FirstCharacterIndex <= FirstTextSourceIndex)
            {
                return new CharacterHit(FirstTextSourceIndex);
            }

            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (characterIndex <= FirstTextSourceIndex)
            {
                return new CharacterHit(FirstTextSourceIndex);
            }

            var currentRun = GetRunAtCharacterIndex(characterIndex, LogicalDirection.Backward, out var currentPosition);

            var previousCharacterHit = characterHit;

            switch (currentRun)
            {
                case ShapedTextRun shapedRun:
                    {
                        //Determine the start of the first hit in local positions.
                        var runOffset = Math.Max(0, characterIndex - currentPosition);

                        var firstCluster = shapedRun.GlyphRun.Metrics.FirstCluster;

                        //Current position is a text source index and first cluster is relative to the GlyphRun's buffer.
                        var textSourceOffset = currentPosition - firstCluster;

                        if (isBackspaceDelete)
                        {
                            var length = 0;

                            while (Codepoint.ReadAt(shapedRun.GlyphRun.Characters.Span, length, out var count) is Codepoint codepoint && codepoint != Codepoint.ReplacementCodepoint)
                            {
                                if (codepoint.Value == 0x0D && Codepoint.ReadAt(shapedRun.GlyphRun.Characters.Span, length + count, out var lfCount).Value == 0x0A)
                                {
                                    count += lfCount;
                                }

                                if (length + count >= runOffset)
                                {
                                    break;
                                }

                                length += count;
                            }

                            previousCharacterHit = new CharacterHit(characterIndex - runOffset + length);
                        }
                        else
                        {
                            previousCharacterHit = shapedRun.GlyphRun.GetPreviousCaretCharacterHit(new CharacterHit(firstCluster + runOffset));

                            if (textSourceOffset > 0)
                            {
                                previousCharacterHit = new CharacterHit(textSourceOffset + previousCharacterHit.FirstCharacterIndex, previousCharacterHit.TrailingLength);
                            }
                        }

                        break;
                    }
                case TextRun:
                    {
                        previousCharacterHit = new CharacterHit(currentPosition);

                        break;
                    }
            }

            if (characterIndex == previousCharacterHit.FirstCharacterIndex + previousCharacterHit.TrailingLength)
            {
                return characterHit;
            }

            return previousCharacterHit;
        }

        private TextBounds GetTextRunBoundsRightToLeft(int firstRunIndex, int lastRunIndex, double endX,
            int firstTextSourceIndex, int currentPosition, int remainingLength, out int coveredLength, out int newPosition)
        {
            coveredLength = 0;
            var textRunBounds = new List<TextRunBounds>();
            var startX = endX;

            for (int i = lastRunIndex; i >= firstRunIndex; i--)
            {
                var currentRun = _textRuns[i];

                if (currentRun is ShapedTextRun shapedTextRun)
                {
                    var runBounds = GetRunBounds(shapedTextRun, startX, firstTextSourceIndex, remainingLength, currentPosition);

                    if (runBounds.TextSourceCharacterIndex < FirstTextSourceIndex + Length)
                    {
                        textRunBounds.Insert(0, runBounds);
                    }

                    if (i == lastRunIndex)
                    {
                        endX = runBounds.Rectangle.Right;

                        startX = endX;
                    }

                    startX -= runBounds.Rectangle.Width;

                    currentPosition = runBounds.TextSourceCharacterIndex + runBounds.Length;

                    coveredLength += runBounds.Length;

                    remainingLength -= runBounds.Length;
                }
                else
                {
                    if (currentPosition < FirstTextSourceIndex + Length)
                    {
                        if (currentRun is DrawableTextRun drawableTextRun)
                        {
                            startX -= drawableTextRun.Size.Width;

                            var runBounds = new TextRunBounds(
                             new Rect(startX, 0, drawableTextRun.Size.Width, Height), currentPosition, currentRun.Length, currentRun);

                            textRunBounds.Insert(0, runBounds);
                        }
                        else
                        {
                            //Add potential TextEndOfParagraph
                            var runBounds = new TextRunBounds(
                              new Rect(endX, 0, 0, Height), currentPosition, currentRun.Length, currentRun);

                            textRunBounds.Add(runBounds);
                        }
                    }

                    currentPosition += currentRun.Length;

                    coveredLength += currentRun.Length;

                    remainingLength -= currentRun.Length;
                }

                if (remainingLength <= 0)
                {
                    break;
                }
            }

            newPosition = currentPosition;

            var runWidth = endX - startX;

            var bounds = new Rect(startX, 0, runWidth, Height);

            return new TextBounds(bounds, FlowDirection.RightToLeft, textRunBounds);
        }

        private TextBounds GetTextBoundsLeftToRight(int firstRunIndex, int lastRunIndex, double startX,
           int firstTextSourceIndex, int currentPosition, int remainingLength, out int coveredLength, out int newPosition)
        {
            coveredLength = 0;
            var textRunBounds = new List<TextRunBounds>(1);
            var endX = startX;

            for (int i = firstRunIndex; i <= lastRunIndex; i++)
            {
                var currentRun = _textRuns[i];

                if (currentRun is ShapedTextRun shapedTextRun)
                {
                    var runBounds = GetRunBounds(shapedTextRun, endX, firstTextSourceIndex, remainingLength, currentPosition);

                    if (runBounds.TextSourceCharacterIndex < FirstTextSourceIndex + Length)
                    {
                        textRunBounds.Add(runBounds);
                    }

                    currentPosition = runBounds.TextSourceCharacterIndex + runBounds.Length;

                    if (i == firstRunIndex)
                    {
                        startX = runBounds.Rectangle.Left;
                    }

                    endX = runBounds.Rectangle.Right;

                    coveredLength += runBounds.Length;

                    remainingLength -= runBounds.Length;
                }
                else
                {
                    if (currentPosition < FirstTextSourceIndex + Length)
                    {
                        if (currentRun is DrawableTextRun drawableTextRun)
                        {
                            var runBounds = new TextRunBounds(
                              new Rect(endX, 0, drawableTextRun.Size.Width, Height), currentPosition, currentRun.Length, currentRun);

                            textRunBounds.Add(runBounds);


                            endX += drawableTextRun.Size.Width;
                        }
                        else
                        {
                            //Add potential TextEndOfParagraph
                            var runBounds = new TextRunBounds(
                               new Rect(endX, 0, 0, Height), currentPosition, currentRun.Length, currentRun);

                            textRunBounds.Add(runBounds);
                        }
                    }

                    currentPosition += currentRun.Length;

                    coveredLength += currentRun.Length;

                    remainingLength -= currentRun.Length;
                }

                if (remainingLength <= 0)
                {
                    break;
                }
            }

            newPosition = currentPosition;

            var runWidth = endX - startX;

            var bounds = new Rect(startX, 0, runWidth, Height);

            return new TextBounds(bounds, FlowDirection.LeftToRight, textRunBounds);
        }

        private TextRunBounds GetRunBounds(ShapedTextRun currentRun, double currentX, int firstTextSourceIndex, int remainingLength, int currentPosition)
        {
            bool isLeftToRight = currentRun.BidiLevel % 2 == 0;

            double startX = currentX;
            double endX = currentX;

            // Determine the start of the first hit in local positions
            var runOffset = Math.Max(0, firstTextSourceIndex - currentPosition);
            var firstCluster = currentRun.GlyphRun.Metrics.FirstCluster;

            //The start index needs to be relative to the first cluster
            var startIndex = firstCluster + runOffset;
            var endIndex = startIndex + remainingLength;

            //Current position is a text source index and first cluster is relative to the GlyphRun's buffer.
            var textSourceOffset = currentPosition - firstCluster;

            Debug.Assert(textSourceOffset >= 0);

            var clusterOffset = 0;

            // Cluster boundary correction
            if (runOffset > 0)
            {
                var characterHit = currentRun.GlyphRun.FindNearestCharacterHit(startIndex, out _);
                var clusterStart = characterHit.FirstCharacterIndex;
                var clusterEnd = clusterStart + characterHit.TrailingLength;

                if (clusterStart < startIndex && clusterEnd > startIndex)
                {
                    //Remember the cluster correction offset
                    clusterOffset = startIndex - clusterStart;

                    //Move to the start of the cluster
                    startIndex -= clusterOffset;
                }
            }

            //Find the visual start and end position of the hit
            var startOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));
            var endOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(endIndex));

            if (isLeftToRight)
            {
                endX = startX + endOffset;
                startX += startOffset;
            }
            else
            {
                //We need the distance from right to left and GetDistanceFromCharacterHit returs a distance from left to right so we need to adjust the offsets
                startX -= currentRun.Size.Width - startOffset;
                endX -= currentRun.Size.Width - endOffset;
            }

            // Find the start of the hit
            var startHit = currentRun.GlyphRun.FindNearestCharacterHit(startIndex, out _);
            var startHitIndex = startHit.FirstCharacterIndex;

            //If the requested text range starts at the trailing edge we need to move at the end of the hit
            if (startHitIndex < startIndex)
            {
                startHitIndex += startHit.TrailingLength;
            }

            //Find the next possible position that contains the endIndex
            var nearestEndHit = currentRun.GlyphRun.FindNearestCharacterHit(endIndex, out _);

            int endHitIndex;

            if (nearestEndHit.FirstCharacterIndex < endIndex)
            {
                //The hit is inside or at the trailing edge
                endHitIndex = nearestEndHit.FirstCharacterIndex + nearestEndHit.TrailingLength;
            }
            else
            {
                //The hit is at the leading edge
                endHitIndex = nearestEndHit.FirstCharacterIndex;
            }

            var coveredLength = Math.Max(0, Math.Abs(startHitIndex - endHitIndex) - clusterOffset);

            // Normalize bounds
            if (endX < startX)
            {
                (endX, startX) = (startX, endX);
            }

            var runWidth = endX - startX;

            //We need to adjust the local position to the text source
            var textSourceIndex = textSourceOffset + startHitIndex + clusterOffset;

            return new TextRunBounds(new Rect(startX, 0, runWidth, Height), textSourceIndex, coveredLength, currentRun);
        }

        public override void Dispose()
        {
            for (int i = 0; i < _textRuns.Length; i++)
            {
                if (_textRuns[i] is ShapedTextRun shapedTextRun)
                {
                    shapedTextRun.Dispose();
                }
            }
        }

        public void FinalizeLine()
        {
            _indexedTextRuns = BidiReorderer.Instance.BidiReorder(_textRuns, _paragraphProperties.FlowDirection, FirstTextSourceIndex);

            _textLineMetrics = CreateLineMetrics();

            if (_textLineBreak is null && _textRuns.Length > 1 && _textRuns[_textRuns.Length - 1] is TextEndOfLine textEndOfLine)
            {
                _textLineBreak = new TextLineBreak(textEndOfLine);
            }
        }

        /// <summary>
        /// Gets the run index of the specified codepoint index.
        /// </summary>
        /// <param name="codepointIndex">The codepoint index.</param>
        /// <param name="direction">The logical direction.</param>
        /// <param name="textPosition">The text position of the found run index.</param>
        /// <returns>The text run index.</returns>
        private TextRun? GetRunAtCharacterIndex(int codepointIndex, LogicalDirection direction, out int textPosition)
        {
            var runIndex = 0;
            textPosition = FirstTextSourceIndex;

            if (_indexedTextRuns is null)
            {
                return null;
            }

            TextRun? currentRun = null;

            while (runIndex < _indexedTextRuns.Count)
            {
                var indexedRun = _indexedTextRuns[runIndex];
                currentRun = indexedRun.TextRun;

                switch (currentRun)
                {
                    case ShapedTextRun shapedRun:
                        {
                            var firstCluster = shapedRun.GlyphRun.Metrics.FirstCluster;

                            firstCluster += Math.Max(0, indexedRun.TextSourceCharacterIndex - firstCluster);

                            if (direction == LogicalDirection.Forward)
                            {
                                if (codepointIndex >= firstCluster && codepointIndex < firstCluster + currentRun.Length)
                                {
                                    return currentRun;
                                }
                            }
                            else
                            {
                                if (codepointIndex > firstCluster && codepointIndex <= firstCluster + currentRun.Length)
                                {
                                    return currentRun;
                                }
                            }

                            if (runIndex + 1 >= _textRuns.Length)
                            {
                                return currentRun;
                            }

                            textPosition += currentRun.Length;

                            break;
                        }
                    case not null:
                        {
                            if (direction == LogicalDirection.Forward)
                            {
                                if (textPosition == codepointIndex)
                                {
                                    return currentRun;
                                }
                            }
                            else
                            {
                                if (textPosition + currentRun.Length == codepointIndex)
                                {
                                    return currentRun;
                                }
                            }

                            if (runIndex + 1 >= _textRuns.Length)
                            {
                                return currentRun;
                            }

                            textPosition += currentRun.Length;

                            break;
                        }

                }

                runIndex++;
            }

            return currentRun;
        }

        private TextLineMetrics CreateLineMetrics()
        {
            var fontMetrics = _paragraphProperties.DefaultTextRunProperties.CachedGlyphTypeface.Metrics;
            var fontRenderingEmSize = _paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize;
            var scale = fontRenderingEmSize / fontMetrics.DesignEmHeight;
            var widthIncludingWhitespace = 0d;
            var trailingWhitespaceLength = 0;
            var newLineLength = 0;
            var ascent = fontMetrics.Ascent * scale;
            var descent = fontMetrics.Descent * scale;
            var lineGap = fontMetrics.LineGap * scale;

            var lineHeight = _paragraphProperties.LineHeight;
            var lineSpacing = _paragraphProperties.LineSpacing;

            for (var index = 0; index < _textRuns.Length; index++)
            {
                switch (_textRuns[index])
                {
                    case ShapedTextRun textRun:
                        {
                            var textMetrics = textRun.TextMetrics;

                            if (ascent > textMetrics.Ascent)
                            {
                                ascent = textMetrics.Ascent;
                            }

                            if (descent < textMetrics.Descent)
                            {
                                descent = textMetrics.Descent;
                            }

                            if (lineGap < textMetrics.LineGap)
                            {
                                lineGap = textMetrics.LineGap;
                            }

                            break;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            if (drawableTextRun.Baseline > -ascent)
                            {
                                ascent = -drawableTextRun.Baseline;
                            }

                            var bottom = drawableTextRun.Size.Height - drawableTextRun.Baseline;

                            if (bottom > descent)
                            {
                                descent = bottom;
                            }

                            break;
                        }
                }
            }

            var inkBounds = new Rect();

            for (var index = 0; index < _textRuns.Length; index++)
            {
                switch (_textRuns[index])
                {
                    case ShapedTextRun textRun:
                        {
                            var glyphRun = textRun.GlyphRun;
                            //Align the ink bounds at the common baseline
                            var offsetY = -ascent - textRun.Baseline;

                            var runBounds = glyphRun.InkBounds.Translate(new Vector(widthIncludingWhitespace, offsetY));

                            inkBounds = inkBounds.Union(runBounds);

                            widthIncludingWhitespace += textRun.Size.Width;

                            break;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            //Align the bounds at the common baseline
                            var offsetY = -ascent - drawableTextRun.Baseline;

                            inkBounds = inkBounds.Union(new Rect(new Point(widthIncludingWhitespace, offsetY), drawableTextRun.Size));

                            widthIncludingWhitespace += drawableTextRun.Size.Width;

                            break;
                        }
                }
            }

            var halfLineGap = lineGap * 0.5;
            var naturalHeight = descent - ascent + lineGap;
            var baseline = -ascent + halfLineGap;
            var height = naturalHeight;

            if (!double.IsNaN(lineHeight) && !MathUtilities.IsZero(lineHeight))
            {
                if (lineHeight <= naturalHeight)
                {
                    //Clamp to the specified line height
                    height = lineHeight;
                    baseline = -ascent;
                }
                else
                {
                    // Center the text vertically within the specified line height
                    height = lineHeight;
                    var extra = lineHeight - (descent - ascent);
                    baseline = -ascent + extra / 2;
                }
            }

            height += lineSpacing;

            var width = widthIncludingWhitespace;

            var isRtl = _paragraphProperties.FlowDirection == FlowDirection.RightToLeft;

            for (int i = 0; i < _textRuns.Length; i++)
            {
                var index = isRtl ? i : _textRuns.Length - 1 - i;
                var currentRun = _textRuns[index];

                if (currentRun is ShapedTextRun shapedText)
                {
                    var glyphRun = shapedText.GlyphRun;
                    var glyphRunMetrics = glyphRun.Metrics;

                    newLineLength += glyphRunMetrics.NewLineLength;

                    if (glyphRunMetrics.TrailingWhitespaceLength == 0)
                    {
                        break;
                    }

                    trailingWhitespaceLength += glyphRunMetrics.TrailingWhitespaceLength;

                    var whitespaceWidth = glyphRun.Bounds.Width - glyphRunMetrics.Width;

                    width -= whitespaceWidth;
                }
            }

            var extent = inkBounds.Height;
            //The height of overhanging pixels at the bottom
            var overhangAfter = inkBounds.Bottom - height + halfLineGap;
            //The width of overhanging pixels at the natural alignment point. Positive value means we are inside.
            var overhangLeading = inkBounds.Left;
            //The width of overhanging pixels at the end of the natural bounds. Positive value means we are inside.
            var overhangTrailing = widthIncludingWhitespace - inkBounds.Right;
            var hasOverflowed = width > _paragraphWidth;

            var start = GetParagraphOffsetX(width, widthIncludingWhitespace);

            _inkBounds = inkBounds.Translate(new Vector(start, 0));

            _bounds = new Rect(start, 0, widthIncludingWhitespace, height);

            return new TextLineMetrics
            {
                HasOverflowed = hasOverflowed,
                Height = height,
                Extent = extent,
                NewlineLength = newLineLength,
                Start = start,
                TextBaseline = baseline,
                TrailingWhitespaceLength = trailingWhitespaceLength,
                Width = width,
                WidthIncludingTrailingWhitespace = widthIncludingWhitespace,
                OverhangLeading = overhangLeading,
                OverhangTrailing = overhangTrailing,
                OverhangAfter = overhangAfter
            };
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

            if (textAlignment == TextAlignment.Justify)
            {
                textAlignment = TextAlignment.Start;
            }

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
                    var start = (_paragraphWidth - width) / 2;

                    if (paragraphFlowDirection == FlowDirection.RightToLeft)
                    {
                        start -= (widthIncludingTrailingWhitespace - width);
                    }

                    return Math.Max(0, start);
                case TextAlignment.Right:
                    return Math.Max(0, _paragraphWidth - widthIncludingTrailingWhitespace);
                default:
                    return 0;
            }
        }
    }
}
