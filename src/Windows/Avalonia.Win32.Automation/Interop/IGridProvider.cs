using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("b17d6187-0907-464b-a168-0ef17a1572b1")]
internal partial interface IGridProvider
{
    IRawElementProviderSimple? GetItem(int row, int column);
    int GetRowCount();
    int GetColumnCount();
}
