using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.LinuxFramebuffer.Input
{
    public interface IInputBackend
    {
        void Initialize(IScreenInfoProvider info, Action<RawInputEventArgs> onInput);
        void SetInputRoot(IInputRoot root);
        void SetMouse(IMouseDevice mouse);
        void SetKeyboard(IKeyboardDevice kbd);
        void SetTouch(TouchDevice touch);
    }
}
