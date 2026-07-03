namespace Avalonia.X11.Selections.DragDrop;

internal static class XdndConstants
{
    // Spec: every application that supports XDND version N must also support all previous versions (3 to N-1).
    public const byte MinXdndVersion = 3;
    public const byte XdndVersion = 5;
}
