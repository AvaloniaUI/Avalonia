namespace Avalonia.IntegrationTests.Win32;

internal static class InteropExtensions
{
    public static PixelRect ToPixelRect(this UnmanagedMethods.RECT rect)
        => new(new PixelPoint(rect.left, rect.top), new PixelPoint(rect.right, rect.bottom));
}
