using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds text characters.
    /// </summary>
    public class TextCharacters : TextRun
    {
        public TextCharacters(ReadOnlySlice<char> text, TextRunProperties properties)
        {
            TextSourceLength = text.Length;
            Text = text;
            Properties = properties;
        }

        public TextCharacters(ReadOnlySlice<char> text, int offsetToFirstCharacter, int length,
            TextRunProperties properties)
        {
            Text = text.Skip(offsetToFirstCharacter).Take(length);
            TextSourceLength = length;
            Properties = properties;
        }

        /// <inheritdoc />
        public override int TextSourceLength { get; }

        /// <inheritdoc />
        public override ReadOnlySlice<char> Text { get; }

        /// <inheritdoc />
        public override TextRunProperties Properties { get; }

        /// <summary>
        /// Gets a list of <see cref="ShapeableTextCharacters"/>.
        /// </summary>
        /// <returns>The shapeable text characters.</returns>
        internal IReadOnlyList<ShapeableTextCharacters> GetShapeableCharacters(ReadOnlySlice<char> runText, sbyte biDiLevel,
            ref TextRunProperties? previousProperties)
        {
            var shapeableCharacters = new List<ShapeableTextCharacters>(2);

            while (!runText.IsEmpty)
            {
                var shapeableRun = CreateShapeableRun(runText, Properties, biDiLevel, ref previousProperties);

                shapeableCharacters.Add(shapeableRun);

                runText = runText.Skip(shapeableRun.Text.Length);

                previousProperties = shapeableRun.Properties;
            }

            return shapeableCharacters;
        }

        /// <summary>
        /// Creates a shapeable text run with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultProperties">The default text run properties.</param>
        /// <param name="biDiLevel">The bidi level of the run.</param>
        /// <param name="previousProperties"></param>
        /// <returns>A list of shapeable text runs.</returns>
        private static ShapeableTextCharacters CreateShapeableRun(ReadOnlySlice<char> text,
            TextRunProperties defaultProperties, sbyte biDiLevel, ref TextRunProperties? previousProperties)
        {
            var defaultTypeface = defaultProperties.Typeface;
            var currentTypeface = defaultTypeface;
            var previousTypeface = previousProperties?.Typeface;

            if (TryGetShapeableLength(text, currentTypeface, null, out var count, out var script))
            {
                return new ShapeableTextCharacters(text.Take(count), defaultProperties.WithTypeface(currentTypeface),
                    biDiLevel);
            }

            if (previousTypeface is not null)
            {
                if (TryGetShapeableLength(text, previousTypeface.Value, defaultTypeface, out count, out _))
                {
                    return new ShapeableTextCharacters(text.Take(count),
                        defaultProperties.WithTypeface(previousTypeface.Value), biDiLevel);
                }
            }

            var codepoint = Codepoint.ReplacementCodepoint;

            var codepointEnumerator = new CodepointEnumerator(text.Skip(count));

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
                FontManager.Current.TryMatchCharacter(codepoint, defaultTypeface.Style, defaultTypeface.Weight,
                    defaultTypeface.Stretch, defaultTypeface.FontFamily, defaultProperties.CultureInfo,
                    out currentTypeface);

            if (matchFound && TryGetShapeableLength(text, currentTypeface, defaultTypeface, out count, out _))
            {
                //Fallback found
                return new ShapeableTextCharacters(text.Take(count), defaultProperties.WithTypeface(currentTypeface),
                    biDiLevel);
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

            return new ShapeableTextCharacters(text.Take(count), defaultProperties, biDiLevel);
        }

        /// <summary>
        /// Tries to get a shapeable length that is supported by the specified typeface.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="defaultTypeface"></param>
        /// <param name="length">The shapeable length.</param>
        /// <param name="script"></param>
        /// <returns></returns>
        protected static bool TryGetShapeableLength(
            ReadOnlySlice<char> text,
            Typeface typeface,
            Typeface? defaultTypeface,
            out int length,
            out Script script)
        {
            length = 0;
            script = Script.Unknown;

            if (text.Length == 0)
            {
                return false;
            }

            var font = typeface.GlyphTypeface;
            var defaultFont = defaultTypeface?.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext())
            {
                var currentGrapheme = enumerator.Current;

                var currentScript = currentGrapheme.FirstCodepoint.Script;

                if (defaultFont != null && defaultFont.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
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

                length += currentGrapheme.Text.Length;
            }

            return length > 0;
        }
    }
}
