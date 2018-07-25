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
        public byte Pressed { get; set; }
        public byte Shift { get; set; }
        public byte Control { get; set; }
        public byte Alt { get; set; }
        public byte Logo { get; set; }
        public VirtualKeyCode VirtualKeyCode { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResizeEvent
    {
        public LogicalSize Size { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CharacterEvent 
    {
        public char Character { get; set; }    
    }
}
