using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    internal readonly struct FormattedTextSource : ITextSource
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
                return null;
            }

            var runText = _text.Skip(textSourceIndex);

            if (runText.IsEmpty)
            {
                return new TextEndOfParagraph();
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
