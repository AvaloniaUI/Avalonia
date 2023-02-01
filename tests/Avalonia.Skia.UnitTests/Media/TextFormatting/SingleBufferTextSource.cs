using System;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    internal class SingleBufferTextSource : ITextSource
    {
        private readonly string _text;
        private readonly GenericTextRunProperties _defaultGenericPropertiesRunProperties;
        private readonly bool _addEndOfParagraph;

        public SingleBufferTextSource(string text, GenericTextRunProperties defaultProperties, bool addEndOfParagraph = false)
        {
            _text = text;
            _defaultGenericPropertiesRunProperties = defaultProperties;
            _addEndOfParagraph = addEndOfParagraph;
        }

        public TextRun GetTextRun(int textSourceIndex)
        {
            if (textSourceIndex >= _text.Length)
            {
                return _addEndOfParagraph ? new TextEndOfParagraph() : null;
            }

            var runText = _text.AsMemory(textSourceIndex);

            if (runText.IsEmpty)
            {
                return _addEndOfParagraph ? new TextEndOfParagraph() : null;
            }

            return new TextCharacters(runText, _defaultGenericPropertiesRunProperties);
        }
    }
}
