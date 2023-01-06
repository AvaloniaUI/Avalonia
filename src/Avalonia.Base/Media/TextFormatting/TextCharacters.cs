using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds text characters.
    /// </summary>
    public class TextCharacters : TextRun
    {
        /// <summary>
        /// Construct a run of text content from character array
        /// </summary>
        public TextCharacters(
            char[] characterArray,
            int offsetToFirstChar,
            int length,
            TextRunProperties textRunProperties
            ) :
            this(
                new CharacterBufferReference(characterArray, offsetToFirstChar),
                length,
                textRunProperties
                )
        { }


        /// <summary>
        /// Construct a run for text content from string 
        /// </summary>
        public TextCharacters(
            string characterString,
            TextRunProperties textRunProperties
            ) :
            this(
                characterString,
                0,  // offsetToFirstChar
                (characterString == null) ? 0 : characterString.Length,
                textRunProperties
                )
        { }

        /// <summary>
        /// Construct a run for text content from string
        /// </summary>
        public TextCharacters(
            string characterString,
            int offsetToFirstChar,
            int length,
            TextRunProperties textRunProperties
            ) :
            this(
                new CharacterBufferReference(characterString, offsetToFirstChar),
                length,
                textRunProperties
                )
        { }

        /// <summary>
        /// Internal constructor of TextContent
        /// </summary>
        public TextCharacters(
            CharacterBufferReference characterBufferReference,
            int length,
            TextRunProperties textRunProperties
            )
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length", "ParameterMustBeGreaterThanZero");
            }

            if (textRunProperties.FontRenderingEmSize <= 0)
            {
                throw new ArgumentOutOfRangeException("textRunProperties.FontRenderingEmSize", "ParameterMustBeGreaterThanZero");
            }

            CharacterBufferReference = characterBufferReference;
            Length = length;
            Properties = textRunProperties;
        }

        /// <inheritdoc />
        public override int Length { get; }

        /// <inheritdoc />
        public override CharacterBufferReference CharacterBufferReference { get; }

        /// <inheritdoc />
        public override TextRunProperties Properties { get; }

        /// <summary>
        /// Gets a list of <see cref="UnshapedTextRun"/>.
        /// </summary>
        /// <returns>The shapeable text characters.</returns>
        internal IReadOnlyList<UnshapedTextRun> GetShapeableCharacters(CharacterBufferRange characterBufferRange, sbyte biDiLevel, ref TextRunProperties? previousProperties)
        {
            var shapeableCharacters = new List<UnshapedTextRun>(2);

            while (characterBufferRange.Length > 0)
            {
                var shapeableRun = CreateShapeableRun(characterBufferRange, Properties, biDiLevel, ref previousProperties);

                shapeableCharacters.Add(shapeableRun);

                characterBufferRange = characterBufferRange.Skip(shapeableRun.Length);

                previousProperties = shapeableRun.Properties;
            }

            return shapeableCharacters;
        }

        /// <summary>
        /// Creates a shapeable text run with unique properties.
        /// </summary>
        /// <param name="characterBufferRange">The character buffer range to create text runs from.</param>
        /// <param name="defaultProperties">The default text run properties.</param>
        /// <param name="biDiLevel">The bidi level of the run.</param>
        /// <param name="previousProperties"></param>
        /// <returns>A list of shapeable text runs.</returns>
        private static UnshapedTextRun CreateShapeableRun(CharacterBufferRange characterBufferRange,
            TextRunProperties defaultProperties, sbyte biDiLevel, ref TextRunProperties? previousProperties)
        {
            var defaultTypeface = defaultProperties.Typeface;
            var currentTypeface = defaultTypeface;
            var previousTypeface = previousProperties?.Typeface;

            if (TryGetShapeableLength(characterBufferRange, currentTypeface, null, out var count, out var script))
            {
                if (script == Script.Common && previousTypeface is not null)
                {
                    if (TryGetShapeableLength(characterBufferRange, previousTypeface.Value, null, out var fallbackCount, out _))
                    {
                        return new UnshapedTextRun(characterBufferRange.CharacterBufferReference, fallbackCount,
                            defaultProperties.WithTypeface(previousTypeface.Value), biDiLevel);
                    }
                }

                return new UnshapedTextRun(characterBufferRange.CharacterBufferReference, count, defaultProperties.WithTypeface(currentTypeface),
                    biDiLevel);
            }

            if (previousTypeface is not null)
            {
                if (TryGetShapeableLength(characterBufferRange, previousTypeface.Value, defaultTypeface, out count, out _))
                {
                    return new UnshapedTextRun(characterBufferRange.CharacterBufferReference, count,
                        defaultProperties.WithTypeface(previousTypeface.Value), biDiLevel);
                }
            }

            var codepoint = Codepoint.ReplacementCodepoint;

            var codepointEnumerator = new CodepointEnumerator(characterBufferRange.Skip(count));

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

            if (matchFound && TryGetShapeableLength(characterBufferRange, currentTypeface, defaultTypeface, out count, out _))
            {
                //Fallback found
                return new UnshapedTextRun(characterBufferRange.CharacterBufferReference, count, defaultProperties.WithTypeface(currentTypeface),
                    biDiLevel);
            }

            // no fallback found
            currentTypeface = defaultTypeface;

            var glyphTypeface = currentTypeface.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(characterBufferRange);

            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.Current;

                if (!grapheme.FirstCodepoint.IsWhiteSpace && glyphTypeface.TryGetGlyph(grapheme.FirstCodepoint, out _))
                {
                    break;
                }

                count += grapheme.Text.Length;
            }

            return new UnshapedTextRun(characterBufferRange.CharacterBufferReference, count, defaultProperties, biDiLevel);
        }

        /// <summary>
        /// Tries to get a shapeable length that is supported by the specified typeface.
        /// </summary>
        /// <param name="characterBufferRange">The character buffer range to shape.</param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="defaultTypeface"></param>
        /// <param name="length">The shapeable length.</param>
        /// <param name="script"></param>
        /// <returns></returns>
        internal static bool TryGetShapeableLength(
            CharacterBufferRange characterBufferRange,
            Typeface typeface,
            Typeface? defaultTypeface,
            out int length,
            out Script script)
        {
            length = 0;
            script = Script.Unknown;

            if (characterBufferRange.Length == 0)
            {
                return false;
            }

            var font = typeface.GlyphTypeface;
            var defaultFont = defaultTypeface?.GlyphTypeface;

            var enumerator = new GraphemeEnumerator(characterBufferRange);

            while (enumerator.MoveNext())
            {
                var currentGrapheme = enumerator.Current;

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

                length += currentGrapheme.Text.Length;
            }

            return length > 0;
        }
    }
}
