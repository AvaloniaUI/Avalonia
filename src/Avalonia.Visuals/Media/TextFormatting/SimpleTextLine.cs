using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Media.TextFormatting
{
    internal class SimpleTextLine : TextLine
    {
        public SimpleTextLine(TextPointer textPointer, IReadOnlyList<TextRun> textRuns, TextLineMetrics lineMetrics) :
            base(textPointer, textRuns, lineMetrics)
        {

        }

        public override void Draw(IDrawingContextImpl drawingContext, Point origin)
        {
            var currentX = origin.X;

            foreach (var textRun in TextRuns)
            {
                if (!(textRun is DrawableTextRun drawableRun))
                {
                    continue;
                }

                var baselineOrigin = new Point(currentX + LineMetrics.BaselineOrigin.X,
                    origin.Y + LineMetrics.BaselineOrigin.Y);

                drawableRun.Draw(drawingContext, baselineOrigin);

                currentX += drawableRun.Bounds.Width;
            }
        }

        /// <summary>
        /// Client to get the character hit corresponding to the specified
        /// distance from the beginning of the line.
        /// </summary>
        /// <param name="distance">distance in text flow direction from the beginning of the line</param>
        /// <returns>character hit</returns>
        public override CharacterHit GetCharacterHitFromDistance(double distance)
        {
            var first = Text.Start;

            if (distance < 0)
            {
                // hit happens before the line, return the first position
                return new CharacterHit(Text.Start);
            }

            // process hit that happens within the line
            var runIndex = new CharacterHit();

            foreach (var run in TextRuns) 
            {
                var shapedTextRun = (ShapedTextRun)run;

                first += runIndex.TrailingLength;

                runIndex = shapedTextRun.GlyphRun.GetCharacterHitFromDistance(distance, out _);

                first += runIndex.FirstCharacterIndex;

                if (distance <= shapedTextRun.Bounds.Width)
                {
                    break;
                }

                distance -= shapedTextRun.Bounds.Width;
            }

            return new CharacterHit(first, runIndex.TrailingLength);
        }

        /// <summary>
        /// Client to get the distance from the beginning of the line from the specified
        /// character hit.
        /// </summary>
        /// <param name="characterHit">character hit of the character to query the distance.</param>
        /// <returns>distance in text flow direction from the beginning of the line.</returns>
        public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            return DistanceFromCp(characterHit.FirstCharacterIndex + (characterHit.TrailingLength != 0 ? 1 : 0));
        }

        /// <summary>
        /// Client to get the next character hit for caret navigation
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the next character hit</returns>
        public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            int nextVisibleCp;
            bool navigableCpFound;

            if (characterHit.TrailingLength == 0)
            {
                navigableCpFound = FindNextVisibleCp(characterHit.FirstCharacterIndex, out nextVisibleCp);

                if (navigableCpFound)
                {
                    // Move from leading to trailing edge
                    return new CharacterHit(nextVisibleCp, 1);
                }
            }

            navigableCpFound = FindNextVisibleCp(characterHit.FirstCharacterIndex + 1, out nextVisibleCp);

            if (navigableCpFound)
            {
                // Move from trailing edge of current character to trailing edge of next
                return new CharacterHit(nextVisibleCp, 1);
            }

            // Can't move, we're after the last character
            return characterHit;
        }

        /// <summary>
        /// Client to get the previous character hit for caret navigation
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the previous character hit</returns>
        public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            int previousVisibleCp;
            bool navigableCpFound;

            int cpHit = characterHit.FirstCharacterIndex;
            bool trailingHit = (characterHit.TrailingLength != 0);

            // Input can be right after the end of the current line. Snap it to be at the end of the line.
            if (cpHit >= Text.Start + Text.Length)
            {
                cpHit = Text.Start + Text.Length - 1;

                trailingHit = true;
            }

            if (trailingHit)
            {
                navigableCpFound = FindPreviousVisibleCp(cpHit, out previousVisibleCp);

                if (navigableCpFound)
                {
                    // Move from trailing to leading edge
                    return new CharacterHit(previousVisibleCp, 0);
                }
            }

            navigableCpFound = FindPreviousVisibleCp(cpHit - 1, out previousVisibleCp);

            if (navigableCpFound)
            {
                // Move from leading edge of current character to leading edge of previous
                return new CharacterHit(previousVisibleCp, 0);
            }

            // Can't move, we're before the first character
            return characterHit;
        }

        /// <summary>
        /// Client to get the previous character hit after backspacing
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the character hit after backspacing</returns>
        public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            // same operation as move-to-previous
            return GetPreviousCaretCharacterHit(characterHit);
        }

        /// <summary>
        /// Get distance from line start to the specified cp
        /// </summary>
        private double DistanceFromCp(int currentIndex)
        {
            var distance = 0.0;
            var dcp = currentIndex - Text.Start;

            foreach (var textRun in TextRuns)
            {
                var run = (ShapedTextRun)textRun;

                distance += run.GlyphRun.GetDistanceFromCharacterHit(new CharacterHit(dcp));

                if (dcp <= run.Text.Length)
                {
                    break;
                }

                dcp -= run.Text.Length;
            }

            return distance;
        }

        /// <summary>
        /// Search forward from the given cp index (inclusive) to find the next navigable cp index.
        /// Return true if one such cp is found, false otherwise.
        /// </summary>
        private bool FindNextVisibleCp(int cp, out int cpVisible)
        {
            cpVisible = cp;

            if (cp >= Text.Start + Text.Length)
            {
                return false; // Cannot go forward anymore
            }

            GetRunIndexAtCp(cp, out var runIndex, out var cpRunStart);

            while (runIndex < TextRuns.Count)
            {
                // When navigating forward, only the trailing edge of visible content is
                // navigable.
                if (runIndex < TextRuns.Count)
                {
                    cpVisible = Math.Max(cpRunStart, cp);
                    return true;
                }

                cpRunStart += TextRuns[runIndex++].Text.Length;
            }

            return false;
        }

        /// <summary>
        /// Search backward from the given cp index (inclusive) to find the previous navigable cp index.
        /// Return true if one such cp is found, false otherwise.
        /// </summary>
        private bool FindPreviousVisibleCp(int cp, out int cpVisible)
        {
            cpVisible = cp;

            if (cp < Text.Start)
            {
                return false; // Cannot go backward anymore.
            }

            // Position the cpRunEnd at the end of the span that contains the given cp
            GetRunIndexAtCp(cp, out var runIndex, out var cpRunEnd);

            cpRunEnd += TextRuns[runIndex].Text.End;

            while (runIndex >= 0)
            {
                // Visible content has caret stops at its leading edge.
                if (runIndex + 1 < TextRuns.Count)
                {
                    cpVisible = Math.Min(cpRunEnd, cp);
                    return true;
                }

                // Newline sequence has caret stops at its leading edge.
                if (runIndex == TextRuns.Count)
                {
                    // Get the cp index at the beginning of the newline sequence.
                    cpVisible = cpRunEnd - TextRuns[runIndex].Text.Length + 1;
                    return true;
                }

                cpRunEnd -= TextRuns[runIndex--].Text.Length;
            }

            return false;
        }

        private void GetRunIndexAtCp(int cp, out int runIndex, out int cpRunStart)
        {
            cpRunStart = Text.Start;
            runIndex = 0;

            // Find the span that contains the given cp
            while (runIndex < TextRuns.Count && cpRunStart + TextRuns[runIndex].Text.Length <= cp)
            {
                cpRunStart += TextRuns[runIndex++].Text.Length;
            }
        }
    }
}
