using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("2acad808-b2d4-452d-a407-91ff1ad167b2")]
internal partial interface ISelectionItemProvider
{
    void Select();
    void AddToSelection();
    void RemoveFromSelection();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsSelected();

    IRawElementProviderSimple? GetSelectionContainer();
}
