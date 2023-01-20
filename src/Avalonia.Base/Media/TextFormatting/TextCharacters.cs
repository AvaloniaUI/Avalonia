using System;
using Avalonia.Media.TextFormatting.Unicode;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds text characters.
    /// </summary>
    public class TextCharacters : TextRun
    {
        /// <summary>
        /// Constructs a run for text content from a string.
        /// </summary>
        public TextCharacters(string text, TextRunProperties textRunProperties)
            : this(text.AsMemory(), textRunProperties)
        {
        }

        /// <summary>
        /// Constructs a run for text content from a memory region.
        /// </summary>
        public TextCharacters(ReadOnlyMemory<char> text, TextRunProperties textRunProperties)
        {
            if (textRunProperties.FontRenderingEmSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(textRunProperties), textRunProperties.FontRenderingEmSize,
                    $"Invalid {nameof(TextRunProperties.FontRenderingEmSize)}");
            }

            Text = text;
            Properties = textRunProperties;
        }

        /// <inheritdoc />
        public override int Length
            => Text.Length;

        /// <inheritdoc />
        public override ReadOnlyMemory<char> Text { get; }

        /// <inheritdoc />
        public override TextRunProperties Properties { get; }

        /// <summary>
        /// Gets a list of <see cref="UnshapedTextRun"/>.
        /// </summary>
        /// <returns>The shapeable text characters.</returns>
        internal void GetShapeableCharacters(ReadOnlyMemory<char> text, sbyte biDiLevel,
            FontManager fontManager, ref TextRunProperties? previousProperties, RentedList<TextRun> results)
        {
            var properties = Properties;

            while (!text.IsEmpty)
            {
                var shapeableRun = CreateShapeableRun(text, properties, biDiLevel, fontManager, ref previousProperties);

                results.Add(shapeableRun);

                text = text.Slice(shapeableRun.Length);

                previousProperties = shapeableRun.Properties;
            }
        }

        /// <summary>
        /// Creates a shapeable text run with unique properties.
        /// </summary>
        /// <param name="text">The characters to create text runs from.</param>
        /// <param name="defaultProperties">The default text run properties.</param>
        /// <param name="biDiLevel">The bidi level of the run.</param>
        /// <param name="previousProperties"></param>
        /// <returns>A list of shapeable text runs.</returns>
        private static UnshapedTextRun CreateShapeableRun(ReadOnlyMemory<char> text,
            TextRunProperties defaultProperties, sbyte biDiLevel, FontManager fontManager,
            ref TextRunProperties? previousProperties)
        {
            var defaultTypeface = defaultProperties.Typeface;
            var currentTypeface = defaultTypeface;
            var previousTypeface = previousProperties?.Typeface;
            var textSpan = text.Span;

            if (TryGetShapeableLength(textSpan, currentTypeface, null, out var count, out var script))
            {
                if (script == Script.Common && previousTypeface is not null)
                {
                    if (TryGetShapeableLength(textSpan, previousTypeface.Value, null, out var fallbackCount, out _))
                    {
                        return new UnshapedTextRun(text.Slice(0, fallbackCount),
                            defaultProperties.WithTypeface(previousTypeface.Value), biDiLevel);
                    }
                }

                return new UnshapedTextRun(text.Slice(0, count), defaultProperties.WithTypeface(currentTypeface),
                    biDiLevel);
            }

            if (previousTypeface is not null)
            {
                if (TryGetShapeableLength(textSpan, previousTypeface.Value, defaultTypeface, out count, out _))
                {
                    return new UnshapedTextRun(text.Slice(0, count),
                        defaultProperties.WithTypeface(previousTypeface.Value), biDiLevel);
                }
            }

            var codepoint = Codepoint.ReplacementCodepoint;

            var codepointEnumerator = new CodepointEnumerator(text.Slice(count).Span);

            while (codepointEnumerator.MoveNext())
            {
                if (codepointEnumerator.Current.IsWhiteSpace)
                {
                    continue;
                }

                codepoint = codepointEnumerator.Current;

                break;
            }

            //ToDo: Fix FontFamily fallback
            var matchFound =
                fontManager.TryMatchCharacter(codepoint, defaultTypeface.Style, defaultTypeface.Weight,
                    defaultTypeface.Stretch, defaultTypeface.FontFamily, defaultProperties.CultureInfo,
                    out currentTypeface);

            if (matchFound && TryGetShapeableLength(textSpan, currentTypeface, defaultTypeface, out count, out _))
            {
                //Fallback found
                return new UnshapedTextRun(text.Slice(0, count), defaultProperties.WithTypeface(currentTypeface),
                    biDiLevel);
            }

            // no fallback found
            currentTypeface = defaultTypeface;

            var glyphTypeface = currentTypeface.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(textSpan);

            while (enumerator.MoveNext(out var grapheme))
            {
                if (!grapheme.FirstCodepoint.IsWhiteSpace && glyphTypeface.TryGetGlyph(grapheme.FirstCodepoint, out _))
                {
                    break;
                }

                count += grapheme.Length;
            }

            return new UnshapedTextRun(text.Slice(0, count), defaultProperties, biDiLevel);
        }

        /// <summary>
        /// Tries to get a shapeable length that is supported by the specified typeface.
        /// </summary>
        /// <param name="text">The characters to shape.</param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="defaultTypeface"></param>
        /// <param name="length">The shapeable length.</param>
        /// <param name="script"></param>
        /// <returns></returns>
        internal static bool TryGetShapeableLength(
            ReadOnlySpan<char> text,
            Typeface typeface,
            Typeface? defaultTypeface,
            out int length,
            out Script script)
        {
            length = 0;
            script = Script.Unknown;

            if (text.IsEmpty)
            {
                return false;
            }

            var font = typeface.GlyphTypeface;
            var defaultFont = defaultTypeface?.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext(out var currentGrapheme))
            {
                var currentScript = currentGrapheme.FirstCodepoint.Script;

                if (!currentGrapheme.FirstCodepoint.IsWhiteSpace && defaultFont != null && defaultFont.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                {
                    break;
                }

                //Stop at the first missing glyph
                if (!currentGrapheme.FirstCodepoint.IsBreakChar && !font.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                {
                    break;
                }

                if (currentScript != script)
                {
                    if (script is Script.Unknown || currentScript != Script.Common &&
                        script is Script.Common or Script.Inherited)
                    {
                        script = currentScript;
                    }
                    else
                    {
                        if (currentScript != Script.Inherited && currentScript != Script.Common)
                        {
                            break;
                        }
                    }
                }

                length += currentGrapheme.Length;
            }

            return length > 0;
        }
    }
}
