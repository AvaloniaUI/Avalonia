using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    internal class TextLineImpl : TextLine
    {
        private readonly IReadOnlyList<ShapedTextCharacters> _textRuns;

        public TextLineImpl(IReadOnlyList<ShapedTextCharacters> textRuns, TextLineMetrics lineMetrics,
            TextLineBreak lineBreak = null)
        {
            _textRuns = textRuns;
            LineMetrics = lineMetrics;
            LineBreak = lineBreak;
        }

        /// <inheritdoc/>
        public override TextRange TextRange => LineMetrics.TextRange;

        /// <inheritdoc/>
        public override IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <inheritdoc/>
        public override TextLineMetrics LineMetrics { get; }

        /// <inheritdoc/>
        public override TextLineBreak LineBreak { get; }

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

            return new CharacterHit(TextRange.End); // Can't move, we're after the last character
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (TryFindPreviousCharacterHit(characterHit, out var previousCharacterHit))
            {
                return previousCharacterHit;
            }

            return new CharacterHit(TextRange.Start); // Can't move, we're before the first character
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

            if (codepointIndex >= TextRange.Start + TextRange.Length)
            {
                return false; // Cannot go forward anymore
            }

            var runIndex = GetRunIndexAtCodepointIndex(codepointIndex);

            while (runIndex < TextRuns.Count)
            {
                var run = _textRuns[runIndex];

                nextCharacterHit = run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

                if (codepointIndex <= nextCharacterHit.FirstCharacterIndex + nextCharacterHit.TrailingLength)
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

                previousCharacterHit = run.GlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

                if (previousCharacterHit.FirstCharacterIndex < codepointIndex)
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
    }
}
