using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    public sealed class TextPathSegmentTrimming : TextTrimming
    {
        private readonly string _ellipsis;

        public TextPathSegmentTrimming(string ellipsis)
        {
            _ellipsis = ellipsis;
        }

        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            return new TextPathSegmentEllipsis(_ellipsis, createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);
        }

        public override string ToString()
        {
            return nameof(PathSegmentEllipsis);
        }
    }
}
