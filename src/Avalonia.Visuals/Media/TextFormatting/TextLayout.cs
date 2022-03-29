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
        private static readonly char[] s_empty = { ' ' };

        private readonly ReadOnlySlice<char> _text;
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly IReadOnlyList<ValueSpan<TextRunProperties>>? _textStyleOverrides;
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
        /// <param name="flowDirection">The text flow direction.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <param name="maxLines">The maximum number of text lines.</param>
        /// <param name="textStyleOverrides">The text style overrides.</param>
        public TextLayout(
            string? text,
            Typeface typeface,
            double fontSize,
            IBrush? foreground,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming? textTrimming = null,
            TextDecorationCollection? textDecorations = null,
            FlowDirection flowDirection = FlowDirection.LeftToRight,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity,
            double lineHeight = double.NaN,
            int maxLines = 0,
            IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides = null)
        {
            _text = string.IsNullOrEmpty(text) ?
                new ReadOnlySlice<char>() :
                new ReadOnlySlice<char>(text.AsMemory());

            _paragraphProperties =
                CreateTextParagraphProperties(typeface, fontSize, foreground, textAlignment, textWrapping,
                    textDecorations, flowDirection, lineHeight);

            _textTrimming = textTrimming ?? TextTrimming.None;

            _textStyleOverrides = textStyleOverrides;

            LineHeight = lineHeight;

            MaxWidth = maxWidth;

            MaxHeight = maxHeight;

            MaxLines = maxLines;

            TextLines = CreateTextLines();
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
        public void Draw(DrawingContext context, Point origin)
        {
            if (!TextLines.Any())
            {
                return;
            }

            var (currentX, currentY) = origin;

            foreach (var textLine in TextLines)
            {
                textLine.Draw(context, new Point(currentX + textLine.Start, currentY));

                currentY += textLine.Height;
            }
        }

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (TextLines.Count == 0)
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= _text.Length)
            {
                var lastLine = TextLines[TextLines.Count - 1];

                var lineX = lastLine.Width;

                var lineY = Bounds.Bottom - lastLine.Height;

                return new Rect(lineX, lineY, 0, lastLine.Height);
            }

            var currentY = 0.0;

            foreach (var textLine in TextLines)
            {
                if (textLine.TextRange.End < textPosition)
                {
                    currentY += textLine.Height;

                    continue;
                }

                var characterHit = new CharacterHit(textPosition);

                var startX = textLine.GetDistanceFromCharacterHit(characterHit);

                var nextCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);

                var endX = textLine.GetDistanceFromCharacterHit(nextCharacterHit);

                return new Rect(startX, currentY, endX - startX, textLine.Height);
            }

            return new Rect();
        }

        public IEnumerable<Rect> HitTestTextRange(int start, int length)
        {
            if (start + length <= 0)
            {
                return Array.Empty<Rect>();
            }

            var result = new List<Rect>(TextLines.Count);
            
            var currentY = 0d;

            foreach (var textLine in TextLines)
            {
                //Current line isn't covered.
                if (textLine.TextRange.Start + textLine.TextRange.Length <= start)
                {
                    currentY += textLine.Height;

                    continue;
                }

                var textBounds = textLine.GetTextBounds(start, length);

                foreach (var bounds in textBounds)
                {
                    Rect? last = result.Count > 0 ? result[result.Count - 1] : null;

                    if (last.HasValue && MathUtilities.AreClose(last.Value.Right, bounds.Rectangle.Left) && MathUtilities.AreClose(last.Value.Top, currentY))
                    {
                        result[result.Count - 1] = last.Value.WithWidth(last.Value.Width + bounds.Rectangle.Width);
                    }
                    else
                    {
                        result.Add(bounds.Rectangle.WithY(currentY));
                    }                  
                }

                if(textLine.TextRange.Start + textLine.TextRange.Length >= start + length)
                {
                    break;
                }

                currentY += textLine.Height;
            }

            return result;
        }

        public TextHitTestResult HitTestPoint(in Point point)
        {
            var currentY = 0d;

            var lineIndex = 0;
            TextLine? currentLine = null;
            CharacterHit characterHit;

            for (; lineIndex < TextLines.Count; lineIndex++)
            {
                currentLine = TextLines[lineIndex];

                if (currentY + currentLine.Height > point.Y)
                {
                    characterHit = currentLine.GetCharacterHitFromDistance(point.X);

                    return GetHitTestResult(currentLine, characterHit, point);
                }

                currentY += currentLine.Height;
            }

            if (currentLine is null)
            {
                return new TextHitTestResult();
            }

            characterHit = currentLine.GetCharacterHitFromDistance(point.X);

            return GetHitTestResult(currentLine, characterHit, point);
        }

        
        public int GetLineIndexFromCharacterIndex(int charIndex, bool trailingEdge)
        {
            if (charIndex < 0)
            {
                return 0;
            }

            if (charIndex > _text.Length)
            {
                return TextLines.Count - 1;
            }

            for (var index = 0; index < TextLines.Count; index++)
            {
                var textLine = TextLines[index];

                if (textLine.TextRange.Start + textLine.TextRange.Length < charIndex)
                {
                    continue;
                }

                if (charIndex >= textLine.TextRange.Start && charIndex <= textLine.TextRange.End + (trailingEdge ? 1 : 0))
                {
                    return index;
                }
            }

            return TextLines.Count - 1;
        }

        private TextHitTestResult GetHitTestResult(TextLine textLine, CharacterHit characterHit, Point point)
        {
            var (x, y) = point;

            var lastTrailingIndex = textLine.TextRange.Start + textLine.TextRange.Length;

            var isInside = x >= 0 && x <= textLine.Width && y >= 0 && y <= textLine.Height;

            if (x >= textLine.Width && textLine.TextRange.Length > 0 && textLine.NewLineLength > 0)
            {
                lastTrailingIndex -= textLine.NewLineLength;
            }

            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var isTrailing = lastTrailingIndex == textPosition && characterHit.TrailingLength > 0 ||
                             y > Bounds.Bottom;

            if (textPosition == textLine.TextRange.Start + textLine.TextRange.Length)
            {
                textPosition -= textLine.NewLineLength;
            }

            if (textLine.NewLineLength > 0 && textPosition + textLine.NewLineLength ==
                characterHit.FirstCharacterIndex + characterHit.TrailingLength)
            {
                characterHit = new CharacterHit(characterHit.FirstCharacterIndex);
            }

            return new TextHitTestResult(characterHit, textPosition, isInside, isTrailing);
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
        /// <param name="flowDirection">The text flow direction.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <returns></returns>
        private static TextParagraphProperties CreateTextParagraphProperties(Typeface typeface, double fontSize,
            IBrush? foreground, TextAlignment textAlignment, TextWrapping textWrapping,
            TextDecorationCollection? textDecorations, FlowDirection flowDirection, double lineHeight)
        {
            var textRunStyle = new GenericTextRunProperties(typeface, fontSize, textDecorations, foreground);

            return new GenericTextParagraphProperties(flowDirection, textAlignment, true, false,
                textRunStyle, textWrapping, lineHeight, 0);
        }

        /// <summary>
        /// Updates the current bounds.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="left">The current left.</param>
        /// <param name="width">The current width.</param>
        /// <param name="height">The current height.</param>
        private static void UpdateBounds(TextLine textLine,ref double left,  ref double width, ref double height)
        {
            var lineWidth = textLine.WidthIncludingTrailingWhitespace;

            if (width < lineWidth)
            {
                width = lineWidth;
            }

            if (left > textLine.Start)
            {
                left = textLine.Start;
            }

            height += textLine.Height;
        }

        /// <summary>
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        private TextLine CreateEmptyTextLine(int startingIndex)
        {
            var flowDirection = _paragraphProperties.FlowDirection;
            var properties = _paragraphProperties.DefaultTextRunProperties;
            var glyphTypeface = properties.Typeface.GlyphTypeface;
            var text = new ReadOnlySlice<char>(s_empty, startingIndex, 1);
            var glyph = glyphTypeface.GetGlyph(s_empty[0]);
            var glyphInfos = new[] { new GlyphInfo(glyph, startingIndex) };

            var shapedBuffer = new ShapedBuffer(text, glyphInfos, glyphTypeface, properties.FontRenderingEmSize,
                (sbyte)flowDirection);

            var textRuns = new List<DrawableTextRun> { new ShapedTextCharacters(shapedBuffer, properties) };

            var textRange = new TextRange(startingIndex, 1);

            return new TextLineImpl(textRuns, textRange, MaxWidth, _paragraphProperties, flowDirection).FinalizeLine();
        }

        private IReadOnlyList<TextLine> CreateTextLines()
        {
            if (_text.IsEmpty || MathUtilities.IsZero(MaxWidth) || MathUtilities.IsZero(MaxHeight))
            {
                var textLine = CreateEmptyTextLine(0);

                Bounds = new Rect(0,0,0, textLine.Height);

                return new List<TextLine> { textLine };
            }

            var textLines = new List<TextLine>();

            double left = double.PositiveInfinity, width = 0.0, height = 0.0;

            var currentPosition = 0;

            var textSource = new FormattedTextSource(_text,
                _paragraphProperties.DefaultTextRunProperties, _textStyleOverrides);

            TextLine? previousLine = null;

            while (currentPosition < _text.Length)
            {
                var textLine = TextFormatter.Current.FormatLine(textSource, currentPosition, MaxWidth,
                    _paragraphProperties, previousLine?.TextLineBreak);

#if DEBUG
                if (textLine.TextRange.Length == 0)
                {
                    throw new InvalidOperationException($"{nameof(textLine)} should not be empty.");
                }
#endif

                currentPosition += textLine.TextRange.Length;
                
                //Fulfill max height constraint
                if (textLines.Count > 0 && !double.IsPositiveInfinity(MaxHeight) && height + textLine.Height > MaxHeight)
                {
                    if (previousLine?.TextLineBreak != null && _textTrimming != TextTrimming.None)
                    {
                        var collapsedLine =
                            previousLine.Collapse(GetCollapsingProperties(MaxWidth));

                        textLines[textLines.Count - 1] = collapsedLine;
                    }

                    break;
                }

                var hasOverflowed = textLine.HasOverflowed;

                if (hasOverflowed && _textTrimming != TextTrimming.None)
                {
                    textLine = textLine.Collapse(GetCollapsingProperties(MaxWidth));
                }

                textLines.Add(textLine);

                UpdateBounds(textLine, ref left, ref width, ref height);

                previousLine = textLine;

                //Fulfill max lines constraint
                if (MaxLines > 0 && textLines.Count >= MaxLines)
                {
                    break;
                }
                
                if (currentPosition != _text.Length || textLine.NewLineLength <= 0)
                {
                    continue;
                }

                var emptyTextLine = CreateEmptyTextLine(currentPosition);

                textLines.Add(emptyTextLine);

                UpdateBounds(emptyTextLine,ref left, ref width, ref height);
            }

            Bounds = new Rect(left, 0, width, height);

            return textLines;
        }

        /// <summary>
        /// Gets the <see cref="TextCollapsingProperties"/> for current text trimming mode.
        /// </summary>
        /// <param name="width">The collapsing width.</param>
        /// <returns>The <see cref="TextCollapsingProperties"/>.</returns>
        private TextCollapsingProperties GetCollapsingProperties(double width)
        {
            return _textTrimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(width, _paragraphProperties.DefaultTextRunProperties));
        }
    }
}
