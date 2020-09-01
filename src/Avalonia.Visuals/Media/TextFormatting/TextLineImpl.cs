using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        private readonly List<ShapedTextCharacters> _textRuns;

        public TextLineImpl(List<ShapedTextCharacters> textRuns, TextLineMetrics lineMetrics,
            TextLineBreak lineBreak = null, bool hasCollapsed = false)
        {
            _textRuns = textRuns;
            LineMetrics = lineMetrics;
            TextLineBreak = lineBreak;
            HasCollapsed = hasCollapsed;
        }

        /// <inheritdoc/>
        public override TextRange TextRange => LineMetrics.TextRange;

        /// <inheritdoc/>
        public override IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <inheritdoc/>
        public override TextLineMetrics LineMetrics { get; }

        /// <inheritdoc/>
        public override TextLineBreak TextLineBreak { get; }

        /// <inheritdoc/>
        public override bool HasCollapsed { get; }

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            var currentX = origin.X;

            foreach (var textRun in _textRuns)
            {
                var baselineOrigin = new Point(currentX, origin.Y + LineMetrics.TextBaseline);

                textRun.Draw(drawingContext, baselineOrigin);

                currentX += textRun.Bounds.Width;
            }
        }

        /// <inheritdoc/>
        public override TextLine Collapse(params TextCollapsingProperties[] collapsingPropertiesList)
        {
            if (collapsingPropertiesList == null || collapsingPropertiesList.Length == 0)
            {
                return this;
            }

            var collapsingProperties = collapsingPropertiesList[0];
            var runIndex = 0;
            var currentWidth = 0.0;
            var textRange = TextRange;
            var collapsedLength = 0;
            TextLineMetrics textLineMetrics;

            var shapedSymbol = CreateShapedSymbol(collapsingProperties.Symbol);

            var availableWidth = collapsingProperties.Width - shapedSymbol.Bounds.Width;

            while (runIndex < _textRuns.Count)
            {
                var currentRun = _textRuns[runIndex];

                currentWidth += currentRun.GlyphRun.Bounds.Width;

                if (currentWidth > availableWidth)
                {
                    var measuredLength = TextFormatterImpl.MeasureCharacters(currentRun, availableWidth);

                    var currentBreakPosition = 0;

                    if (measuredLength < textRange.End)
                    {
                        var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                        while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                        {
                            var nextBreakPosition = lineBreaker.Current.PositionWrap;

                            if (nextBreakPosition == 0)
                            {
                                break;
                            }

                            if (nextBreakPosition > measuredLength)
                            {
                                break;
                            }

                            currentBreakPosition = nextBreakPosition;
                        }
                    }

                    if (collapsingProperties.Style == TextCollapsingStyle.TrailingWord)
                    {
                        measuredLength = currentBreakPosition;
                    }

                    collapsedLength += measuredLength;

                    var splitResult = TextFormatterImpl.SplitTextRuns(_textRuns, collapsedLength);

                    var shapedTextCharacters = new List<ShapedTextCharacters>(splitResult.First.Count + 1);

                    shapedTextCharacters.AddRange(splitResult.First);

                    shapedTextCharacters.Add(shapedSymbol);

                    textRange = new TextRange(textRange.Start, collapsedLength);

                    var shapedWidth = GetShapedWidth(shapedTextCharacters);

                    textLineMetrics = new TextLineMetrics(new Size(shapedWidth, LineMetrics.Size.Height),
                        LineMetrics.TextBaseline, textRange, false);

                    return new TextLineImpl(shapedTextCharacters, textLineMetrics, TextLineBreak, true);
                }

                availableWidth -= currentRun.GlyphRun.Bounds.Width;

                collapsedLength += currentRun.GlyphRun.Characters.Length;

                runIndex++;
            }

            textLineMetrics =
                new TextLineMetrics(LineMetrics.Size.WithWidth(LineMetrics.Size.Width + shapedSymbol.Bounds.Width),
                    LineMetrics.TextBaseline, TextRange, LineMetrics.HasOverflowed);

            return new TextLineImpl(new List<ShapedTextCharacters>(_textRuns) { shapedSymbol }, textLineMetrics, null,
                true);
        }

        /// <inheritdoc/>
        public override CharacterHit GetCharacterHitFromDistance(double distance)
        {
            if (distance < 0)
            {
                // hit happens before the line, return the first position
                return new CharacterHit(TextRange.Start);
            }

            // process hit that happens within the line
            var characterHit = new CharacterHit();

            foreach (var run in _textRuns)
            {
                characterHit = run.GlyphRun.GetCharacterHitFromDistance(distance, out _);

                if (distance <= run.Bounds.Width)
                {
                    break;
                }

                distance -= run.Bounds.Width;
            }

            return characterHit;
        }

        /// <inheritdoc/>
        public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            return DistanceFromCodepointIndex(characterHit.FirstCharacterIndex + (characterHit.TrailingLength != 0 ? 1 : 0));
        }

        /// <inheritdoc/>
        public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindNextCharacterHit(characterHit, out var nextCharacterHit))
            {
                return nextCharacterHit;
            }

            if (characterHit.FirstCharacterIndex + characterHit.TrailingLength <= TextRange.Start + TextRange.Length)
            {
                return characterHit; // Can't move, we're after the last character
            }

            var runIndex = GetRunIndexAtCodepointIndex(TextRange.End);

            var textRun = _textRuns[runIndex];

            characterHit = textRun.GlyphRun.GetNextCaretCharacterHit(characterHit);

            return characterHit; // Can't move, we're after the last character
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindPreviousCharacterHit(characterHit, out var previousCharacterHit))
            {
                return previousCharacterHit;
            }

            if (characterHit.FirstCharacterIndex < TextRange.Start)
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

        /// <summary>
        /// Get distance from line start to the specified codepoint index.
        /// </summary>
        private double DistanceFromCodepointIndex(int codepointIndex)
        {
            var currentDistance = 0.0;

            foreach (var textRun in _textRuns)
            {
                if (codepointIndex > textRun.Text.End)
                {
                    currentDistance += textRun.Bounds.Width;

                    continue;
                }

                return currentDistance + textRun.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(codepointIndex));
            }

            return currentDistance;
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

            if (codepointIndex > TextRange.End)
            {
                return false; // Cannot go forward anymore
            }

            var runIndex = GetRunIndexAtCodepointIndex(codepointIndex);

            while (runIndex < TextRuns.Count)
            {
                var run = _textRuns[runIndex];

                var foundCharacterHit = run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

                var isAtEnd = foundCharacterHit.FirstCharacterIndex + foundCharacterHit.TrailingLength ==
                              TextRange.Length;

                var characterIndex = codepointIndex - run.Text.Start;

                var codepoint = Codepoint.ReadAt(run.GlyphRun.Characters, characterIndex, out _);

                if (codepoint.IsBreakChar)
                {
                    foundCharacterHit = run.GlyphRun.FindNearestCharacterHit(codepointIndex - 1, out _);

                    isAtEnd = true;
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
            if (characterHit.FirstCharacterIndex == TextRange.Start)
            {
                previousCharacterHit = new CharacterHit(TextRange.Start);

                return true;
            }

            previousCharacterHit = characterHit;

            var codepointIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (codepointIndex < TextRange.Start)
            {
                return false; // Cannot go backward anymore.
            }

            var runIndex = GetRunIndexAtCodepointIndex(codepointIndex);

            while (runIndex >= 0)
            {
                var run = _textRuns[runIndex];

                var foundCharacterHit = run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

                previousCharacterHit = characterHit.TrailingLength != 0 ?
                    foundCharacterHit :
                    new CharacterHit(foundCharacterHit.FirstCharacterIndex);

                if (previousCharacterHit.FirstCharacterIndex < characterHit.FirstCharacterIndex)
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
        /// <returns>The text run index.</returns>
        private int GetRunIndexAtCodepointIndex(int codepointIndex)
        {
            if (codepointIndex >= TextRange.End)
            {
                return _textRuns.Count - 1;
            }

            if (codepointIndex <= 0)
            {
                return 0;
            }

            var runIndex = 0;

            while (runIndex < _textRuns.Count)
            {
                var run = _textRuns[runIndex];

                if (run.Text.End > codepointIndex)
                {
                    return runIndex;
                }

                runIndex++;
            }

            return runIndex;
        }

        /// <summary>
        /// Creates a shaped symbol.
        /// </summary>
        /// <param name="textRun">The symbol run to shape.</param>
        /// <returns>
        /// The shaped symbol.
        /// </returns>
        internal static ShapedTextCharacters CreateShapedSymbol(TextRun textRun)
        {
            var formatterImpl = AvaloniaLocator.Current.GetService<ITextShaperImpl>();

            var glyphRun = formatterImpl.ShapeText(textRun.Text, textRun.Properties.Typeface, textRun.Properties.FontRenderingEmSize,
                textRun.Properties.CultureInfo);

            return new ShapedTextCharacters(glyphRun, textRun.Properties);
        }

        /// <summary>
        /// Gets the shaped width of specified shaped text characters.
        /// </summary>
        /// <param name="shapedTextCharacters">The shaped text characters.</param>
        /// <returns>
        /// The shaped width.
        /// </returns>
        private static double GetShapedWidth(IReadOnlyList<ShapedTextCharacters> shapedTextCharacters)
        {
            var shapedWidth = 0.0;

            for (var i = 0; i < shapedTextCharacters.Count; i++)
            {
                shapedWidth += shapedTextCharacters[i].Bounds.Width;
            }

            return shapedWidth;
        }
    }
}
