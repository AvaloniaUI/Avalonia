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
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly Size _constraint;
        private readonly ReadOnlySlice<char> _text;
        private readonly double _paragraphWidth;

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
            _text = string.IsNullOrEmpty(text) ?
                new ReadOnlySlice<char>() :
                new ReadOnlySlice<char>(text.AsMemory());

            _paragraphProperties =
                CreateTextParagraphProperties(_text, typeface, fontSize, textAlignment, textWrapping, textTrimming);

            _constraint = constraint;

            _paragraphWidth = _constraint.Width > 0 && !double.IsPositiveInfinity(_constraint.Width) ?
                _constraint.Width :
                Bounds.Width;

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

        private static TextParagraphProperties CreateTextParagraphProperties(ReadOnlySlice<char> text, Typeface typeface,
            double fontSize, TextAlignment textAlignment, TextWrapping textWrapping, TextTrimming textTrimming)
        {
            var textRunStyle = new TextRunStyle(typeface, fontSize, null);

            return new TextParagraphProperties(textRunStyle, textAlignment, textWrapping, textTrimming);
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

                if (currentTextRun.TextPointer.Length == 0)
                {
                    continue;
                }

                if (currentPosition + currentTextRun.TextPointer.Length - 1 < span.StartIndex)
                {
                    currentPosition += currentTextRun.TextPointer.Length;

                    continue;
                }

                if (currentPosition == span.StartIndex + appliedLength)
                {
                    var splitLength = span.Length - appliedLength;

                    if (splitLength >= currentTextRun.TextPointer.Length)
                    {
                        // Apply to the whole run 
                        textRuns.RemoveAt(runIndex);

                        var updatedTextRun = ApplyTextStyleSpan(span, currentTextRun);

                        appliedLength += updatedTextRun.TextPointer.Length;

                        textRuns.Insert(runIndex, updatedTextRun);
                    }
                    else
                    {
                        // Apply at start of the run 
                        var start = currentTextRun.Split(splitLength);

                        textRuns.RemoveAt(runIndex);

                        var updatedTextRun = ApplyTextStyleSpan(span, start.First);

                        appliedLength += updatedTextRun.TextPointer.Length;

                        textRuns.Insert(runIndex, updatedTextRun);

                        runIndex++;

                        textRuns.Insert(runIndex, start.Second);
                    }
                }
                else
                {
                    var splitLength = Math.Min(
                        span.StartIndex + appliedLength - currentPosition,
                        currentTextRun.TextPointer.Length);

                    if (splitLength > 0)
                    {
                        var start = currentTextRun.Split(splitLength);

                        if (splitLength + span.Length - appliedLength >= currentTextRun.TextPointer.Length)
                        {
                            // Apply at the end of the run
                            textRuns.RemoveAt(runIndex);

                            textRuns.Insert(runIndex, start.First);

                            runIndex++;

                            var updatedTextRun = ApplyTextStyleSpan(span, start.Second);

                            appliedLength += updatedTextRun.TextPointer.Length;

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

                            appliedLength += updatedTextRun.TextPointer.Length;

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

                currentPosition += currentTextRun.TextPointer.Length;
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
            var textPointer = textRun.TextPointer;

            var runStyle = textRun.Style;

            var drawingEffect = span.Foreground?.ToImmutable() ?? runStyle.Foreground;

            var textFormat = new TextFormat(span.Typeface ?? runStyle.TextFormat.Typeface,
                span.FontSize ?? runStyle.TextFormat.FontRenderingEmSize);

            return new TextRunProperties(textPointer, new TextRunStyle(textFormat, drawingEffect));
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
        ///     Creates a empty text line.
        /// </summary>
        /// <returns></returns>
        private TextLine CreateEmptyTextLine(int startingIndex)
        {
            var xOrigin = TextLine.GetParagraphOffsetX(0, _paragraphWidth, _paragraphProperties.TextAlignment);

            var textFormat = _paragraphProperties.DefaultTextRunStyle.TextFormat;

            var ascent = textFormat.FontMetrics.Ascent;

            var descent = textFormat.FontMetrics.Descent;

            var lineGap = textFormat.FontMetrics.LineGap;

            return new TextLine(
                new ReadOnlySlice<char>(new ReadOnlyMemory<char>(), startingIndex, 0),
                new List<TextRun>(),
                new TextLineMetrics(0, xOrigin, ascent, descent, lineGap));
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

                Bounds = new Rect(textLine.LineMetrics.BaselineOrigin.X, 0, 0, textLine.LineMetrics.Size.Height);
            }
            else
            {
                var runProperties = TextFormatter.CreateTextRuns(_text, _paragraphProperties.DefaultTextRunStyle);

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
                            //The line breaker isn't treating \n\r as a pair so we have to fix that here.
                            if (_text[lineBreaker.CurrentBreak.PositionMeasure] == '\n'
                             && _text[lineBreaker.CurrentBreak.PositionWrap] == '\r')
                            {
                                length++;
                            }
                        }
                    }
                    else
                    {
                        length = _text.Length - currentPosition;
                    }

                    var remainingLength = length;

                    while (remainingLength > 0)
                    {
                        var textSlice = _text.AsSlice(currentPosition, remainingLength);

                        var textLine = TextFormatter.FormatLine(textSlice, _paragraphWidth, _paragraphProperties,
                            runProperties);

                        UpdateBounds(textLine, ref left, ref right, ref bottom);

                        textLines.Add(textLine);

                        remainingLength -= textLine.Text.Length;

                        currentPosition += textLine.Text.Length;
                    }

                    if (lineBreaker.CurrentBreak.Required && currentPosition == _text.Length)
                    {
                        var emptyTextLine = CreateEmptyTextLine(currentPosition);

                        UpdateBounds(emptyTextLine, ref left, ref right, ref bottom);

                        textLines.Add(emptyTextLine);

                        break;
                    }

                    if (!double.IsPositiveInfinity(_constraint.Height) && _constraint.Height < Bounds.Height)
                    {
                        break;
                    }
                }

                Bounds = new Rect(left, 0, right, bottom);

                TextLines = textLines;
            }
        }
    }
}
