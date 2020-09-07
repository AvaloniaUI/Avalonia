using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a multi line text layout.
    /// </summary>
    public class TextLayout
    {
        private static readonly char[] s_empty = { '\u200B' };

        private readonly ReadOnlySlice<char> _text;
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly IReadOnlyList<ValueSpan<TextRunProperties>> _textStyleOverrides;
        private readonly TextTrimming _textTrimming;

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
        /// <param name="lineHeight">The height of each line of text.</param>
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
            double lineHeight = double.NaN,
            int maxLines = 0,
            IReadOnlyList<ValueSpan<TextRunProperties>> textStyleOverrides = null)
        {
            _text = string.IsNullOrEmpty(text) ?
                new ReadOnlySlice<char>() :
                new ReadOnlySlice<char>(text.AsMemory());

            _paragraphProperties =
                CreateTextParagraphProperties(typeface, fontSize, foreground, textAlignment, textWrapping,
                    textDecorations, lineHeight);

            _textTrimming = textTrimming;

            _textStyleOverrides = textStyleOverrides;

            LineHeight = lineHeight;

            MaxWidth = maxWidth;

            MaxHeight = maxHeight;

            MaxLines = maxLines;

            UpdateLayout();
        }

        /// <summary>
        /// Gets or sets the height of each line of text.
        /// </summary>
        /// <remarks>
        /// A value of NaN (equivalent to an attribute value of "Auto") indicates that the line height
        /// is determined automatically from the current font characteristics. The default is NaN.
        /// </remarks>
        public double LineHeight { get; }

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
        public int MaxLines { get; }

        /// <summary>
        /// Gets the text lines.
        /// </summary>
        /// <value>
        /// The text lines.
        /// </value>
        public IReadOnlyList<TextLine> TextLines { get; private set; }

        /// <summary>
        /// Gets the size of the layout.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public Size Size { get; private set; }

        /// <summary>
        /// Draws the text layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public void Draw(DrawingContext context, Point origin)
        {
            if (!TextLines.Any())
            {
                return;
            }

            var currentY = origin.Y;

            foreach (var textLine in TextLines)
            {
                var offsetX = TextLine.GetParagraphOffsetX(textLine.LineMetrics.Size.Width, Size.Width,
                    _paragraphProperties.TextAlignment);

                textLine.Draw(context, new Point(origin.X + offsetX, currentY));

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
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <returns></returns>
        private static TextParagraphProperties CreateTextParagraphProperties(Typeface typeface, double fontSize,
            IBrush foreground, TextAlignment textAlignment, TextWrapping textWrapping,
            TextDecorationCollection textDecorations, double lineHeight)
        {
            var textRunStyle = new GenericTextRunProperties(typeface, fontSize, textDecorations, foreground);

            return new GenericTextParagraphProperties(textRunStyle, textAlignment, textWrapping, lineHeight);
        }

        /// <summary>
        /// Updates the current bounds.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="width">The current width.</param>
        /// <param name="height">The current height.</param>
        private static void UpdateBounds(TextLine textLine, ref double width, ref double height)
        {
            if (width < textLine.LineMetrics.Size.Width)
            {
                width = textLine.LineMetrics.Size.Width;
            }

            height += textLine.LineMetrics.Size.Height;
        }

        /// <summary>
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        private TextLine CreateEmptyTextLine(int startingIndex)
        {
            var properties = _paragraphProperties.DefaultTextRunProperties;

            var glyphRun = TextShaper.Current.ShapeText(new ReadOnlySlice<char>(s_empty, startingIndex, 1),
                properties.Typeface, properties.FontRenderingEmSize, properties.CultureInfo);

            var textRuns = new List<ShapedTextCharacters>
            {
                new ShapedTextCharacters(glyphRun, _paragraphProperties.DefaultTextRunProperties)
            };

            return new TextLineImpl(textRuns,
                TextLineMetrics.Create(textRuns, new TextRange(startingIndex, 1), MaxWidth, _paragraphProperties));
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

                Size = new Size(0, textLine.LineMetrics.Size.Height);
            }
            else
            {
                var textLines = new List<TextLine>();

                double width = 0.0, height = 0.0;

                var currentPosition = 0;

                var textSource = new FormattedTextSource(_text,
                    _paragraphProperties.DefaultTextRunProperties, _textStyleOverrides);

                TextLine previousLine = null;

                while (currentPosition < _text.Length)
                {
                    var textLine = TextFormatter.Current.FormatLine(textSource, currentPosition, MaxWidth,
                        _paragraphProperties, previousLine?.TextLineBreak);

                    currentPosition += textLine.TextRange.Length;

                    if (textLines.Count > 0)
                    {
                        if (textLines.Count == MaxLines || !double.IsPositiveInfinity(MaxHeight) &&
                            height + textLine.LineMetrics.Size.Height > MaxHeight)
                        {
                            if (previousLine?.TextLineBreak != null && _textTrimming != TextTrimming.None)
                            {
                                var collapsedLine =
                                    previousLine.Collapse(GetCollapsingProperties(MaxWidth));

                                textLines[textLines.Count - 1] = collapsedLine;
                            }

                            break;
                        }
                    }

                    var hasOverflowed = textLine.LineMetrics.HasOverflowed;

                    if (hasOverflowed && _textTrimming != TextTrimming.None)
                    {
                        textLine = textLine.Collapse(GetCollapsingProperties(MaxWidth));
                    }

                    textLines.Add(textLine);

                    UpdateBounds(textLine, ref width, ref height);

                    previousLine = textLine;

                    if (currentPosition != _text.Length || textLine.TextLineBreak == null)
                    {
                        continue;
                    }

                    var emptyTextLine = CreateEmptyTextLine(currentPosition);

                    textLines.Add(emptyTextLine);
                }

                Size = new Size(width, height);

                TextLines = textLines;
            }
        }

        /// <summary>
        /// Gets the <see cref="TextCollapsingProperties"/> for current text trimming mode.
        /// </summary>
        /// <param name="width">The collapsing width.</param>
        /// <returns>The <see cref="TextCollapsingProperties"/>.</returns>
        private TextCollapsingProperties GetCollapsingProperties(double width)
        {
            return _textTrimming switch
            {
                TextTrimming.CharacterEllipsis => new TextTrailingCharacterEllipsis(width,
                    _paragraphProperties.DefaultTextRunProperties),
                TextTrimming.WordEllipsis => new TextTrailingWordEllipsis(width,
                    _paragraphProperties.DefaultTextRunProperties),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private readonly struct FormattedTextSource : ITextSource
        {
            private readonly ReadOnlySlice<char> _text;
            private readonly TextRunProperties _defaultProperties;
            private readonly IReadOnlyList<ValueSpan<TextRunProperties>> _textModifier;

            public FormattedTextSource(ReadOnlySlice<char> text, TextRunProperties defaultProperties,
                IReadOnlyList<ValueSpan<TextRunProperties>> textModifier)
            {
                _text = text;
                _defaultProperties = defaultProperties;
                _textModifier = textModifier;
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex > _text.End)
                {
                    return new TextEndOfLine();
                }

                var runText = _text.Skip(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfLine();
                }

                var textStyleRun = CreateTextStyleRun(runText, _defaultProperties, _textModifier);

                return new TextCharacters(runText.Take(textStyleRun.Length), textStyleRun.Value);
            }

            /// <summary>
            /// Creates a span of text run properties that has modifier applied.
            /// </summary>
            /// <param name="text">The text to create the properties for.</param>
            /// <param name="defaultProperties">The default text properties.</param>
            /// <param name="textModifier">The text properties modifier.</param>
            /// <returns>
            /// The created text style run.
            /// </returns>
            private static ValueSpan<TextRunProperties> CreateTextStyleRun(ReadOnlySlice<char> text,
                TextRunProperties defaultProperties, IReadOnlyList<ValueSpan<TextRunProperties>> textModifier)
            {
                if (textModifier == null || textModifier.Count == 0)
                {
                    return new ValueSpan<TextRunProperties>(text.Start, text.Length, defaultProperties);
                }

                var currentProperties = defaultProperties;

                var hasOverride = false;

                var i = 0;

                var length = 0;

                for (; i < textModifier.Count; i++)
                {
                    var propertiesOverride = textModifier[i];

                    var textRange = new TextRange(propertiesOverride.Start, propertiesOverride.Length);

                    if (textRange.End < text.Start)
                    {
                        continue;
                    }

                    if (textRange.Start > text.End)
                    {
                        length = text.Length;
                        break;
                    }

                    if (textRange.Start > text.Start)
                    {
                        if (propertiesOverride.Value != currentProperties)
                        {
                            length = Math.Min(Math.Abs(textRange.Start - text.Start), text.Length);

                            break;
                        }
                    }

                    length += Math.Min(text.Length - length, textRange.Length);

                    if (hasOverride)
                    {
                        continue;
                    }

                    hasOverride = true;

                    currentProperties = propertiesOverride.Value;
                }

                if (length < text.Length && i == textModifier.Count)
                {
                    if (currentProperties == defaultProperties)
                    {
                        length = text.Length;
                    }
                }

                if (length != text.Length)
                {
                    text = text.Take(length);
                }

                return new ValueSpan<TextRunProperties>(text.Start, length, currentProperties);
            }
        }
    }
}
