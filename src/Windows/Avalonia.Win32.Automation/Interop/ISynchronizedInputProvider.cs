using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[Guid("fdc8f176-aed2-477a-8c89-5604c66f278d")]
internal enum SynchronizedInputType
{
    KeyUp = 0x01,
    KeyDown = 0x02,
    MouseLeftButtonUp = 0x04,
    MouseLeftButtonDown = 0x08,
    MouseRightButtonUp = 0x10,
    MouseRightButtonDown = 0x20
}
#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("29db1a06-02ce-4cf7-9b42-565d4fab20ee")]
internal partial interface ISynchronizedInputProvider
{
    void StartListening(SynchronizedInputType inputType);
    void Cancel();
}
