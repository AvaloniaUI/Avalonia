using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Utility;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a multi line text layout.
    /// </summary>
    public class TextLayout
    {
        private static readonly ReadOnlySlice<char> s_empty = new ReadOnlySlice<char>(new[] { '\u200B' });

        private readonly ReadOnlySlice<char> _text;
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly IReadOnlyList<TextStyleRun> _textStyleOverrides;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextLayout" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="maxLines">The maximum number of text lines.</param>
        /// <param name="textStyleOverrides">The text style overrides.</param>
        public TextLayout(
            string text,
            Typeface typeface,
            double fontSize,
            IBrush foreground,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming textTrimming = TextTrimming.None,
            TextDecorationCollection textDecorations = null,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity,
            int maxLines = 0,
            IReadOnlyList<TextStyleRun> textStyleOverrides = null)
        {
            _text = string.IsNullOrEmpty(text) ?
                new ReadOnlySlice<char>() :
                new ReadOnlySlice<char>(text.AsMemory());

            _paragraphProperties =
                CreateTextParagraphProperties(typeface, fontSize, foreground, textAlignment, textWrapping, textTrimming, textDecorations?.ToImmutable());

            _textStyleOverrides = textStyleOverrides;

            MaxWidth = maxWidth;

            MaxHeight = maxHeight;

            MaxLines = maxLines;

            UpdateLayout();
        }

        /// <summary>
        /// Gets the maximum width.
        /// </summary>
        public double MaxWidth { get; }


        /// <summary>
        /// Gets the maximum height.
        /// </summary>
        public double MaxHeight { get; }


        /// <summary>
        /// Gets the maximum number of text lines.
        /// </summary>
        public double MaxLines { get; }

        /// <summary>
        /// Gets the text lines.
        /// </summary>
        /// <value>
        /// The text lines.
        /// </value>
        public IReadOnlyList<TextLine> TextLines { get; private set; }

        /// <summary>
        /// Gets the bounds of the layout.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public Rect Bounds { get; private set; }

        /// <summary>
        /// Draws the text layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public void Draw(IDrawingContextImpl context, Point origin)
        {
            if (!TextLines.Any())
            {
                return;
            }

            var currentY = origin.Y;

            foreach (var textLine in TextLines)
            {
                textLine.Draw(context, new Point(origin.X, currentY));

                currentY += textLine.LineMetrics.Size.Height;
            }
        }

        /// <summary>
        /// Creates the default <see cref="TextParagraphProperties"/> that are used by the <see cref="TextFormatter"/>.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <returns></returns>
        private static TextParagraphProperties CreateTextParagraphProperties(Typeface typeface, double fontSize,
            IBrush foreground, TextAlignment textAlignment, TextWrapping textWrapping, TextTrimming textTrimming,
            ImmutableTextDecoration[] textDecorations)
        {
            var textRunStyle = new TextStyle(typeface, fontSize, foreground, textDecorations);

            return new TextParagraphProperties(textRunStyle, textAlignment, textWrapping, textTrimming);
        }

        /// <summary>
        /// Updates the current bounds.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="bottom">The bottom.</param>
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
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        private TextLine CreateEmptyTextLine(int startingIndex)
        {
            var textFormat = _paragraphProperties.DefaultTextStyle.TextFormat;

            var glyphRun = TextShaper.Current.ShapeText(s_empty, textFormat);

            var textRuns = new[] { new ShapedTextRun(glyphRun, _paragraphProperties.DefaultTextStyle) };

            return new SimpleTextLine(new TextPointer(startingIndex, 0), textRuns,
                TextLineMetrics.Create(textRuns, MaxWidth, _paragraphProperties.TextAlignment));
        }

        /// <summary>
        /// Updates the layout and applies specified text style overrides.
        /// </summary>
        private void UpdateLayout()
        {
            if (_text.IsEmpty || MathUtilities.IsZero(MaxWidth) || MathUtilities.IsZero(MaxHeight))
            {
                var textLine = CreateEmptyTextLine(0);

                TextLines = new List<TextLine> { textLine };

                Bounds = new Rect(textLine.LineMetrics.BaselineOrigin.X, 0, 0, textLine.LineMetrics.Size.Height);
            }
            else
            {
                var textLines = new List<TextLine>();

                double left = 0.0, right = 0.0, bottom = 0.0;

                var lineBreaker = new LineBreakEnumerator(_text);

                var currentPosition = 0;

                while (currentPosition < _text.Length && (MaxLines == 0 || textLines.Count < MaxLines))
                {
                    int length;

                    if (lineBreaker.MoveNext())
                    {
                        if (!lineBreaker.Current.Required)
                        {
                            continue;
                        }

                        length = lineBreaker.Current.PositionWrap - currentPosition;

                        if (currentPosition + length < _text.Length)
                        {
                            //The line breaker isn't treating \n\r as a pair so we have to fix that here.
                            if (_text[lineBreaker.Current.PositionMeasure] == '\n'
                             && _text[lineBreaker.Current.PositionWrap] == '\r')
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

                    while (remainingLength > 0 && (MaxLines == 0 || textLines.Count < MaxLines))
                    {
                        var textSlice = _text.AsSlice(currentPosition, remainingLength);

                        var textSource = new FormattedTextSource(textSlice, _paragraphProperties.DefaultTextStyle, _textStyleOverrides);

                        var textLine = TextFormatter.Current.FormatLine(textSource, 0, MaxWidth, _paragraphProperties);

                        UpdateBounds(textLine, ref left, ref right, ref bottom);

                        textLines.Add(textLine);

                        if (!double.IsPositiveInfinity(MaxHeight) && bottom + textLine.LineMetrics.Size.Height > MaxHeight)
                        {
                            currentPosition = _text.Length;
                            break;
                        }

                        if (_paragraphProperties.TextTrimming != TextTrimming.None)
                        {
                            currentPosition += remainingLength;

                            break;
                        }

                        remainingLength -= textLine.Text.Length;

                        currentPosition += textLine.Text.Length;
                    }
                }

                if (lineBreaker.Current.Required && currentPosition == _text.Length)
                {
                    var emptyTextLine = CreateEmptyTextLine(currentPosition);

                    UpdateBounds(emptyTextLine, ref left, ref right, ref bottom);

                    textLines.Add(emptyTextLine);
                }

                Bounds = new Rect(left, 0, right, bottom);

                TextLines = textLines;
            }
        }

        private struct FormattedTextSource : ITextSource
        {
            private readonly ReadOnlySlice<char> _text;
            private readonly TextStyle _defaultStyle;
            private readonly IReadOnlyList<TextStyleRun> _textStyleOverrides;

            public FormattedTextSource(ReadOnlySlice<char> text, TextStyle defaultStyle,
                IReadOnlyList<TextStyleRun> textStyleOverrides)
            {
                _text = text;
                _defaultStyle = defaultStyle;
                _textStyleOverrides = textStyleOverrides;
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                var runText = _text.Skip(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfLine();
                }

                var textStyleRun = CreateTextStyleRunWithOverride(runText, _defaultStyle, _textStyleOverrides);

                return new TextCharacters(runText.Take(textStyleRun.TextPointer.Length), textStyleRun.Style);
            }

            /// <summary>
            /// Creates a text style run that has overrides applied. Only overrides with equal TextStyle.
            /// If optimizeForShaping is <c>true</c> Foreground is ignored.
            /// </summary>
            /// <param name="text">The text to create the run for.</param>
            /// <param name="defaultTextStyle">The default text style for segments that don't have an override.</param>
            /// <param name="textStyleOverrides">The text style overrides.</param>
            /// <returns>
            /// The created text style run.
            /// </returns>
            private static TextStyleRun CreateTextStyleRunWithOverride(ReadOnlySlice<char> text,
                TextStyle defaultTextStyle, IReadOnlyList<TextStyleRun> textStyleOverrides)
            {
                if(textStyleOverrides == null || textStyleOverrides.Count == 0)
                {
                    return new TextStyleRun(new TextPointer(text.Start, text.Length), defaultTextStyle);
                }

                var currentTextStyle = defaultTextStyle;

                var hasOverride = false;

                var i = 0;

                var length = 0;

                for (; i < textStyleOverrides.Count; i++)
                {
                    var styleOverride = textStyleOverrides[i];

                    var textPointer = styleOverride.TextPointer;

                    if (textPointer.End < text.Start)
                    {
                        continue;
                    }

                    if (textPointer.Start > text.End)
                    {
                        length = text.Length;
                        break;
                    }

                    if (textPointer.Start > text.Start)
                    {
                        if (styleOverride.Style.TextFormat != currentTextStyle.TextFormat ||
                            !currentTextStyle.Foreground.Equals(styleOverride.Style.Foreground))
                        {
                            length = Math.Min(Math.Abs(textPointer.Start - text.Start), text.Length);

                            break;
                        }
                    }

                    length += Math.Min(text.Length - length, textPointer.Length);

                    if (hasOverride)
                    {
                        continue;
                    }

                    hasOverride = true;

                    currentTextStyle = styleOverride.Style;
                }

                if (length < text.Length && i == textStyleOverrides.Count)
                {
                    if (currentTextStyle.Foreground.Equals(defaultTextStyle.Foreground) &&
                        currentTextStyle.TextFormat == defaultTextStyle.TextFormat)
                    {
                        length = text.Length;
                    }
                }

                if (length != text.Length)
                {
                    text = text.Take(length);
                }

                return new TextStyleRun(new TextPointer(text.Start, length), currentTextStyle);
            }
        }
    }
}
