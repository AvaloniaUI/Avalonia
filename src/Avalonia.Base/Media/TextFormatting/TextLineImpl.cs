using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal sealed class TextLineImpl : TextLine
    {
        private readonly TextRun[] _textRuns;
        private readonly double _paragraphWidth;
        private readonly TextParagraphProperties _paragraphProperties;
        private TextLineMetrics _textLineMetrics;
        private TextLineBreak? _textLineBreak;
        private readonly FlowDirection _resolvedFlowDirection;

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

            if (_textRuns[lastIndex] is TextEndOfLine)
            {
                lastIndex--;
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
                    currentPosition = Length - firstRun.Length;
                }

                return GetRunCharacterHit(firstRun, currentPosition, 0);
            }

            if (distance >= WidthIncludingTrailingWhitespace)
            {
                var lastRun = _textRuns[lastIndex];

                if (_paragraphProperties.FlowDirection == FlowDirection.LeftToRight)
                {
                    currentPosition = Length - lastRun.Length;
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
                for (var index = 0; index < _textRuns.Length; index++)
                {
                    var currentRun = _textRuns[index];

                    if (currentRun is ShapedTextRun shapedRun && !shapedRun.ShapedBuffer.IsLeftToRight)
                    {
                        var i = index;

                        var rightToLeftWidth = shapedRun.Size.Width;

                        while (i + 1 <= _textRuns.Length - 1)
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

                for (var index = _textRuns.Length - 1; index >= 0; index--)
                {
                    var currentRun = _textRuns[index];

                    if (TryGetDistanceFromCharacterHit(currentRun, characterHit, currentPosition, remainingLength,
                        flowDirection, out var distance, out var currentGlyphRun))
                    {
                        if (currentGlyphRun != null)
                        {
                            currentDistance -= currentGlyphRun.Size.Width;
                        }

                        return currentDistance + distance;
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
            if (_textRuns.Length == 0)
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

        public override IReadOnlyList<TextBounds> GetTextBounds(int firstTextSourceIndex, int textLength)
        {
            if (_textRuns.Length == 0)
            {
                return Array.Empty<TextBounds>();
            }

            var result = new List<TextBounds>();

            var currentPosition = FirstTextSourceIndex;
            var remainingLength = textLength;

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

            if (_paragraphProperties.FlowDirection == FlowDirection.LeftToRight)
            {
                var currentX = Start;

                for (int i = 0; i < _textRuns.Length; i++)
                {
                    var currentRun = _textRuns[i];

                    var firstRunIndex = i;
                    var lastRunIndex = firstRunIndex;
                    var currentDirection = GetDirection(currentRun, FlowDirection.LeftToRight);
                    var directionalWidth = 0.0;

                    if (currentRun is DrawableTextRun currentDrawable)
                    {
                        directionalWidth = currentDrawable.Size.Width;
                    }

                    // Find consecutive runs of same direction
                    for (; lastRunIndex + 1 < _textRuns.Length; lastRunIndex++)
                    {
                        var nextRun = _textRuns[lastRunIndex + 1];

                        var nextDirection = GetDirection(nextRun, currentDirection);

                        if (currentDirection != nextDirection)
                        {
                            break;
                        }

                        if (nextRun is DrawableTextRun nextDrawable)
                        {
                            directionalWidth += nextDrawable.Size.Width;
                        }
                    }

                    //Skip runs that are not part of the hit test range
                    switch (currentDirection)
                    {
                        case FlowDirection.RightToLeft:
                            {
                                for (; lastRunIndex >= firstRunIndex; lastRunIndex--)
                                {
                                    currentRun = _textRuns[lastRunIndex];

                                    if (currentPosition + currentRun.Length > firstTextSourceIndex)
                                    {
                                        break;
                                    }

                                    currentPosition += currentRun.Length;

                                    if (currentRun is DrawableTextRun drawableTextRun)
                                    {
                                        directionalWidth -= drawableTextRun.Size.Width;
                                        currentX += drawableTextRun.Size.Width;
                                    }

                                    if(lastRunIndex - 1 < 0)
                                    {
                                        break;
                                    }
                                }

                                break;
                            }
                        default:
                            {
                                for (; firstRunIndex <= lastRunIndex; firstRunIndex++)
                                {
                                    currentRun = _textRuns[firstRunIndex];

                                    if (currentPosition + currentRun.Length > firstTextSourceIndex)
                                    {
                                        break;
                                    }

                                    currentPosition += currentRun.Length;

                                    if (currentRun is DrawableTextRun drawableTextRun)
                                    {
                                        currentX += drawableTextRun.Size.Width;
                                        directionalWidth -= drawableTextRun.Size.Width;
                                    }

                                    if(firstRunIndex + 1 == _textRuns.Length)
                                    {
                                        break;
                                    }
                                }

                                break;
                            }
                    }

                    i = lastRunIndex;

                    if (directionalWidth == 0)
                    {
                        continue;
                    }

                    var coveredLength = 0;
                    TextBounds? textBounds = null;

                    switch (currentDirection)
                    {

                        case FlowDirection.RightToLeft:
                            {
                                textBounds = GetTextRunBoundsRightToLeft(firstRunIndex, lastRunIndex, currentX + directionalWidth, firstTextSourceIndex,
                                        currentPosition, remainingLength, out coveredLength, out currentPosition);

                                currentX += directionalWidth;

                                break;
                            }
                        default:
                            {
                                textBounds = GetTextBoundsLeftToRight(firstRunIndex, lastRunIndex, currentX, firstTextSourceIndex,
                                        currentPosition, remainingLength, out coveredLength, out currentPosition);

                                currentX = textBounds.Rectangle.Right;

                                break;
                            }
                    }

                    if (coveredLength > 0)
                    {
                        result.Add(textBounds);

                        remainingLength -= coveredLength;
                    }

                    if (remainingLength <= 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                var currentX = Start + WidthIncludingTrailingWhitespace;

                for (int i = _textRuns.Length - 1; i >= 0; i--)
                {
                    var currentRun = _textRuns[i];
                    var firstRunIndex = i;
                    var lastRunIndex = firstRunIndex;
                    var currentDirection = GetDirection(currentRun, FlowDirection.RightToLeft);
                    var directionalWidth = 0.0;

                    if (currentRun is DrawableTextRun currentDrawable)
                    {
                        directionalWidth = currentDrawable.Size.Width;
                    }

                    // Find consecutive runs of same direction
                    for (; firstRunIndex - 1 > 0; firstRunIndex--)
                    {
                        var previousRun = _textRuns[firstRunIndex - 1];

                        var previousDirection = GetDirection(previousRun, currentDirection);

                        if (currentDirection != previousDirection)
                        {
                            break;
                        }

                        if (currentRun is DrawableTextRun previousDrawable)
                        {
                            directionalWidth += previousDrawable.Size.Width;
                        }
                    }

                    //Skip runs that are not part of the hit test range
                    switch (currentDirection)
                    {
                        case FlowDirection.RightToLeft:
                            {
                                for (; lastRunIndex >= firstRunIndex; lastRunIndex--)
                                {
                                    currentRun = _textRuns[lastRunIndex];

                                    if (currentPosition + currentRun.Length <= firstTextSourceIndex)
                                    {
                                        currentPosition += currentRun.Length;

                                        if (currentRun is DrawableTextRun drawableTextRun)
                                        {
                                            currentX -= drawableTextRun.Size.Width;
                                            directionalWidth -= drawableTextRun.Size.Width;
                                        }

                                        continue;
                                    }

                                    break;
                                }

                                break;
                            }
                        default:
                            {
                                for (; firstRunIndex <= lastRunIndex; firstRunIndex++)
                                {
                                    currentRun = _textRuns[firstRunIndex];

                                    if (currentPosition + currentRun.Length <= firstTextSourceIndex)
                                    {
                                        currentPosition += currentRun.Length;

                                        if (currentRun is DrawableTextRun drawableTextRun)
                                        {
                                            currentX += drawableTextRun.Size.Width;
                                            directionalWidth -= drawableTextRun.Size.Width;
                                        }

                                        continue;
                                    }

                                    break;
                                }

                                break;
                            }
                    }

                    i = firstRunIndex;

                    if (directionalWidth == 0)
                    {
                        continue;
                    }

                    var coveredLength = 0;

                    TextBounds? textBounds = null;

                    switch (currentDirection)
                    {
                        case FlowDirection.LeftToRight:
                            {
                                textBounds = GetTextBoundsLeftToRight(firstRunIndex, lastRunIndex, currentX - directionalWidth, firstTextSourceIndex,
                                        currentPosition, remainingLength, out coveredLength, out currentPosition);

                                currentX -= directionalWidth;

                                break;
                            }
                        default:
                            {
                                textBounds = GetTextRunBoundsRightToLeft(firstRunIndex, lastRunIndex, currentX, firstTextSourceIndex,
                                        currentPosition, remainingLength, out coveredLength, out currentPosition);

                                currentX = textBounds.Rectangle.Left;

                                break;
                            }
                    }

                    //Visual order is always left to right so we need to insert
                    result.Insert(0, textBounds);

                    remainingLength -= coveredLength;

                    if (remainingLength <= 0)
                    {
                        break;
                    }
                }
            }

            return result;
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
                    var runBounds = GetRunBoundsRightToLeft(shapedTextRun, startX, firstTextSourceIndex, remainingLength, currentPosition, out var offset);

                    textRunBounds.Insert(0, runBounds);

                    if (offset > 0)
                    {
                        endX = runBounds.Rectangle.Right;

                        startX = endX;
                    }

                    startX -= runBounds.Rectangle.Width;

                    currentPosition += runBounds.Length + offset;

                    coveredLength += runBounds.Length;

                    remainingLength -= runBounds.Length;
                }
                else
                {
                    if (currentRun is DrawableTextRun drawableTextRun)
                    {
                        startX -= drawableTextRun.Size.Width;

                        textRunBounds.Insert(0,
                          new TextRunBounds(
                              new Rect(startX, 0, drawableTextRun.Size.Width, Height), currentPosition, currentRun.Length, currentRun));
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
            var textRunBounds = new List<TextRunBounds>();
            var endX = startX;

            for (int i = firstRunIndex; i <= lastRunIndex; i++)
            {
                var currentRun = _textRuns[i];

                if (currentRun is ShapedTextRun shapedTextRun)
                {
                    var runBounds = GetRunBoundsLeftToRight(shapedTextRun, endX, firstTextSourceIndex, remainingLength, currentPosition, out var offset);

                    textRunBounds.Add(runBounds);

                    if (offset > 0)
                    {
                        startX = runBounds.Rectangle.Left;

                        endX = startX;
                    }

                    currentPosition += runBounds.Length + offset;

                    endX += runBounds.Rectangle.Width;

                    coveredLength += runBounds.Length;

                    remainingLength -= runBounds.Length;
                }
                else
                {
                    if (currentRun is DrawableTextRun drawableTextRun)
                    {
                        textRunBounds.Add(
                            new TextRunBounds(
                                new Rect(endX, 0, drawableTextRun.Size.Width, Height), currentPosition, currentRun.Length, currentRun));

                        endX += drawableTextRun.Size.Width;
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

        private TextRunBounds GetRunBoundsLeftToRight(ShapedTextRun currentRun, double startX,
            int firstTextSourceIndex, int remainingLength, int currentPosition, out int offset)
        {
            var startIndex = currentPosition;

            offset = Math.Max(0, firstTextSourceIndex - currentPosition);

            var firstCluster = currentRun.GlyphRun.Metrics.FirstCluster;

            if (currentPosition != firstCluster)
            {
                startIndex = firstCluster + offset;
            }
            else
            {
                startIndex += offset;
            }

            var startOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));
            var endOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex + remainingLength));

            var endX = startX + endOffset;
            startX += startOffset;

            var startHit = currentRun.GlyphRun.GetCharacterHitFromDistance(startOffset, out _);
            var endHit = currentRun.GlyphRun.GetCharacterHitFromDistance(endOffset, out _);

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

            return new TextRunBounds(new Rect(startX, 0, runWidth, Height), currentPosition, characterLength, currentRun);
        }

        private TextRunBounds GetRunBoundsRightToLeft(ShapedTextRun currentRun, double endX,
            int firstTextSourceIndex, int remainingLength, int currentPosition, out int offset)
        {
            var startX = endX;

            var startIndex = currentPosition;

            offset = Math.Max(0, firstTextSourceIndex - currentPosition);

            var firstCluster = currentRun.GlyphRun.Metrics.FirstCluster;

            if (currentPosition != firstCluster)
            {
                startIndex = firstCluster + offset;
            }
            else
            {
                startIndex += offset;
            }

            var endOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex));

            var startOffset = currentRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(startIndex + remainingLength));

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
            _textLineMetrics = CreateLineMetrics();

            if (_textLineBreak is null && _textRuns.Length > 1 && _textRuns[_textRuns.Length - 1] is TextEndOfLine textEndOfLine)
            {
                _textLineBreak = new TextLineBreak(textEndOfLine);
            }

            BidiReorderer.Instance.BidiReorder(_textRuns, _resolvedFlowDirection);
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

            while (runIndex < _textRuns.Length)
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

            while (runIndex < _textRuns.Length)
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

                            if (runIndex + 1 >= _textRuns.Length)
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

                            if (runIndex + 1 >= _textRuns.Length)
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
            var fontMetrics = _paragraphProperties.DefaultTextRunProperties.CachedGlyphTypeface.Metrics;
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

            var lastRunIndex = _textRuns.Length - 1;

            if (lastRunIndex > 0 && _textRuns[lastRunIndex] is TextEndOfLine)
            {
                lastRunIndex--;
            }

            for (var index = 0; index < _textRuns.Length; index++)
            {
                switch (_textRuns[index])
                {
                    case ShapedTextRun textRun:
                        {
                            var textMetrics = textRun.TextMetrics;

                            if (fontRenderingEmSize < textMetrics.FontRenderingEmSize)
                            {
                                fontRenderingEmSize = textMetrics.FontRenderingEmSize;

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
                                newLineLength += textRun.GlyphRun.Metrics.NewLineLength;
                            }

                            widthIncludingWhitespace += textRun.Size.Width;

                            break;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            widthIncludingWhitespace += drawableTextRun.Size.Width;

                            if (index == lastRunIndex)
                            {
                                width = widthIncludingWhitespace;
                                trailingWhitespaceLength = 0;
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
    }
}
