using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    internal class FormattableTextSource : ITextSource
    {
        private readonly ReadOnlySlice<char> _text;
        private readonly TextRunProperties _defaultStyle;
        private ReadOnlySlice<ValueSpan<TextRunProperties>> _styleSpans;

        public FormattableTextSource(string text, TextRunProperties defaultStyle,
            ReadOnlySlice<ValueSpan<TextRunProperties>> styleSpans)
        {
            _text = text.AsMemory();

            _defaultStyle = defaultStyle;

            _styleSpans = styleSpans;
        }

        public TextRun GetTextRun(int textSourceIndex)
        {
            if (_styleSpans.IsEmpty)
            {
                return new TextEndOfParagraph();
            }

            var currentSpan = _styleSpans[0];

            _styleSpans = _styleSpans.Skip(1);

            return new TextCharacters(_text.AsSlice(currentSpan.Start, currentSpan.Length),
                _defaultStyle);
        }
    }
}
