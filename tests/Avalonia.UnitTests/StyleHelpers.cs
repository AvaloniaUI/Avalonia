using Avalonia.Styling;

#nullable enable

namespace Avalonia.UnitTests
{
    public static class StyleHelpers
    {
        public static void TryAttach(Style style, StyledElement element, object? host = null)
        {
            style.TryAttach(element, host ?? element, PropertyStore.FrameType.Style);
        }
    }
}
