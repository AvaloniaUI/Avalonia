using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Interop.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComInterface]
#else
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
    [Guid("d02541f1-fb81-4d64-ae32-f520f8a6dbd1")]
    internal partial interface IGridItemProvider
    {
        int Row();
        int Column();
        int RowSpan();
        int ColumnSpan();
        IRawElementProviderSimple ContainingGrid();
    }
}
