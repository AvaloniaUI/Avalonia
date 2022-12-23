﻿using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal readonly struct FormattedTextSource : ITextSource
    {
        private readonly CharacterBufferRange _text;
        private readonly TextRunProperties _defaultProperties;
        private readonly IReadOnlyList<ValueSpan<TextRunProperties>>? _textModifier;

        public FormattedTextSource(string text, TextRunProperties defaultProperties,
            IReadOnlyList<ValueSpan<TextRunProperties>>? textModifier)
        {
            _text = new CharacterBufferRange(text);
            _defaultProperties = defaultProperties;
            _textModifier = textModifier;
        }

        public TextRun? GetTextRun(int textSourceIndex)
        {
            if (textSourceIndex > _text.Length)
            {
                return null;
            }

            var runText = _text.Skip(textSourceIndex);

            if (runText.IsEmpty)
            {
                return null;
            }

            var textStyleRun = CreateTextStyleRun(runText, textSourceIndex, _defaultProperties, _textModifier);

            return new TextCharacters(runText.Take(textStyleRun.Length).CharacterBufferReference, textStyleRun.Length, textStyleRun.Value);
        }

        /// <summary>
        /// Creates a span of text run properties that has modifier applied.
        /// </summary>
        /// <param name="text">The text to create the properties for.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="defaultProperties">The default text properties.</param>
        /// <param name="textModifier">The text properties modifier.</param>
        /// <returns>
        /// The created text style run.
        /// </returns>
        private static ValueSpan<TextRunProperties> CreateTextStyleRun(CharacterBufferRange text, int firstTextSourceIndex,
            TextRunProperties defaultProperties, IReadOnlyList<ValueSpan<TextRunProperties>>? textModifier)
        {
            if (textModifier == null || textModifier.Count == 0)
            {
                return new ValueSpan<TextRunProperties>(firstTextSourceIndex, text.Length, defaultProperties);
            }

            var currentProperties = defaultProperties;

            var hasOverride = false;

            var i = 0;

            var length = 0;

            for (; i < textModifier.Count; i++)
            {
                var propertiesOverride = textModifier[i];

                var textRange = new TextRange(propertiesOverride.Start, propertiesOverride.Length);

                if (textRange.Start + textRange.Length <= firstTextSourceIndex)
                {
                    continue;
                }

                if (textRange.Start > firstTextSourceIndex + text.Length)
                {
                    length = text.Length;
                    break;
                }

                if (textRange.Start > firstTextSourceIndex)
                {
                    if (propertiesOverride.Value != currentProperties)
                    {
                        length = Math.Min(Math.Abs(textRange.Start - firstTextSourceIndex), text.Length);

                        break;
                    }
                }

                length = Math.Max(0, textRange.Start + textRange.Length - firstTextSourceIndex);

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

            if (length == 0 && currentProperties != defaultProperties)
            {
                currentProperties = defaultProperties;
                length = text.Length;
            }

            length = CoerceLength(text, length);

            return new ValueSpan<TextRunProperties>(firstTextSourceIndex, length, currentProperties);
        }

        private static int CoerceLength(CharacterBufferRange text, int length)
        {
            var finalLength = 0;

            var graphemeEnumerator = new GraphemeEnumerator(text);

            while (graphemeEnumerator.MoveNext())
            {
                var grapheme = graphemeEnumerator.Current;

                finalLength += grapheme.Text.Length;

                if (finalLength >= length)
                {
                    return finalLength;
                }
            }

            return Math.Min(length, text.Length);
        }
    }
}
