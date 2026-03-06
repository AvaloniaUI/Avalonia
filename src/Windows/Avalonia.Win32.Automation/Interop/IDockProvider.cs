using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[Guid("70d46e77-e3a8-449d-913c-e30eb2afecdb")]
internal enum DockPosition
{
    Top,
    Left,
    Bottom,
    Right,
    Fill,
    None
}

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("159bc72c-4ad3-485e-9637-d7052edf0146")]
internal partial interface IDockProvider
{
    void SetDockPosition(DockPosition dockPosition);
    DockPosition GetDockPosition();
}
