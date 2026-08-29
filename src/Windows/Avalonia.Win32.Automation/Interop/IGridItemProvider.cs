using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("d02541f1-fb81-4d64-ae32-f520f8a6dbd1")]
internal partial interface IGridItemProvider
{
    int GetRow();
    int GetColumn();
    int GetRowSpan();
    int GetColumnSpan();
    IRawElementProviderSimple GetContainingGrid();
}
