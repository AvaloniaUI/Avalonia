using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public sealed class TextTrailingTrimming : TextTrimming
    {
        private readonly ReadOnlySlice<char> _ellipsis;
        private readonly bool _isWordBased;

        public TextTrailingTrimming(char ellipsis, bool isWordBased) : this(new[] {ellipsis}, isWordBased)
        {
        }
        
        public TextTrailingTrimming(char[] ellipsis, bool isWordBased)
        {
            _isWordBased = isWordBased;
            _ellipsis = new ReadOnlySlice<char>(ellipsis);
        }

        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            if (_isWordBased)
            {
                return new TextTrailingWordEllipsis(_ellipsis, createInfo.Width, createInfo.TextRunProperties);
            }

            return new TextTrailingCharacterEllipsis(_ellipsis, createInfo.Width, createInfo.TextRunProperties);
        }

        public override string ToString()
        {
            return _isWordBased ? nameof(WordEllipsis) : nameof(CharacterEllipsis);
        }
    }
}
