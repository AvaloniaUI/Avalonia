using System;
using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Meta = 8,
    }

    [Flags]
    public enum KeyStates
    {
        None = 0,
        Down = 1,
        Toggled = 2,
    }

    [Flags]
    public enum RawInputModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Meta = 8,

        LeftMouseButton = 16,
        RightMouseButton = 32,
        MiddleMouseButton = 64,
        XButton1MouseButton = 128,
        XButton2MouseButton = 256,
        KeyboardMask = Alt | Control | Shift | Meta,

        PenInverted = 512,
        PenEraser = 1024,
        PenBarrelButton = 2048
    }

    [PrivateApi]
    public interface IKeyboardDevice : IInputDevice
    {
    }
}
