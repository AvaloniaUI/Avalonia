// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Utility;

namespace Avalonia.Media.Text
{
    [DebuggerTypeProxy(typeof(TextLineDebuggerProxy))]
    public class TextLine
    {
        public TextLine(ReadOnlySlice<char> text, IReadOnlyList<TextRun> textRuns, TextLineMetrics lineMetrics)
        {
            Text = text;
            TextRuns = textRuns;
            LineMetrics = lineMetrics;
        }

        /// <summary>
        ///     Gets the text pointer.
        /// </summary>
        /// <value>
        /// The text pointer.
        /// </value>
        public ReadOnlySlice<char> Text { get; }

        /// <summary>
        ///     Gets the text runs.
        /// </summary>
        /// <value>
        ///     The text runs.
        /// </value>
        public IReadOnlyList<TextRun> TextRuns { get; }

        /// <summary>
        ///     Gets the line metrics.
        /// </summary>
        /// <value>
        ///     The line metrics.
        /// </value>
        public TextLineMetrics LineMetrics { get; }

        /// <summary>
        ///     Get the character hit corresponding to the specified 
        ///     distance from the beginning of the line.
        /// </summary>
        /// <param name="distance">distance in text flow direction from the beginning of the line</param>
        /// <returns>character hit</returns>
        public CharacterHit GetCharacterHitFromDistance(double distance)
        {
            if (distance < 0)
            {
                return new CharacterHit(Text.Start);
            }

            var currentX = LineMetrics.BaselineOrigin.X;

            foreach (var currentRun in TextRuns)
            {
                if (currentX + currentRun.GlyphRun.Bounds.Width < distance)
                {
                    currentX += currentRun.GlyphRun.Bounds.Width;

                    continue;
                }

                var remainingDistance = distance - currentX;

                var currentGlyphRun = currentRun.GlyphRun;

                return currentGlyphRun.GetCharacterHitFromDistance(remainingDistance, out _);
            }

            return new CharacterHit(Text.Start + Text.Length);
        }

        /// <summary>
        ///     Get the distance from the beginning of the line from the specified 
        ///     character hit.
        /// </summary>
        /// <param name="characterHit">character hit of the character to query the distance.</param>
        /// <returns>distance in text flow direction from the beginning of the line.</returns>
        public double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var currentX = LineMetrics.BaselineOrigin.X;
            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (textPosition < 0 || textPosition < Text.Start)
            {
                return 0;
            }

            foreach (var textRun in TextRuns)
            {
                if (textRun.GlyphRun.Characters.End < textPosition)
                {
                    currentX += textRun.GlyphRun.Bounds.Width;

                    continue;
                }

                var distance = textRun.GlyphRun.GetDistanceFromCharacterHit(characterHit);

                return currentX + distance;
            }

            return LineMetrics.Size.Width;
        }

        /// <summary>
        ///     Get the next character hit for caret navigation.
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the next character hit</returns>
        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (textPosition > Text.Start)
            {
                return characterHit;
            }

            for (var runIndex = 0; runIndex < TextRuns.Count; runIndex++)
            {
                var textRun = TextRuns[runIndex];

                if (textRun.GlyphRun.Characters.End < textPosition)
                {
                    continue;
                }

                var glyphRun = textRun.GlyphRun;

                return glyphRun.GetNextCaretCharacterHit(characterHit);
            }

            return characterHit;
        }

        /// <summary>
        ///     Get the previous character hit for caret navigation.
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the previous character hit</returns>
        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            if (textPosition < Text.Start)
            {
                return characterHit;
            }

            foreach (var textRun in TextRuns)
            {
                var glyphRun = textRun.GlyphRun;

                return glyphRun.GetPreviousCaretCharacterHit(characterHit);
            }

            return characterHit;
        }

        /// <summary>
        ///     Get the previous character hit after backspacing.
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the character hit after backspacing</returns>
        public CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            var previousHit = GetPreviousCaretCharacterHit(characterHit);

            return previousHit;
        }

        /// <summary>
        ///     Gets the text line offset x.
        /// </summary>
        /// <param name="lineWidth">The line width.</param>
        /// <param name="paragraphWidth"></param>
        /// <param name="textAlignment"></param>
        /// <returns></returns>
        internal static double GetParagraphOffsetX(double lineWidth, double paragraphWidth, TextAlignment textAlignment)
        {
            //ToDo: This needs to be set after all lines are build up.

            if (double.IsPositiveInfinity(paragraphWidth))
            {
                return 0;
            }

            switch (textAlignment)
            {
                case TextAlignment.Center:
                    return (paragraphWidth - lineWidth) / 2;

                case TextAlignment.Right:
                    return paragraphWidth - lineWidth;

                default:
                    return 0.0f;
            }
        }

        private class TextLineDebuggerProxy
        {
            private readonly TextLine _textLine;

            public TextLineDebuggerProxy(TextLine textLine)
            {
                _textLine = textLine;
            }

            public string Text
            {
                get
                {
                    unsafe
                    {
                        fixed (char* charsPtr = _textLine.Text.AsSpan())
                        {
                            return new string(charsPtr, 0, _textLine.Text.Length);
                        }
                    }
                }
            }

            public IReadOnlyList<TextRun> TextRuns => _textLine.TextRuns;

            public TextLineMetrics LineMetrics => _textLine.LineMetrics;
        }
    }
}
