using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        private IReadOnlyList<TextRun> _textRuns;
        private readonly double _paragraphWidth;
        private readonly TextParagraphProperties _paragraphProperties;
        private TextLineMetrics _textLineMetrics;
        private readonly FlowDirection _resolvedFlowDirection;

        public TextLineImpl(IReadOnlyList<TextRun> textRuns, int firstTextSourceIndex, int length, double paragraphWidth,
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
        public override int NewLineLength => _textLineMetrics.NewlineLength;

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
                if (textRun is DrawableTextRun drawable)
                {
                    var offsetY = GetBaselineOffset(this, drawable);

                    drawable.Draw(drawingContext, new Point(currentX, currentY + offsetY));

                    currentX += drawable.Size.Width;
                }
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
                var firstRun = _textRuns[0];

                return GetRunCharacterHit(firstRun, FirstTextSourceIndex, 0);
            }

            if (distance >= WidthIncludingTrailingWhitespace)
            {
                var lastRun = _textRuns[_textRuns.Count - 1];

                var size = 0.0;

                if (lastRun is DrawableTextRun drawableTextRun)
                {
                    size = drawableTextRun.Size.Width;
                }

                return GetRunCharacterHit(lastRun, FirstTextSourceIndex + Length - lastRun.Length, size);
            }

            // process hit that happens within the line
            var characterHit = new CharacterHit();
            var currentPosition = FirstTextSourceIndex;
            var currentDistance = 0.0;

            for (var i = 0; i < _textRuns.Count; i++)
            {
                var currentRun = _textRuns[i];

                if (currentRun is ShapedTextRun shapedRun && !shapedRun.ShapedBuffer.IsLeftToRight)
                {
                    var rightToLeftIndex = i;
                    currentPosition += currentRun.Length;

                    while (rightToLeftIndex + 1 <= _textRuns.Count - 1)
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
                        if (j > _textRuns.Count - 1)
                        {
                            break;
                        }

                        currentRun = _textRuns[j];

                        if(currentRun is not ShapedTextRun)
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
                    if (i < _textRuns.Count - 1 && currentDistance + drawableTextRun.Size.Width < distance)
                    {
                        currentDistance += drawableTextRun.Size.Width;

                        currentPosition += currentRun.Length;

                        continue;
                    }
                }
                else
                {
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
            var flowDirection = _paragraphProperties.FlowDirection;
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
            var currentPosition = FirstTextSourceIndex;
            var remainingLength = characterIndex - FirstTextSourceIndex;

            var currentDistance = Start;

            if (flowDirection == FlowDirection.LeftToRight)
            {
                for (var index = 0; index < _textRuns.Count; index++)
                {
                    var currentRun = _textRuns[index];

                    if (currentRun is ShapedTextRun shapedRun && !shapedRun.ShapedBuffer.IsLeftToRight)
                    {
                        var i = index;

                        var rightToLeftWidth = shapedRun.Size.Width;

                        while (i + 1 <= _textRuns.Count - 1)
                        {
                            var nextRun = _textRuns[i + 1];

                            if (nextRun is ShapedTextRun nextShapedRun && !nextShapedRun.ShapedBuffer.IsLeftToRight)
                            {
                                i++;

                                rightToLeftWidth += nextShapedRun.Size.Width;

                                continue;
                            }

                            break;
                        }

                        if (i > index)
                        {
                            while (i >= index)
                            {
                                currentRun = _textRuns[i];

                                if (currentRun is DrawableTextRun drawable)
                                {
                                    rightToLeftWidth -= drawable.Size.Width;
                                }

                                if (currentPosition + currentRun.Length >= characterIndex)
                                {
                                    break;
                                }

                                currentPosition += currentRun.Length;

                                remainingLength -= currentRun.Length;

                                i--;
                            }

                            currentDistance += rightToLeftWidth;
                        }
                    }

                    if (currentPosition + currentRun.Length >= characterIndex &&
                        TryGetDistanceFromCharacterHit(currentRun, characterHit, currentPosition, remainingLength, flowDirection, out var distance, out _))
                    {
                        return Math.Max(0, currentDistance + distance);
                    }

                    if (currentRun is DrawableTextRun drawableTextRun)
                    {
                        currentDistance += drawableTextRun.Size.Width;
                    }

                    //No hit hit found so we add the full width

                    currentPosition += currentRun.Length;
                    remainingLength -= currentRun.Length;
                }
            }
            else
            {
                currentDistance += WidthIncludingTrailingWhitespace;

                for (var index = _textRuns.Count - 1; index >= 0; index--)
                {
                    var currentRun = _textRuns[index];

                    if (TryGetDistanceFromCharacterHit(currentRun, characterHit, currentPosition, remainingLength,
                        flowDirection, out var distance, out var currentGlyphRun))
                    {
                        if (currentGlyphRun != null)
                        {
                            distance = currentGlyphRun.Size.Width - distance;
                        }

                        return Math.Max(0, currentDistance - distance);
                    }

                    if (currentRun is DrawableTextRun drawableTextRun)
                    {
                        currentDistance -= drawableTextRun.Size.Width;
                    }

                    //No hit hit found so we add the full width
                    currentPosition += currentRun.Length;
                    remainingLength -= currentRun.Length;
                }
            }

            return Math.Max(0, currentDistance);
        }

        private static bool TryGetDistanceFromCharacterHit(
            TextRun currentRun,
            CharacterHit characterHit,
            int currentPosition,
            int remainingLength,
            FlowDirection flowDirection,
            out double distance,
            out GlyphRun? currentGlyphRun)
        {
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;
            var isTrailingHit = characterHit.TrailingLength > 0;

            distance = 0;
            currentGlyphRun = null;

            switch (currentRun)
            {
                case ShapedTextRun shapedTextCharacters:
                    {
                        currentGlyphRun = shapedTextCharacters.GlyphRun;

                        if (currentPosition + remainingLength <= currentPosition + currentRun.Length)
                        {
                            characterHit = new CharacterHit(currentPosition + remainingLength);

                            distance = currentGlyphRun.GetDistanceFromCharacterHit(characterHit);

                            return true;
                        }

                        if (currentPosition + remainingLength == currentPosition + currentRun.Length && isTrailingHit)
                        {
                            if (currentGlyphRun.IsLeftToRight || flowDirection == FlowDirection.RightToLeft)
                            {
                                distance = currentGlyphRun.Size.Width;
                            }

                            return true;
                        }

                        break;
                    }
                case DrawableTextRun drawableTextRun:
                    {
                        if (characterIndex == currentPosition)
                        {
                            return true;
                        }

                        if (characterIndex == currentPosition + currentRun.Length)
                        {
                            distance = drawableTextRun.Size.Width;

                            return true;

                        }

                        break;
                    }
                default:
                    {
                        return false;
                    }
            }

            return false;
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
                case ShapedTextRun shapedRun:
                    {
                        nextCharacterHit = shapedRun.GlyphRun.GetNextCaretCharacterHit(characterHit);
                        break;
                    }
                default:
                    {
                        nextCharacterHit = new CharacterHit(currentPosition + currentRun.Length);
                        break;
                    }
            }

            if (characterHit.FirstCharacterIndex + characterHit.TrailingLength == nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength)
            {
                return characterHit;
            }

            return nextCharacterHit;
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
            var currentRect = default(Rect);

            TextRunBounds lastRunBounds = default;

            for (var index = 0; index < TextRuns.Count; index++)
            {
                if (TextRuns[index] is not DrawableTextRun currentRun)
                {
                    continue;
                }

                var characterLength = 0;
                var endX = startX;

                TextRunBounds currentRunBounds;

                double combinedWidth;

                if (currentRun is ShapedTextRun currentShapedRun)
                {
                    var firstCluster = currentShapedRun.GlyphRun.Metrics.FirstCluster;

                    if (currentPosition + currentRun.Length <= firstTextSourceIndex)
                    {
                        startX += currentRun.Size.Width;

                        currentPosition += currentRun.Length;

                        continue;
                    }

                    if (currentShapedRun.ShapedBuffer.IsLeftToRight)
                    {
                        var startIndex = firstCluster + Math.Max(0, firstTextSourceIndex - currentPosition);

                        double startOffset;

                        double endOffset;

                        startOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));

                        endOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex + remainingLength));

                        startX += startOffset;

                        endX += endOffset;

                        var endHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(endOffset, out _);

                        var startHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(startOffset, out _);

                        characterLength = Math.Abs(endHit.FirstCharacterIndex + endHit.TrailingLength - startHit.FirstCharacterIndex - startHit.TrailingLength);

                        currentDirection = FlowDirection.LeftToRight;
                    }
                    else
                    {
                        var rightToLeftIndex = index;
                        var rightToLeftWidth = currentShapedRun.Size.Width;

                        while (rightToLeftIndex + 1 <= _textRuns.Count - 1 && _textRuns[rightToLeftIndex + 1] is ShapedTextRun nextShapedRun)
                        {
                            if (nextShapedRun == null || nextShapedRun.ShapedBuffer.IsLeftToRight)
                            {
                                break;
                            }

                            rightToLeftIndex++;

                            rightToLeftWidth += nextShapedRun.Size.Width;

                            if (currentPosition + nextShapedRun.Length > firstTextSourceIndex + textLength)
                            {
                                break;
                            }

                            currentShapedRun = nextShapedRun;
                        }

                        startX += rightToLeftWidth;

                        currentRunBounds = GetRightToLeftTextRunBounds(currentShapedRun, startX, firstTextSourceIndex, characterIndex, currentPosition, remainingLength);

                        remainingLength -= currentRunBounds.Length;
                        currentPosition = currentRunBounds.TextSourceCharacterIndex + currentRunBounds.Length;
                        endX = currentRunBounds.Rectangle.Right;
                        startX = currentRunBounds.Rectangle.Left;

                        var rightToLeftRunBounds = new List<TextRunBounds> { currentRunBounds };

                        for (int i = rightToLeftIndex - 1; i >= index; i--)
                        {
                            if (TextRuns[i] is not ShapedTextRun)
                            {
                                continue;
                            }

                            currentShapedRun = (ShapedTextRun)TextRuns[i];

                            currentRunBounds = GetRightToLeftTextRunBounds(currentShapedRun, startX, firstTextSourceIndex, characterIndex, currentPosition, remainingLength);

                            rightToLeftRunBounds.Insert(0, currentRunBounds);

                            remainingLength -= currentRunBounds.Length;
                            startX = currentRunBounds.Rectangle.Left;

                            currentPosition += currentRunBounds.Length;
                        }

                        combinedWidth = endX - startX;

                        currentRect = new Rect(startX, 0, combinedWidth, Height);

                        currentDirection = FlowDirection.RightToLeft;

                        if (!MathUtilities.IsZero(combinedWidth))
                        {
                            result.Add(new TextBounds(currentRect, currentDirection, rightToLeftRunBounds));
                        }

                        startX = endX;
                    }
                }
                else
                {
                    if (currentPosition + currentRun.Length <= firstTextSourceIndex)
                    {
                        startX += currentRun.Size.Width;

                        currentPosition += currentRun.Length;

                        continue;
                    }

                    if (currentPosition < firstTextSourceIndex)
                    {
                        startX += currentRun.Size.Width;
                    }

                    if (currentPosition + currentRun.Length <= characterIndex)
                    {
                        endX += currentRun.Size.Width;

                        characterLength = currentRun.Length;
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

                combinedWidth = endX - startX;

                currentRunBounds = new TextRunBounds(new Rect(startX, 0, combinedWidth, Height), currentPosition, characterLength, currentRun);

                currentPosition += characterLength;

                remainingLength -= characterLength;

                startX = endX;

                if (currentRunBounds.TextRun != null && !MathUtilities.IsZero(combinedWidth) || NewLineLength > 0)
                {
                    if (result.Count > 0 && lastDirection == currentDirection && MathUtilities.AreClose(currentRect.Left, lastRunBounds.Rectangle.Right))
                    {
                        currentRect = currentRect.WithWidth(currentWidth + combinedWidth);

                        var textBounds = result[result.Count - 1];

                        textBounds.Rectangle = currentRect;

                        textBounds.TextRunBounds.Add(currentRunBounds);
                    }
                    else
                    {
                        currentRect = currentRunBounds.Rectangle;

                        result.Add(new TextBounds(currentRect, currentDirection, new List<TextRunBounds> { currentRunBounds }));
                    }
                }

                lastRunBounds = currentRunBounds;

                currentWidth += combinedWidth;

                if (remainingLength <= 0 || currentPosition >= characterIndex)
                {
                    break;
                }

                lastDirection = currentDirection;
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

            var startX = WidthIncludingTrailingWhitespace;
            double currentWidth = 0;
            var currentRect = default(Rect);

            for (var index = TextRuns.Count - 1; index >= 0; index--)
            {
                if (TextRuns[index] is not DrawableTextRun currentRun)
                {
                    continue;
                }

                if (currentPosition + currentRun.Length < firstTextSourceIndex)
                {
                    startX -= currentRun.Size.Width;

                    currentPosition += currentRun.Length;

                    continue;
                }

                var characterLength = 0;
                var endX = startX;

                if (currentRun is ShapedTextRun currentShapedRun)
                {
                    var offset = Math.Max(0, firstTextSourceIndex - currentPosition);

                    currentPosition += offset;

                    var startIndex = currentPosition;
                    double startOffset;
                    double endOffset;

                    if (currentShapedRun.ShapedBuffer.IsLeftToRight)
                    {
                        if (currentPosition < startIndex)
                        {
                            startOffset = endOffset = 0;
                        }
                        else
                        {
                            endOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex + remainingLength));

                            startOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));
                        }
                    }
                    else
                    {
                        endOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));

                        startOffset = currentShapedRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex + remainingLength));
                    }

                    startX -= currentRun.Size.Width - startOffset;
                    endX -= currentRun.Size.Width - endOffset;

                    var endHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(endOffset, out _);
                    var startHit = currentShapedRun.GlyphRun.GetCharacterHitFromDistance(startOffset, out _);

                    characterLength = Math.Abs(startHit.FirstCharacterIndex + startHit.TrailingLength - endHit.FirstCharacterIndex - endHit.TrailingLength);

                    currentDirection = currentShapedRun.ShapedBuffer.IsLeftToRight ?
                        FlowDirection.LeftToRight :
                        FlowDirection.RightToLeft;
                }
                else
                {
                    if (currentPosition + currentRun.Length <= characterIndex)
                    {
                        endX -= currentRun.Size.Width;
                    }

                    if (currentPosition < firstTextSourceIndex)
                    {
                        startX -= currentRun.Size.Width;

                        characterLength = currentRun.Length;
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

                var currentRunBounds = new TextRunBounds(new Rect(Start + startX, 0, runWidth, Height), currentPosition, characterLength, currentRun);

                if (!MathUtilities.IsZero(runWidth) || NewLineLength > 0)
                {
                    if (lastDirection == currentDirection && result.Count > 0 && MathUtilities.AreClose(currentRect.Right, Start + startX))
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
                }

                currentWidth += runWidth;
                currentPosition += characterLength;

                if (currentPosition > characterIndex)
                {
                    break;
                }

                lastDirection = currentDirection;
                remainingLength -= characterLength;

                if (remainingLength <= 0)
                {
                    break;
                }
            }

            result.Reverse();

            return result;
        }

        private TextRunBounds GetRightToLeftTextRunBounds(ShapedTextRun currentRun, double endX, int firstTextSourceIndex, int characterIndex, int currentPosition, int remainingLength)
        {
            var startX = endX;

            var offset = Math.Max(0, firstTextSourceIndex - currentPosition);

            currentPosition += offset;

            var startIndex = currentPosition;

            double startOffset;
            double endOffset;

            endOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));

            startOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex + remainingLength));

            startX -= currentRun.Size.Width - startOffset;
            endX -= currentRun.Size.Width - endOffset;

            var endHit = currentRun.GlyphRun.GetCharacterHitFromDistance(endOffset, out _);
            var startHit = currentRun.GlyphRun.GetCharacterHitFromDistance(startOffset, out _);

            var characterLength = Math.Abs(startHit.FirstCharacterIndex + startHit.TrailingLength - endHit.FirstCharacterIndex - endHit.TrailingLength);

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

            return new TextRunBounds(new Rect(Start + startX, 0, runWidth, Height), currentPosition, characterLength, currentRun);
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

        private static sbyte GetRunBidiLevel(TextRun run, FlowDirection flowDirection)
        {
            if (run is ShapedTextRun shapedTextCharacters)
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
                        if (current.Run is ShapedTextRun { IsReversed: false } shapedTextCharacters)
                        {
                            shapedTextCharacters.Reverse();
                        }
                    }

                    current = current.Next;
                }

                minLevelToReverse--;
            }

            var textRuns = new List<TextRun>(_textRuns.Count);

            current = orderedRun;

            while (current != null)
            {
                textRuns.Add(current.Run);

                current = current.Next;
            }

            _textRuns = textRuns;
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
                    case ShapedTextRun shapedRun:
                        {
                            var foundCharacterHit = shapedRun.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

                            var isAtEnd = foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength == FirstTextSourceIndex + Length;

                            if (isAtEnd && !shapedRun.GlyphRun.IsLeftToRight)
                            {
                                nextCharacterHit = foundCharacterHit;

                                return true;
                            }

                            //var characterIndex = codepointIndex - shapedRun.Text.Start;

                            //if (characterIndex < 0 && shapedRun.ShapedBuffer.IsLeftToRight)
                            //{
                            //    foundCharacterHit = new CharacterHit(foundCharacterHit.FirstCharacterIndex);
                            //}

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
                                nextCharacterHit = new CharacterHit(currentPosition + currentRun.Length);

                                return true;
                            }

                            break;
                        }
                }

                currentPosition += currentRun.Length;
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
                    case ShapedTextRun shapedRun:
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
                            if (characterIndex == currentPosition + currentRun.Length)
                            {
                                previousCharacterHit = new CharacterHit(currentPosition);

                                return true;
                            }

                            break;
                        }
                }

                currentPosition -= currentRun.Length;
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
            TextRun? previousRun = null;

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextRun shapedRun:
                        {
                            var firstCluster = shapedRun.GlyphRun.Metrics.FirstCluster;

                            if (firstCluster > codepointIndex)
                            {
                                break;
                            }

                            if (previousRun is ShapedTextRun previousShaped && !previousShaped.ShapedBuffer.IsLeftToRight)
                            {
                                if (shapedRun.ShapedBuffer.IsLeftToRight)
                                {
                                    if (firstCluster >= codepointIndex)
                                    {
                                        return --runIndex;
                                    }
                                }
                                else
                                {
                                    if (codepointIndex > firstCluster + currentRun.Length)
                                    {
                                        return --runIndex;
                                    }
                                }
                            }

                            if (direction == LogicalDirection.Forward)
                            {
                                if (codepointIndex >= firstCluster && codepointIndex <= firstCluster + currentRun.Length)
                                {
                                    return runIndex;
                                }
                            }
                            else
                            {
                                if (codepointIndex > firstCluster &&
                                    codepointIndex <= firstCluster + currentRun.Length)
                                {
                                    return runIndex;
                                }
                            }

                            if (runIndex + 1 >= _textRuns.Count)
                            {
                                return runIndex;
                            }

                            textPosition += currentRun.Length;

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

                            textPosition += currentRun.Length;

                            break;
                        }

                }

                runIndex++;
                previousRun = currentRun;
            }

            return runIndex;
        }

        private TextLineMetrics CreateLineMetrics()
        {
            var fontMetrics = _paragraphProperties.DefaultTextRunProperties.Typeface.GlyphTypeface.Metrics;
            var fontRenderingEmSize = _paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize;
            var scale = fontRenderingEmSize / fontMetrics.DesignEmHeight;

            var width = 0d;
            var widthIncludingWhitespace = 0d;
            var trailingWhitespaceLength = 0;
            var newLineLength = 0;
            var ascent = fontMetrics.Ascent * scale;
            var descent = fontMetrics.Descent * scale;
            var lineGap = fontMetrics.LineGap * scale;

            var height = descent - ascent + lineGap;

            var lineHeight = _paragraphProperties.LineHeight;

            var lastRunIndex = _textRuns.Count - 1;

            if (_textRuns[lastRunIndex] is TextEndOfLine && lastRunIndex > 0)
            {
                lastRunIndex--;
            }

            for (var index = 0; index < _textRuns.Count; index++)
            {
                switch (_textRuns[index])
                {
                    case ShapedTextRun textRun:
                        {
                            var textMetrics =
                                new TextMetrics(textRun.Properties.Typeface.GlyphTypeface, textRun.Properties.FontRenderingEmSize);

                            if (fontRenderingEmSize < textRun.Properties.FontRenderingEmSize)
                            {
                                fontRenderingEmSize = textRun.Properties.FontRenderingEmSize;

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

                                if (descent - ascent + lineGap > height)
                                {
                                    height = descent - ascent + lineGap;
                                }
                            }

                            if (index == lastRunIndex)
                            {
                                width = widthIncludingWhitespace + textRun.GlyphRun.Metrics.Width;
                                trailingWhitespaceLength = textRun.GlyphRun.Metrics.TrailingWhitespaceLength;
                                newLineLength = textRun.GlyphRun.Metrics.NewLineLength;
                            }

                            widthIncludingWhitespace += textRun.GlyphRun.Metrics.WidthIncludingTrailingWhitespace;

                            break;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            widthIncludingWhitespace += drawableTextRun.Size.Width;

                            switch (_paragraphProperties.FlowDirection)
                            {
                                case FlowDirection.LeftToRight:
                                    {
                                        if (index == lastRunIndex)
                                        {
                                            width = widthIncludingWhitespace;
                                            trailingWhitespaceLength = 0;
                                            newLineLength = 0;
                                        }

                                        break;
                                    }

                                case FlowDirection.RightToLeft:
                                    {
                                        if (index == lastRunIndex)
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

        private sealed class OrderedBidiRun
        {
            public OrderedBidiRun(TextRun run, sbyte level)
            {
                Run = run;
                Level = level;
            }

            public sbyte Level { get; }

            public TextRun Run { get; }

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
