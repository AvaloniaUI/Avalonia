using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    public enum MouseEventType : int
    {
        LeaveWindow,
        LeftButtonDown,
        LeftButtonUp,
        RightButtonDown,
        RightButtonUp,
        MiddleButtonDown,
        MiddleButtonUp,
        Move,
        Wheel,
        NonClientLeftButtonDown
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseEvent
    {
        public MouseEventType EventType { get; set; }
        public LogicalPosition Position { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardEvent
    {
        public bool Pressed { get; set; }
        public bool Shift { get; set; }
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Logo { get; set; }
        UInt32 VirtualKeyCode { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResizeEvent
    {
        public LogicalSize Size { get; set; }
    }
}
