using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Avalonia.Windowing.Bindings
{
    public delegate void MouseEventCallback(IntPtr windowId, MouseEvent mouseEvent);
    public delegate void ResizeEventCallback(IntPtr windowId, ResizeEvent resizeEvent);
    public delegate void KeyboardEventCallback(IntPtr windowId, KeyboardEvent keyboardEvent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    public delegate void AwakenedEventCallback();
     
    [StructLayout(LayoutKind.Sequential)]
    public struct EventNotifier
    {
        public MouseEventCallback OnMouseEvent;
        public AwakenedEventCallback OnAwakened;
        public ResizeEventCallback OnResized;
        public KeyboardEventCallback OnKeyboardEvent;
    }
}
