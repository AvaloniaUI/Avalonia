﻿using Avalonia.Media.TextFormatting;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    internal class SingleBufferTextSource : ITextSource
    {
        private readonly CharacterBufferRange _text;
        private readonly GenericTextRunProperties _defaultGenericPropertiesRunProperties;

        public SingleBufferTextSource(string text, GenericTextRunProperties defaultProperties)
        {
            _text = new CharacterBufferRange(text);
            _defaultGenericPropertiesRunProperties = defaultProperties;
        }

        public TextRun GetTextRun(int textSourceIndex)
        {
            if (textSourceIndex >= _text.Length)
            {
                return null;
            }

            var runText = _text.Skip(textSourceIndex);

            if (runText.IsEmpty)
            {
                return null;
            }

            return new TextCharacters(runText.CharacterBufferReference, runText.Length, _defaultGenericPropertiesRunProperties);
        }
    }
}
