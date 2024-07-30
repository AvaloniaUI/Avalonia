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
    [Guid("a0a839a9-8da1-4a82-806a-8e0d44e79f56")]
    internal unsafe partial interface IRawElementProviderSimple2 : IRawElementProviderSimple
    {
        void ShowContextMenu();
    }
}
