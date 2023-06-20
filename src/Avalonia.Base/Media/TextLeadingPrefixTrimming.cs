using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    public sealed class TextLeadingPrefixTrimming : TextTrimming
    {
        private readonly string _ellipsis;
        private readonly int _prefixLength;

        public TextLeadingPrefixTrimming(string ellipsis, int prefixLength)
        {
            _prefixLength = prefixLength;
            _ellipsis = ellipsis;
        }

        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            return new TextLeadingPrefixCharacterEllipsis(_ellipsis, _prefixLength, createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);
        }

        public override string ToString()
        {
            return nameof(PrefixCharacterEllipsis);
        }
    }
}
