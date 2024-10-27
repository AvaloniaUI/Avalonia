using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("d02541f1-fb81-4d64-ae32-f520f8a6dbd1")]
internal partial interface IGridItemProvider
{
    int GetRow();
    int GetColumn();
    int GetRowSpan();
    int GetColumnSpan();
    IRawElementProviderSimple GetContainingGrid();
}
