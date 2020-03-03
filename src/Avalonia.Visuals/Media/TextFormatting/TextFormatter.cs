using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utility;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a base class for text formatting.
    /// </summary>
    public abstract class TextFormatter
    {
        /// <summary>
        /// Gets the current <see cref="TextFormatter"/> that is used for non complex text formatting.
        /// </summary>
        public static TextFormatter Current
        {
            get
            {
                var current = AvaloniaLocator.Current.GetService<TextFormatter>();

                if (current != null)
                {
                    return current;
                }

                current = new SimpleTextFormatter();

                AvaloniaLocator.CurrentMutable.Bind<TextFormatter>().ToConstant(current);

                return current;
            }
        }

        /// <summary>
        /// Formats a text line.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first character index to start the text line from.</param>
        /// <param name="paragraphWidth">A <see cref="double"/> value that specifies the width of the paragraph that the line fills.</param>
        /// <param name="paragraphProperties">A <see cref="TextParagraphProperties"/> value that represents paragraph properties,
        /// such as TextWrapping, TextAlignment, or TextStyle.</param>
        /// <returns>The formatted line.</returns>
        public abstract TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties);

        /// <summary>
        /// Creates a text style run with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultStyle"></param>
        /// <returns>A list of text runs.</returns>
        protected TextStyleRun CreateShapableTextStyleRun(ReadOnlySlice<char> text, TextStyle defaultStyle)
        {
            var defaultTypeface = defaultStyle.TextFormat.Typeface;

            var currentTypeface = defaultTypeface;

            if (TryGetRunProperties(text, currentTypeface, defaultTypeface, out var count))
            {
                return new TextStyleRun(new TextPointer(text.Start, count), new TextStyle(currentTypeface,
                    defaultStyle.TextFormat.FontRenderingEmSize,
                    defaultStyle.Foreground, defaultStyle.TextDecorations));

            }

            var codepoint = Codepoint.ReadAt(text, count, out _);

            //ToDo: Fix FontFamily fallback
            currentTypeface =
                FontManager.Current.MatchCharacter(codepoint, defaultTypeface.Weight, defaultTypeface.Style);

            if (currentTypeface != null && TryGetRunProperties(text, currentTypeface, defaultTypeface, out count))
            {
                //Fallback found
                return new TextStyleRun(new TextPointer(text.Start, count), new TextStyle(currentTypeface,
                    defaultStyle.TextFormat.FontRenderingEmSize,
                    defaultStyle.Foreground, defaultStyle.TextDecorations));

            }

            // no fallback found
            currentTypeface = defaultTypeface;

            var glyphTypeface = currentTypeface.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.Current;

                if (!grapheme.FirstCodepoint.IsWhiteSpace && glyphTypeface.TryGetGlyph(grapheme.FirstCodepoint, out _))
                {
                    break;
                }

                count += grapheme.Text.Length;
            }

            return new TextStyleRun(new TextPointer(text.Start, count),
                new TextStyle(currentTypeface, defaultStyle.TextFormat.FontRenderingEmSize,
                    defaultStyle.Foreground, defaultStyle.TextDecorations));
        }

        /// <summary>
        /// Tries to get run properties.
        /// </summary>
        /// <param name="defaultTypeface"></param>
        /// <param name="text"></param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected bool TryGetRunProperties(ReadOnlySlice<char> text, Typeface typeface, Typeface defaultTypeface,
            out int count)
        {
            if (text.Length == 0)
            {
                count = 0;
                return false;
            }

            var isFallback = typeface != defaultTypeface;

            count = 0;
            var script = Script.Common;
            //var direction = BiDiClass.LeftToRight;

            var font = typeface.GlyphTypeface;
            var defaultFont = defaultTypeface.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.Current;

                var currentScript = grapheme.FirstCodepoint.Script;

                //var currentDirection = grapheme.FirstCodepoint.BiDiClass;

                //// ToDo: Implement BiDi algorithm
                //if (currentScript.HorizontalDirection != direction)
                //{
                //    if (!UnicodeUtility.IsWhiteSpace(grapheme.FirstCodepoint))
                //    {
                //        break;
                //    }
                //}

                if (currentScript != script)
                {
                    if (currentScript != Script.Inherited && currentScript != Script.Common)
                    {
                        if (script == Script.Inherited || script == Script.Common)
                        {
                            script = currentScript;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (isFallback)
                {
                    if (defaultFont.TryGetGlyph(grapheme.FirstCodepoint, out _))
                    {
                        break;
                    }
                }

                if (!font.TryGetGlyph(grapheme.FirstCodepoint, out _))
                {
                    if (!grapheme.FirstCodepoint.IsWhiteSpace)
                    {
                        break;
                    }
                }

                count += grapheme.Text.Length;
            }

            return count > 0;
        }
    }
}
