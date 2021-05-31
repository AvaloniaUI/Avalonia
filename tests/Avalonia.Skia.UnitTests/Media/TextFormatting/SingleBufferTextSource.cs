using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    internal class SingleBufferTextSource : ITextSource
    {
        private readonly ReadOnlySlice<char> _text;
        private readonly GenericTextRunProperties _defaultGenericPropertiesRunProperties;

        public SingleBufferTextSource(string text, GenericTextRunProperties defaultProperties)
        {
            _text = text.AsMemory();
            _defaultGenericPropertiesRunProperties = defaultProperties;
        }

        public TextRun GetTextRun(int textSourceIndex)
        {
            if (textSourceIndex > _text.Length)
            {
                return null;
            }
            
            var runText = _text.Skip(textSourceIndex);
            
            if (runText.IsEmpty)
            {
                return new TextEndOfParagraph();
            }

            return new TextCharacters(runText, _defaultGenericPropertiesRunProperties);
        }
    }
}
