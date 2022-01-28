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
        internal IList<ShapeableTextCharacters> GetShapeableCharacters(ReadOnlySlice<char> runText, sbyte biDiLevel)
        {
            var shapeableCharacters = new List<ShapeableTextCharacters>(2);

            while (!runText.IsEmpty)
            {
                var shapeableRun = CreateShapeableRun(runText, Properties, biDiLevel);

                shapeableCharacters.Add(shapeableRun);

                runText = runText.Skip(shapeableRun.Text.Length);
            }

            return shapeableCharacters;
        }

        /// <summary>
        /// Creates a shapeable text run with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultProperties">The default text run properties.</param>
        /// <param name="biDiLevel">The bidi level of the run.</param>
        /// <returns>A list of shapeable text runs.</returns>
        private ShapeableTextCharacters CreateShapeableRun(ReadOnlySlice<char> text, TextRunProperties defaultProperties, sbyte biDiLevel)
        {
            var defaultTypeface = defaultProperties.Typeface;

            var currentTypeface = defaultTypeface;

            if (TryGetShapeableLength(text, currentTypeface, defaultTypeface, out var count))
            {
                return new ShapeableTextCharacters(text.Take(count),
                    new GenericTextRunProperties(currentTypeface, defaultProperties.FontRenderingEmSize,
                        defaultProperties.TextDecorations, defaultProperties.ForegroundBrush), biDiLevel);
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
                    defaultTypeface.FontFamily, defaultProperties.CultureInfo, out currentTypeface);

            if (matchFound && TextCharacters.TryGetShapeableLength(text, currentTypeface, defaultTypeface, out count))
            {
                //Fallback found
                return new ShapeableTextCharacters(text.Take(count),
                    new GenericTextRunProperties(currentTypeface, defaultProperties.FontRenderingEmSize,
                    defaultProperties.TextDecorations, defaultProperties.ForegroundBrush), biDiLevel);
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

            return new ShapeableTextCharacters(text.Take(count),
                new GenericTextRunProperties(currentTypeface, defaultProperties.FontRenderingEmSize,
                    defaultProperties.TextDecorations, defaultProperties.ForegroundBrush), biDiLevel);
        }

        /// <summary>
        /// Tries to get run properties.
        /// </summary>
        /// <param name="defaultTypeface"></param>
        /// <param name="text"></param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected static bool TryGetShapeableLength(ReadOnlySlice<char> text, Typeface typeface, Typeface defaultTypeface,
            out int length)
        {
            if (text.Length == 0)
            {
                length = 0;
                return false;
            }

            var isFallback = typeface != defaultTypeface;

            length = 0;
            var script = Script.Unknown;

            var font = typeface.GlyphTypeface;
            var defaultFont = defaultTypeface.GlyphTypeface;
            
            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext())
            {
                var currentGrapheme = enumerator.Current;

                var currentScript = currentGrapheme.FirstCodepoint.Script;

                if (currentScript != script)
                {
                    if (script is Script.Unknown || currentScript != Script.Common && (script is Script.Common || script is Script.Inherited))
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

                //Only handle non whitespace here
                if(!currentGrapheme.FirstCodepoint.IsWhiteSpace)
                {
                    //Stop at the first glyph that is present in the default typeface.
                    if (isFallback && defaultFont.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                    {
                        break;
                    }

                    //Stop at the first missing glyph
                    if (!font.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                    {
                        break;
                    }
                }

                if (!currentGrapheme.FirstCodepoint.IsWhiteSpace && !font.TryGetGlyph(currentGrapheme.FirstCodepoint, out _))
                {
                    break;
                }

                length += currentGrapheme.Text.Length;
            }

            return length > 0;
        }
    }
}
