using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Avalonia.Windowing.Bindings
{
    public delegate void MouseEventCallback(WindowId windowId, MouseEvent mouseEvent);
    public delegate void ResizeEventCallback(WindowId windowId, ResizeEvent resizeEvent);
    public delegate void KeyboardEventCallback(WindowId windowId, KeyboardEvent keyboardEvent);
    public delegate void CharacterEventCallback(WindowId windowId, CharacterEvent characterEvent);
    public delegate void AwakenedEventCallback();
    public delegate byte ShouldExitEventLoopCallback(WindowId windowId);
    public delegate void CloseRequestedCallback(WindowId windowId);
    public delegate void FocusedCallback(WindowId windowId, byte focused);

    [StructLayout(LayoutKind.Sequential)]
    public struct EventNotifier
    {
        public MouseEventCallback OnMouseEvent;
        public AwakenedEventCallback OnAwakened;
        public ResizeEventCallback OnResized;
        public KeyboardEventCallback OnKeyboardEvent;
        public CharacterEventCallback OnCharacterEvent;
        public ShouldExitEventLoopCallback OnShouldExitEventLoop;
        public CloseRequestedCallback OnCloseRequested;
        public FocusedCallback OnFocused;
    }
}
