// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Text;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a piece of text with formatting.
    /// </summary>
    public class FormattedText
    {
        private Size _constraint = Size.Infinity;
        private TextLayout _textLayout;
        private IReadOnlyList<FormattedTextStyleSpan> _spans;
        private Typeface _typeface;
        private double _fontSize;
        private string _text;
        private IBrush _foreground;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;
        private TextTrimming _textTrimming;

        public FormattedText()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="typeface"></param>
        /// <param name="fontSize"></param>
        /// <param name="foreground"></param>
        /// <param name="textAlignment"></param>
        /// <param name="textWrapping"></param>
        /// <param name="textTrimming"></param>
        /// <param name="constraint"></param>
        public FormattedText(string text, Typeface typeface, double fontSize, IBrush foreground,
            TextAlignment textAlignment, TextWrapping textWrapping, TextTrimming textTrimming, 
            Size constraint)
        {
            _text = text;
            _typeface = typeface;
            _fontSize = fontSize;
            _foreground = foreground;
            _textAlignment = textAlignment;
            _textWrapping = textWrapping;
            _textTrimming = textTrimming;
            _constraint = constraint;
        }

        /// <summary>
        /// Gets the bounds of the text within the <see cref="Constraint"/>.
        /// </summary>
        /// <returns>The bounds of the text.</returns>
        public Rect Bounds => TextLayout.Bounds;

        /// <summary>
        /// Gets or sets the constraint of the text.
        /// </summary>
        public Size Constraint
        {
            get => _constraint;
            set
            {
                if (value == _constraint)
                {
                    return;
                }

                Set(ref _constraint, value);
            }
        }

        /// <summary>
        /// Gets or sets the base typeface.
        /// </summary>
        public Typeface Typeface
        {
            get => _typeface;
            set => Set(ref _typeface, value);
        }


        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set => Set(ref _fontSize, value);
        }

        /// <summary>
        /// Gets or sets the foreground.
        /// </summary>
        public IBrush Foreground
        {
            get => _foreground;
            set => Set(ref _foreground, value);
        }

        /// <summary>
        /// Gets or sets a collection of spans that describe the formatting of subsections of the
        /// text.
        /// </summary>
        public IReadOnlyList<FormattedTextStyleSpan> Spans
        {
            get => _spans;
            set => Set(ref _spans, value);
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text
        {
            get => _text;
            set => Set(ref _text, value);
        }

        /// <summary>
        /// Gets or sets the alignment of the text.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set => Set(ref _textAlignment, value);
        }

        /// <summary>
        /// Gets or sets the text wrapping.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => _textWrapping;
            set => Set(ref _textWrapping, value);
        }

        /// <summary>
        /// Gets or sets the text trimming.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get => _textTrimming;
            set => Set(ref _textTrimming, value);
        }

        /// <summary>
        /// Gets the actual text layout.
        /// </summary>
        public TextLayout TextLayout =>
            _textLayout ?? (_textLayout = new TextLayout(
                Text, Typeface, FontSize, Foreground, 
                TextAlignment, TextWrapping, TextTrimming,
                Constraint, Spans));

        /// <summary>
        ///     Hit tests the specified point.
        /// </summary>
        /// <param name="point">The point to hit test against.</param>
        /// <returns></returns>
        public TextHitTestResult HitTestPoint(Point point)
        {
            if (_text.Length == 0)
            {
                return new TextHitTestResult();
            }

            if (point == default)
            {
                return new TextHitTestResult(new CharacterHit(), point, true, false);
            }

            GlyphRun currentGlyphRun = null;
            var currentX = 0.0;
            var currentY = 0.0;
            var characterHit = new CharacterHit(_text.Length);
            var isInside = false;
            var isTrailing = false;

            foreach (var currentLine in TextLayout.TextLines)
            {
                currentX = 0;

                if (point.Y < currentY + currentLine.LineMetrics.Size.Height)
                {
                    currentX = currentLine.LineMetrics.BaselineOrigin.X;

                    if (point.X < 0.0)
                    {
                        currentGlyphRun = currentLine.TextRuns.FirstOrDefault()?.GlyphRun;

                        if (currentGlyphRun != null)
                        {
                            characterHit =
                                    currentGlyphRun.GetPreviousCaretCharacterHit(
                                        new CharacterHit(currentGlyphRun.Characters.Start));
                        }
                        else
                        {
                            characterHit = new CharacterHit(currentLine.Text.Start);
                        }

                        break;
                    }

                    if (point.X > currentLine.LineMetrics.Size.Width)
                    {
                        currentGlyphRun = currentLine.TextRuns.LastOrDefault()?.GlyphRun;

                        if (currentGlyphRun != null)
                        {
                            characterHit =
                                currentGlyphRun.FindNearestCharacterHit(currentGlyphRun.Characters.End, out var width);

                            isTrailing = width > 0;

                            currentX += currentGlyphRun.Bounds.Width;
                        }
                        else
                        {
                            characterHit = new CharacterHit(currentLine.Text.End + 1);
                        }

                        break;
                    }

                    foreach (var currentRun in currentLine.TextRuns)
                    {
                        if (currentX + currentRun.GlyphRun.Bounds.Width < point.X)
                        {
                            currentX += currentRun.GlyphRun.Bounds.Width;

                            continue;
                        }

                        var distance = point.X - currentX;

                        currentGlyphRun = currentRun.GlyphRun;

                        characterHit = currentGlyphRun.GetCharacterHitFromDistance(distance, out isInside);

                        currentGlyphRun.FindNearestCharacterHit(characterHit.FirstCharacterIndex, out var width);

                        isTrailing = width > 0.0 && characterHit.TrailingLength > 0;

                        break;
                    }
                }

                currentY += currentLine.LineMetrics.Size.Height;
            }

            if (currentGlyphRun == null)
            {
                currentGlyphRun = TextLayout.TextLines.Last().TextRuns.LastOrDefault()?.GlyphRun;

                var width = 0.0;

                currentGlyphRun?.FindNearestCharacterHit(_text.Length, out width);

                isTrailing = width > 0;
            }

            return new TextHitTestResult(characterHit, new Point(currentX, currentY), isInside, isTrailing);
        }

        /// <summary>
        ///     Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (!TextLayout.TextLines.Any())
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= _text.Length)
            {
                var lastLine = TextLayout.TextLines.Last();

                var offsetX = lastLine.LineMetrics.BaselineOrigin.X;

                var lineX = offsetX + lastLine.LineMetrics.Size.Width;

                var lineY = Bounds.Height - lastLine.LineMetrics.Size.Height;

                return new Rect(lineX, lineY, 0, lastLine.LineMetrics.Size.Height);
            }

            var currentY = 0.0;

            foreach (var textLine in TextLayout.TextLines)
            {
                if (textLine.Text.End < textPosition)
                {
                    currentY += textLine.LineMetrics.Size.Height;

                    continue;
                }

                var currentX = textLine.LineMetrics.BaselineOrigin.X;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (textRun.GlyphRun.Characters.End < textPosition)
                    {
                        currentX += textRun.GlyphRun.Bounds.Width;

                        continue;
                    }

                    var characterHit = textRun.GlyphRun.FindNearestCharacterHit(textPosition, out var width);

                    var distance = textRun.GlyphRun.GetDistanceFromCharacterHit(characterHit);

                    currentX += distance - width;

                    if (characterHit.TrailingLength == 0)
                    {
                        width = 0.0;
                    }

                    return new Rect(currentX, currentY, width, textRun.GlyphRun.Bounds.Height);
                }
            }

            return new Rect();
        }

        /// <summary>
        ///     Get a set of hit-test rectangles corresponding to a range of text positions.
        /// </summary>
        /// <param name="textPosition">The starting text position.</param>
        /// <param name="textLength">The text length.</param>
        /// <returns></returns>
        public IEnumerable<Rect> HitTestTextRange(int textPosition, int textLength)
        {
            var result = new List<Rect>();

            var currentY = 0.0;

            var remainingLength = textLength;

            foreach (var textLine in TextLayout.TextLines)
            {
                if (textLine.Text.End < textPosition)
                {
                    currentY += textLine.LineMetrics.Size.Height;

                    continue;
                }

                var lineX = textLine.LineMetrics.BaselineOrigin.X;

                var width = 0.0;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (remainingLength == 0)
                    {
                        break;
                    }

                    if (textRun.GlyphRun.Characters.End < textPosition)
                    {
                        lineX += textRun.GlyphRun.Bounds.Width;

                        continue;
                    }

                    if (textRun.GlyphRun.Characters.Start < textPosition)
                    {
                        var startHit = textRun.GlyphRun.FindNearestCharacterHit(textPosition, out _);

                        var offset =
                            textRun.GlyphRun.GetDistanceFromCharacterHit(
                                new CharacterHit(startHit.FirstCharacterIndex));

                        var length = textPosition - textRun.GlyphRun.Characters.Start;

                        var endHit = textRun.GlyphRun.FindNearestCharacterHit(textPosition + remainingLength, out _);

                        width = textRun.GlyphRun.GetDistanceFromCharacterHit(endHit) - offset;

                        lineX += offset;

                        remainingLength -= length;

                        continue;
                    }

                    if (remainingLength < textRun.GlyphRun.Characters.Length)
                    {
                        var characterHit =
                            textRun.GlyphRun.FindNearestCharacterHit(
                                textRun.GlyphRun.Characters.Start + remainingLength - 1, out _);

                        width += textRun.GlyphRun.GetDistanceFromCharacterHit(characterHit);

                        remainingLength = 0;

                        break;
                    }

                    width += textRun.GlyphRun.Bounds.Width;

                    remainingLength -= textRun.GlyphRun.Characters.Length;
                }

                var rect = new Rect(lineX, currentY, width, textLine.LineMetrics.Size.Height);

                result.Add(rect);

                if (remainingLength == 0)
                {
                    break;
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            return result;
        }

        private void Set<T>(ref T field, T value)
        {
            field = value;
            _textLayout = null;
        }
    }
}
