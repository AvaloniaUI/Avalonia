using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Wayland;

internal sealed class TitleBarMoveDragHelper
{
    private RawPointerEventArgs? _press;

    public void Begin(RawPointerEventArgs press) => _press = press;

    public bool HandleEvent(RawPointerEventArgs pointer, out RawPointerEventArgs? movePress)
    {
        movePress = null;
        if (_press is not { } press)
            return false;

        // A hover without the left button means the release was missed.
        if (pointer.Type == RawPointerEventType.Move && (pointer.InputModifiers & RawInputModifiers.LeftMouseButton) != 0)
        {
            var tapSize = AvaloniaLocator.Current.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Mouse)
                ?? new Size(4, 4);
            var delta = pointer.Position - press.Position;
            if (Math.Abs(delta.X) <= tapSize.Width / 2 && Math.Abs(delta.Y) <= tapSize.Height / 2)
                return true;

            movePress = press;
        }

        _press = null;
        return movePress != null || pointer.Type == RawPointerEventType.LeftButtonUp;
    }
}
