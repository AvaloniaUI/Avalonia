using Avalonia.Styling;

#nullable enable

namespace Avalonia.UnitTests
{
    public static class StyleHelpers
    {
        public static SelectorMatchResult TryAttach(Style style, StyledElement element, object? host = null)
        {
            return style.TryAttach(element, host ?? element, PropertyStore.FrameType.Style);
        }
    }
}
