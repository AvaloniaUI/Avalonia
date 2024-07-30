using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Interop.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
#else
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
    [Guid("2360c714-4bf1-4b26-ba65-9b21316127eb")]
    internal partial interface IScrollItemProvider
    {
        void ScrollIntoView();
    }
}
