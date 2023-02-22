using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    public readonly record struct TextCollapsingCreateInfo
    {
        public readonly double Width;
        public readonly TextRunProperties TextRunProperties;
        public readonly FlowDirection FlowDirection;

        public TextCollapsingCreateInfo(double width, TextRunProperties textRunProperties, FlowDirection flowDirection)
        {
            Width = width;
            TextRunProperties = textRunProperties;
            FlowDirection = flowDirection;
        }
    }
}
