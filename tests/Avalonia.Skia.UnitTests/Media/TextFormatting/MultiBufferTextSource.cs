using Avalonia.Media.TextFormatting;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    internal class MultiBufferTextSource : ITextSource
    {
        private readonly string[] _runTexts;
        private readonly GenericTextRunProperties _defaultStyle;

        public MultiBufferTextSource(GenericTextRunProperties defaultStyle)
        {
            _defaultStyle = defaultStyle;

            _runTexts = new[] { "A123456789", "B123456789", "C123456789", "D123456789", "E123456789" };
        }

        public static TextRange TextRange => new TextRange(0, 50);

        public TextRun GetTextRun(int textSourceIndex)
        {
            if (textSourceIndex >= 50)
            {
                return null;
            }

            var index = textSourceIndex / 10;

            var runText = _runTexts[index];

            return new TextCharacters(runText, _defaultStyle);
        }
    }
}
