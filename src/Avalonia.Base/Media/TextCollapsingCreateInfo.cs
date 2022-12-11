using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    public readonly record struct TextCollapsingCreateInfo
    {
        public readonly double Width;
        public readonly TextRunProperties TextRunProperties;

        public TextCollapsingCreateInfo(double width, TextRunProperties textRunProperties)
        {
            Width = width;
            TextRunProperties = textRunProperties;
        }
    }
}
