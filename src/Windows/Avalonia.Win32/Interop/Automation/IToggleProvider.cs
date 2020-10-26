using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("ad7db4af-7166-4478-a402-ad5b77eab2fa")]
    internal enum ToggleState
    {
        Off,
        On,
        Indeterminate
    }

    [ComVisible(true)]
    [Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IToggleProvider
    {
        void Toggle( );
        ToggleState ToggleState { get; }
    }
}
