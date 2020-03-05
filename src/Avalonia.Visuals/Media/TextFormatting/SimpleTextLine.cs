using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Media.TextFormatting
{
    internal class SimpleTextLine : TextLine
    {
        private readonly IReadOnlyList<ShapedTextRun> _textRuns;

        public SimpleTextLine(TextPointer textPointer, IReadOnlyList<ShapedTextRun> textRuns, TextLineMetrics lineMetrics)
        {
            Text = textPointer;
            _textRuns = textRuns;
            LineMetrics = lineMetrics;
        }

        /// <inheritdoc/>
        public override TextPointer Text { get; }

        /// <inheritdoc/>
        public override IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <inheritdoc/>
        public override TextLineMetrics LineMetrics { get; }

        /// <inheritdoc/>
        public override void Draw(IDrawingContextImpl drawingContext, Point origin)
        {
            var currentX = origin.X;

            foreach (var textRun in _textRuns)
            {
                var baselineOrigin = new Point(currentX + LineMetrics.BaselineOrigin.X,
                    origin.Y + LineMetrics.BaselineOrigin.Y);

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
                return new CharacterHit(Text.Start);
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
            int nextVisibleCp;
            bool navigableCpFound;

            if (characterHit.TrailingLength == 0)
            {
                navigableCpFound = FindNextCodepointIndex(characterHit.FirstCharacterIndex, out nextVisibleCp);

                if (navigableCpFound)
                {
                    // Move from leading to trailing edge
                    return new CharacterHit(nextVisibleCp, 1);
                }
            }

            navigableCpFound = FindNextCodepointIndex(characterHit.FirstCharacterIndex + 1, out nextVisibleCp);

            if (navigableCpFound)
            {
                // Move from trailing edge of current character to trailing edge of next
                return new CharacterHit(nextVisibleCp, 1);
            }

            // Can't move, we're after the last character
            return characterHit;
        }

        /// <inheritdoc/>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            int previousCodepointIndex;
            bool codepointIndexFound;

            var cpHit = characterHit.FirstCharacterIndex;
            var trailingHit = characterHit.TrailingLength != 0;

            // Input can be right after the end of the current line. Snap it to be at the end of the line.
            if (cpHit >= Text.Start + Text.Length)
            {
                cpHit = Text.Start + Text.Length - 1;

                trailingHit = true;
            }

            if (trailingHit)
            {
                codepointIndexFound = FindPreviousCodepointIndex(cpHit, out previousCodepointIndex);

                if (codepointIndexFound)
                {
                    // Move from trailing to leading edge
                    return new CharacterHit(previousCodepointIndex, 0);
                }
            }

            codepointIndexFound = FindPreviousCodepointIndex(cpHit - 1, out previousCodepointIndex);

            if (codepointIndexFound)
            {
                // Move from leading edge of current character to leading edge of previous
                return new CharacterHit(previousCodepointIndex, 0);
            }

            // Can't move, we're before the first character
            return characterHit;
        }

        /// <inheritdoc/>
        public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            // same operation as move-to-previous
            return GetPreviousCaretCharacterHit(characterHit);
        }

        /// <summary>
        /// Get distance from line start to the specified codepoint index
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
        /// Search forward from the given codepoint index (inclusive) to find the next navigable codepoint index.
        /// Return true if one such codepoint index is found, false otherwise.
        /// </summary>
        private bool FindNextCodepointIndex(int codepointIndex, out int nextCodepointIndex)
        {
            nextCodepointIndex = codepointIndex;

            if (codepointIndex >= Text.Start + Text.Length)
            {
                return false; // Cannot go forward anymore
            }

            GetRunIndexAtCodepointIndex(codepointIndex, out var runIndex, out var cpRunStart);

            while (runIndex < TextRuns.Count)
            {
                // When navigating forward, only the trailing edge of visible content is
                // navigable.
                if (runIndex < TextRuns.Count)
                {
                    nextCodepointIndex = Math.Max(cpRunStart, codepointIndex);
                    return true;
                }

                cpRunStart += TextRuns[runIndex++].Text.Length;
            }

            return false;
        }

        /// <summary>
        /// Search backward from the given codepoint index (inclusive) to find the previous navigable codepoint index.
        /// Return true if one such codepoint is found, false otherwise.
        /// </summary>
        private bool FindPreviousCodepointIndex(int codepointIndex, out int previousCodepointIndex)
        {
            previousCodepointIndex = codepointIndex;

            if (codepointIndex < Text.Start)
            {
                return false; // Cannot go backward anymore.
            }

            // Position the cpRunEnd at the end of the span that contains the given cp
            GetRunIndexAtCodepointIndex(codepointIndex, out var runIndex, out var codepointIndexAtRunEnd);

            codepointIndexAtRunEnd += TextRuns[runIndex].Text.End;

            while (runIndex >= 0)
            {
                // Visible content has caret stops at its leading edge.
                if (runIndex + 1 < TextRuns.Count)
                {
                    previousCodepointIndex = Math.Min(codepointIndexAtRunEnd, codepointIndex);
                    return true;
                }

                // Newline sequence has caret stops at its leading edge.
                if (runIndex == TextRuns.Count)
                {
                    // Get the cp index at the beginning of the newline sequence.
                    previousCodepointIndex = codepointIndexAtRunEnd - TextRuns[runIndex].Text.Length + 1;
                    return true;
                }

                codepointIndexAtRunEnd -= TextRuns[runIndex--].Text.Length;
            }

            return false;
        }

        private void GetRunIndexAtCodepointIndex(int codepointIndex, out int runIndex, out int codepointIndexAtRunStart)
        {
            codepointIndexAtRunStart = Text.Start;
            runIndex = 0;

            // Find the span that contains the given cp
            while (runIndex < TextRuns.Count &&
                   codepointIndexAtRunStart + TextRuns[runIndex].Text.Length <= codepointIndex)
            {
                codepointIndexAtRunStart += TextRuns[runIndex++].Text.Length;
            }
        }
    }
}
