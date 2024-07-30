using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation.Provider;

namespace Avalonia.Win32.Interop.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
#else
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
    [Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    internal partial interface IToggleProvider
    {
        void Toggle();
        ToggleState ToggleState();
    }
}
