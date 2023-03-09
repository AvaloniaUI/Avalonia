using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    public sealed class TextTrailingTrimming : TextTrimming
    {
        private readonly string _ellipsis;
        private readonly bool _isWordBased;
        
        public TextTrailingTrimming(string ellipsis, bool isWordBased)
        {
            _isWordBased = isWordBased;
            _ellipsis = ellipsis;
        }

        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            if (_isWordBased)
            {
                return new TextTrailingWordEllipsis(_ellipsis, createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);
            }

            return new TextTrailingCharacterEllipsis(_ellipsis, createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);
        }

        public override string ToString()
        {
            return _isWordBased ? nameof(WordEllipsis) : nameof(CharacterEllipsis);
        }
    }
}
