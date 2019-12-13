// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Text.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.Media.Text
{
    public class TextLayout
    {
        private static readonly ITextFormatter s_textFormatter;
        private static readonly ReadOnlySlice<char> s_ellipsis = new ReadOnlySlice<char>(new[] { '\u2026' });

        private readonly Typeface _typeface;
        private readonly double _fontSize;
        private readonly TextAlignment _textAlignment;
        private readonly TextWrapping _textWrapping;
        private readonly TextTrimming _textTrimming;
        private readonly Size _constraint;
        private readonly ReadOnlySlice<char> _text;

        static TextLayout()
        {
            s_textFormatter = AvaloniaLocator.Current.GetService<ITextFormatter>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TextLayout" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="constraint">The constraint.</param>
        /// <param name="spans">The spans.</param>
        public TextLayout(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping textWrapping,
            TextTrimming textTrimming,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans = null)
        {
            _typeface = typeface;
            _fontSize = fontSize;
            _textAlignment = textAlignment;
            _textWrapping = textWrapping;
            _textTrimming = textTrimming;
            _constraint = constraint;
            _text = string.IsNullOrEmpty(text) ?
                new ReadOnlySlice<char>() :
                new ReadOnlySlice<char>(text.AsMemory());
            CreateTextLines(spans);
        }

        /// <summary>
        ///     Gets the text lines.
        /// </summary>
        /// <value>
        ///     The text lines.
        /// </value>
        public IReadOnlyList<TextLine> TextLines { get; private set; }

        /// <summary>
        ///     Gets the text metrics of the layout.
        /// </summary>
        /// <value>
        ///     The size.
        /// </value>
        public Rect Bounds { get; private set; }

        /// <summary>
        ///     Draws the layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="foreground">The default foreground.</param>
        /// <param name="origin">The origin.</param>
        public void Draw(IDrawingContextImpl context, IBrush foreground, Point origin)
        {
            if (!TextLines.Any())
            {
                return;
            }

            var currentY = origin.Y;

            foreach (var textLine in TextLines)
            {
                var currentX = origin.X;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (textRun.TextFormat.Typeface == null || textRun.GlyphRun.GlyphIndices.Length == 0)
                    {
                        continue;
                    }

                    var baselineOrigin = new Point(currentX + textLine.LineMetrics.BaselineOrigin.X,
                        currentY + textLine.LineMetrics.BaselineOrigin.Y);

                    context.DrawGlyphRun(textRun.Foreground ?? foreground, textRun.GlyphRun, baselineOrigin);

                    currentX += textRun.GlyphRun.Bounds.Width;
                }

                currentY += textLine.LineMetrics.Size.Height;
            }
        }

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

            var pointY = (float)point.Y;

            var currentY = 0.0;

            for (var i = 0; i < TextLines.Count; i++)
            {
                var currentLine = TextLines[i];

                CharacterHit characterHit;

                if (pointY < currentY + currentLine.LineMetrics.Size.Height)
                {
                    var currentX = currentLine.LineMetrics.BaselineOrigin.X;

                    bool isInside;

                    foreach (var currentRun in currentLine.TextRuns)
                    {
                        if (currentX + currentRun.GlyphRun.Bounds.Width < point.X)
                        {
                            currentX += currentRun.GlyphRun.Bounds.Width;

                            continue;
                        }

                        var distance = point.X - currentX;

                        characterHit = currentRun.GlyphRun.GetCharacterHitFromDistance(distance, out isInside);

                        return new TextHitTestResult(characterHit, new Point(distance, currentY), isInside,
                            characterHit.TrailingLength > 0);
                    }

                    var lastTextRun = currentLine.TextRuns[currentLine.TextRuns.Count - 1];

                    characterHit = lastTextRun.GlyphRun.GetCharacterHitFromDistance(currentX, out isInside);

                    return new TextHitTestResult(characterHit, new Point(currentX, currentY), isInside,
                        characterHit.FirstCharacterIndex + characterHit.TrailingLength == _text.Length);
                }

                if (i == TextLines.Count - 1)
                {
                    var lastTextRun = currentLine.TextRuns[currentLine.TextRuns.Count - 1];

                    characterHit = lastTextRun.GlyphRun.FindNearestCharacterHit(lastTextRun.GlyphRun.Characters.End, out _);

                    var currentX = currentLine.LineMetrics.BaselineOrigin.X + currentLine.LineMetrics.Size.Width;

                    return new TextHitTestResult(characterHit, new Point(currentX, currentY), false, true);
                }

                currentY += currentLine.LineMetrics.Size.Height;
            }

            return new TextHitTestResult();
        }

        /// <summary>
        ///     Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (!TextLines.Any())
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= _text.Length)
            {
                var lastLine = TextLines.Last();

                var offsetX = lastLine.LineMetrics.BaselineOrigin.X;

                var lineX = offsetX + lastLine.LineMetrics.Size.Width;

                var lineY = Bounds.Height - lastLine.LineMetrics.Size.Height;

                return new Rect(lineX, lineY, 0, lastLine.LineMetrics.Size.Height);
            }

            var currentY = 0.0;

            foreach (var textLine in TextLines)
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

            foreach (var textLine in TextLines)
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


        /// <summary>
        ///     Creates an ellipsis.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <param name="foreground">The foreground.</param>
        /// <returns></returns>
        private static TextRun CreateEllipsisRun(TextFormat textFormat, IBrush foreground)
        {
            var glyphRun = s_textFormatter.CreateShapedGlyphRun(s_ellipsis, textFormat);

            return new TextRun(glyphRun, textFormat, foreground);
        }

        /// <summary>
        ///     Applies a text style span to the layout.
        /// </summary>
        /// <param name="textRuns"></param>
        /// <param name="span">The text style span.</param>
        private static void ApplyTextStyleSpan(IList<TextRunProperties> textRuns, FormattedTextStyleSpan span)
        {
            var currentPosition = 0;
            var appliedLength = 0;

            for (var runIndex = 0; runIndex < textRuns.Count; runIndex++)
            {
                var currentTextRun = textRuns[runIndex];

                if (currentTextRun.Text.Length == 0)
                {
                    continue;
                }

                if (currentPosition + currentTextRun.Text.Length - 1 < span.StartIndex)
                {
                    currentPosition += currentTextRun.Text.Length;

                    continue;
                }

                if (currentPosition == span.StartIndex + appliedLength)
                {
                    var splitLength = span.Length - appliedLength;

                    if (splitLength >= currentTextRun.Text.Length)
                    {
                        // Apply to the whole run 
                        textRuns.RemoveAt(runIndex);

                        var updatedTextRun = ApplyTextStyleSpan(span, currentTextRun);

                        appliedLength += updatedTextRun.Text.Length;

                        textRuns.Insert(runIndex, updatedTextRun);
                    }
                    else
                    {
                        // Apply at start of the run 
                        var start = currentTextRun.Split(splitLength);

                        textRuns.RemoveAt(runIndex);

                        var updatedTextRun = ApplyTextStyleSpan(span, start.First);

                        appliedLength += updatedTextRun.Text.Length;

                        textRuns.Insert(runIndex, updatedTextRun);

                        runIndex++;

                        textRuns.Insert(runIndex, start.Second);
                    }
                }
                else
                {
                    var splitLength = Math.Min(
                        span.StartIndex + appliedLength - currentPosition,
                        currentTextRun.Text.Length);

                    if (splitLength > 0)
                    {
                        var start = currentTextRun.Split(splitLength);

                        if (splitLength + span.Length - appliedLength >= currentTextRun.Text.Length)
                        {
                            // Apply at the end of the run
                            textRuns.RemoveAt(runIndex);

                            textRuns.Insert(runIndex, start.First);

                            runIndex++;

                            var updatedTextRun = ApplyTextStyleSpan(span, start.Second);

                            appliedLength += updatedTextRun.Text.Length;

                            textRuns.Insert(runIndex, updatedTextRun);
                        }
                        else
                        {
                            splitLength = span.Length;

                            // Apply in between the run
                            var end = start.Second.Split(splitLength);

                            textRuns.RemoveAt(runIndex);

                            textRuns.Insert(runIndex, start.First);

                            runIndex++;

                            var updatedTextRun = ApplyTextStyleSpan(span, end.First);

                            appliedLength += updatedTextRun.Text.Length;

                            textRuns.Insert(runIndex, updatedTextRun);

                            runIndex++;

                            textRuns.Insert(runIndex, end.Second);
                        }
                    }
                    else
                    {
                        textRuns.RemoveAt(runIndex);

                        var updatedTextRun = ApplyTextStyleSpan(span, currentTextRun);

                        textRuns.Insert(runIndex, updatedTextRun);
                    }
                }

                if (appliedLength >= span.Length)
                {
                    return;
                }

                currentPosition += currentTextRun.Text.Length;
            }
        }

        /// <summary>
        ///     Applies the text style span to a text run.
        /// </summary>
        /// <param name="span">The text span.</param>
        /// <param name="textRun">The text run.</param>
        /// <returns></returns>
        private static TextRunProperties ApplyTextStyleSpan(FormattedTextStyleSpan span, TextRunProperties textRun)
        {
            var textPointer = textRun.Text;
            var drawingEffect = span.Foreground?.ToImmutable() ?? textRun.Foreground;

            var textFormat = new TextFormat(span.Typeface ?? textRun.TextFormat.Typeface,
                span.FontSize ?? textRun.TextFormat.FontSize);

            return new TextRunProperties(textPointer, textFormat, drawingEffect);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textLine"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        private static void UpdateBounds(TextLine textLine, ref double left, ref double right, ref double bottom)
        {
            if (right < textLine.LineMetrics.BaselineOrigin.X + textLine.LineMetrics.Size.Width)
            {
                right = textLine.LineMetrics.BaselineOrigin.X + textLine.LineMetrics.Size.Width;
            }

            if (left < textLine.LineMetrics.BaselineOrigin.X)
            {
                left = textLine.LineMetrics.BaselineOrigin.X;
            }

            bottom += textLine.LineMetrics.Size.Height;
        }

        /// <summary>
        ///     Creates the text line metrics.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <returns></returns>
        private TextLineMetrics CreateTextLineMetrics(IEnumerable<TextRun> textRuns)
        {
            var width = 0.0;
            var ascent = 0.0;
            var descent = 0.0;
            var lineGap = 0.0;

            foreach (var textRun in textRuns)
            {
                UpdateTextLineMetrics(textRun, ref width, ref ascent, ref descent, ref lineGap);
            }

            var xOrigin = GetTextLineOffsetX(width);

            return new TextLineMetrics(width, xOrigin, ascent, descent, lineGap);
        }

        /// <summary>
        ///     Creates a empty text line.
        /// </summary>
        /// <returns></returns>
        private TextLine CreateEmptyTextLine(int startingIndex)
        {
            var fontMetrics = new FontMetrics(_typeface, _fontSize);

            return new TextLine(new ReadOnlySlice<char>(new ReadOnlyMemory<char>(), startingIndex, 0),
                new List<TextRun>(),
                new TextLineMetrics(0, 0, fontMetrics.Ascent, fontMetrics.Descent, fontMetrics.LineGap));
        }

        /// <summary>
        ///     Gets the text line offset x.
        /// </summary>
        /// <param name="lineWidth">The line width.</param>
        /// <returns></returns>
        private double GetTextLineOffsetX(double lineWidth)
        {
            //ToDo: This needs to be set after all lines are build up.

            var availableWidth = _constraint.Width > 0 && !double.IsPositiveInfinity(_constraint.Width) ?
                _constraint.Width :
                Bounds.Width;

            switch (_textAlignment)
            {
                case TextAlignment.Center:
                    return (availableWidth - lineWidth) / 2;

                case TextAlignment.Right:
                    return availableWidth - lineWidth;

                default:
                    return 0.0f;
            }
        }

        private static void UpdateTextLineMetrics(TextRun textRun, ref double width, ref double ascent, ref double descent, ref double lineGap)
        {
            width += textRun.GlyphRun.Bounds.Width;

            if (ascent > textRun.TextFormat.FontMetrics.Ascent)
            {
                ascent = textRun.TextFormat.FontMetrics.Ascent;
            }

            if (descent < textRun.TextFormat.FontMetrics.Descent)
            {
                descent = textRun.TextFormat.FontMetrics.Descent;
            }

            if (lineGap < textRun.TextFormat.FontMetrics.LineGap)
            {
                lineGap = textRun.TextFormat.FontMetrics.LineGap;
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="spans"></param>
        private void CreateTextLines(IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            if (_text.Length == 0 || Math.Abs(_constraint.Width) < double.Epsilon ||
                Math.Abs(_constraint.Height) < double.Epsilon)
            {
                var textLine = CreateEmptyTextLine(0);

                TextLines = new List<TextLine> { textLine };

                Bounds = new Rect(GetTextLineOffsetX(0), 0, 0, textLine.LineMetrics.Size.Height);
            }
            else
            {
                var runProperties = s_textFormatter.CreateTextRuns(_text, _typeface, _fontSize);

                var textLines = new List<TextLine>();

                if (spans != null)
                {
                    foreach (var span in spans)
                    {
                        ApplyTextStyleSpan(runProperties, span);
                    }
                }

                double left = 0.0, right = 0.0, bottom = 0.0;

                var lineBreaker = new LineBreaker(_text);

                var currentPosition = 0;

                while (currentPosition < _text.Length)
                {
                    int length;

                    if (lineBreaker.NextBreak())
                    {
                        if (!lineBreaker.CurrentBreak.Required)
                        {
                            continue;
                        }

                        length = lineBreaker.CurrentBreak.PositionWrap - currentPosition;

                        if (currentPosition + length < _text.Length)
                        {
                            if (_text[lineBreaker.CurrentBreak.PositionWrap] == '\r' &&
                                _text[lineBreaker.CurrentBreak.PositionWrap - 1] == '\n'
                                || _text[lineBreaker.CurrentBreak.PositionWrap] == '\n' &&
                                _text[lineBreaker.CurrentBreak.PositionWrap - 1] == '\r')
                            {
                                length++;
                            }
                        }
                    }
                    else
                    {
                        length = _text.Length - currentPosition;
                    }

                    var textSlice = _text.AsSlice(currentPosition, length);

                    var textRuns = s_textFormatter.FormatTextRuns(textSlice, runProperties);

                    if (_textTrimming != TextTrimming.None)
                    {
                        var textLine = PerformTextTrimming(textRuns, textSlice);

                        UpdateBounds(textLine, ref left, ref right, ref bottom);

                        textLines.Add(textLine);

                        currentPosition += textLine.Text.Length;
                    }
                    else
                    {
                        if (_textWrapping == TextWrapping.Wrap)
                        {
                            foreach (var textLine in PerformTextWrapping(textRuns, textSlice))
                            {
                                UpdateBounds(textLine, ref left, ref right, ref bottom);

                                textLines.Add(textLine);

                                currentPosition += textLine.Text.Length;
                            }
                        }
                        else
                        {
                            var textLineMetrics = CreateTextLineMetrics(textRuns);

                            var textLine = new TextLine(textSlice, textRuns, textLineMetrics);

                            UpdateBounds(textLine, ref left, ref right, ref bottom);

                            textLines.Add(textLine);

                            currentPosition += textLine.Text.Length;
                        }
                    }

                    if (lineBreaker.CurrentBreak.Required && currentPosition == _text.Length)
                    {
                        var textLine = CreateEmptyTextLine(currentPosition);

                        UpdateBounds(textLine, ref left, ref right, ref bottom);

                        textLines.Add(textLine);

                        break;
                    }
                }

                Bounds = new Rect(left, 0, right, bottom);

                TextLines = textLines;
            }
        }

        /// <summary>
        ///     Performs text trimming and returns a trimmed line.
        /// </summary>
        /// <param name="textRuns"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private TextLine PerformTextTrimming(IReadOnlyList<TextRun> textRuns, ReadOnlySlice<char> text)
        {
            var availableWidth = _constraint.Width;
            var currentWidth = 0.0;
            var runIndex = 0;

            while (runIndex < textRuns.Count)
            {
                var currentRun = textRuns[runIndex];

                currentWidth += currentRun.GlyphRun.Bounds.Width;

                if (currentWidth > availableWidth)
                {
                    var ellipsisRun = CreateEllipsisRun(currentRun.TextFormat, currentRun.Foreground);

                    var measuredLength = MeasureText(textRuns, _constraint.Width - ellipsisRun.GlyphRun.Bounds.Width);

                    if (_textTrimming == TextTrimming.WordEllipsis)
                    {
                        if (measuredLength < text.End)
                        {
                            var currentBreakPosition = 0;

                            var lineBreaker = new LineBreaker(text);

                            while (currentBreakPosition < measuredLength && lineBreaker.NextBreak())
                            {
                                var nextBreakPosition = lineBreaker.CurrentBreak.PositionWrap;

                                if (nextBreakPosition == -1)
                                {
                                    break;
                                }

                                if (nextBreakPosition > measuredLength)
                                {
                                    break;
                                }

                                currentBreakPosition = nextBreakPosition;
                            }

                            measuredLength = currentBreakPosition + 1;
                        }
                    }

                    var splitResult = SplitTextRuns(textRuns, measuredLength);

                    var trimmedRuns = new List<TextRun>(splitResult.First.Count + 1);

                    trimmedRuns.AddRange(splitResult.First);

                    trimmedRuns.Add(ellipsisRun);

                    var textLineMetrics = CreateTextLineMetrics(trimmedRuns);

                    return new TextLine(_text.Take(measuredLength), textRuns, textLineMetrics);
                }

                availableWidth -= currentRun.GlyphRun.Bounds.Width;

                runIndex++;
            }

            return new TextLine(text, textRuns, CreateTextLineMetrics(textRuns));
        }

        /// <summary>
        ///     Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns">The text run'S.</param>
        /// <param name="text">The text to analyze for break opportunities.</param>
        /// <returns></returns>
        private IEnumerable<TextLine> PerformTextWrapping(IReadOnlyList<TextRun> textRuns, ReadOnlySlice<char> text)
        {
            var textLines = new List<TextLine>();
            var currentPosition = 0;
            var currentTextRuns = textRuns;

            while (currentPosition < text.Length)
            {
                var count = MeasureText(currentTextRuns, _constraint.Width);

                var endPosition = currentPosition + count;

                if (endPosition < text.Length)
                {
                    var currentBreakPosition = currentPosition;

                    var lineBreaker = new LineBreaker(text);

                    while (currentBreakPosition < endPosition && lineBreaker.NextBreak())
                    {
                        var nextBreakPosition = lineBreaker.CurrentBreak.PositionWrap;

                        if (nextBreakPosition == -1)
                        {
                            break;
                        }

                        if (nextBreakPosition > endPosition)
                        {
                            break;
                        }

                        currentBreakPosition = nextBreakPosition;
                    }

                    var length = currentBreakPosition - currentPosition;

                    var splitResult = SplitTextRuns(currentTextRuns, length);

                    textLines.Add(new TextLine(text.AsSlice(currentPosition, length), splitResult.First,
                        CreateTextLineMetrics(splitResult.First)));

                    currentPosition += length;

                    currentTextRuns = splitResult.Second;
                }
                else
                {
                    var length = text.Length - currentPosition;

                    var lineMetrics = CreateTextLineMetrics(currentTextRuns);

                    textLines.Add(new TextLine(text.AsSlice(currentPosition, length), currentTextRuns, lineMetrics));

                    break;
                }
            }

            return textLines;
        }

        /// <summary>
        ///     Measures the number of characters that fits into available width.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <returns></returns>
        private static int MeasureText(IEnumerable<TextRun> textRuns, double availableWidth)
        {
            var measuredWidth = 0.0;
            var count = 0;

            foreach (var textRun in textRuns)
            {
                if (textRun.GlyphRun.Bounds.Width + measuredWidth < availableWidth)
                {
                    measuredWidth += textRun.GlyphRun.Bounds.Width;
                    count += textRun.GlyphRun.Characters.Length;
                    continue;
                }

                var index = 0;

                for (; index < textRun.GlyphRun.GlyphAdvances.Length; index++)
                {
                    var advance = textRun.GlyphRun.GlyphAdvances[index];

                    if (measuredWidth + advance > availableWidth)
                    {
                        break;
                    }

                    measuredWidth += advance;
                }

                var cluster = textRun.GlyphRun.GlyphClusters[index];

                var characterHit = textRun.GlyphRun.FindNearestCharacterHit(cluster, out _);

                count += characterHit.FirstCharacterIndex - textRun.GlyphRun.Characters.Start +
                         (textRun.GlyphRun.IsLeftToRight ? characterHit.TrailingLength : 0);
            }

            return count;
        }

        /// <summary>
        ///     Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <returns></returns>
        private static SplitTextRunsResult SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length)
        {
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                if (currentLength + currentRun.GlyphRun.Characters.Length < length)
                {
                    currentLength += currentRun.GlyphRun.Characters.Length;
                    continue;
                }

                var firstCount = currentRun.GlyphRun.Characters.Length > 1 ? i + 1 : i;

                var first = new TextRun[firstCount];

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first[j] = textRuns[j];
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRun.GlyphRun.Characters.Length == length)
                {
                    var second = new TextRun[secondCount];

                    var offset = currentRun.GlyphRun.Characters.Length > 1 ? 1 : 0;

                    if (secondCount > 0)
                    {
                        for (var j = 0; j < secondCount; j++)
                        {
                            second[j] = textRuns[i + j + offset];
                        }
                    }

                    first[i] = currentRun;

                    return new SplitTextRunsResult(first, second);
                }
                else
                {
                    secondCount++;

                    var second = new TextRun[secondCount];

                    if (secondCount > 0)
                    {
                        for (var j = 1; j < secondCount; j++)
                        {
                            second[j] = textRuns[i + j];
                        }
                    }

                    var split = currentRun.Split(length - currentLength);

                    first[i] = split.First;

                    second[0] = split.Second;

                    return new SplitTextRunsResult(first, second);
                }
            }

            return new SplitTextRunsResult(textRuns, null);
        }

        private readonly struct SplitTextRunsResult
        {
            public SplitTextRunsResult(IReadOnlyList<TextRun> first, IReadOnlyList<TextRun> second)
            {
                First = first;

                Second = second;
            }

            /// <summary>
            ///     Gets the first text runs.
            /// </summary>
            /// <value>
            ///     The first text runs.
            /// </value>
            public IReadOnlyList<TextRun> First { get; }

            /// <summary>
            ///     Gets the second text runs.
            /// </summary>
            /// <value>
            ///     The second text runs.
            /// </value>
            public IReadOnlyList<TextRun> Second { get; }
        }
    }
}
