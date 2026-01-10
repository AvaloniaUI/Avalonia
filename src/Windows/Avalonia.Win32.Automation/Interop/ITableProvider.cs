using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[Guid("15fdf2e2-9847-41cd-95dd-510612a025ea")]
internal enum RowOrColumnMajor
{
    RowMajor,
    ColumnMajor,
    Indeterminate,
}
#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("9c860395-97b3-490a-b52a-858cc22af166")]
internal partial interface ITableProvider
{
#if NET8_0_OR_GREATER
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
    IRawElementProviderSimple[] GetRowHeaders();
#if NET8_0_OR_GREATER
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
    IRawElementProviderSimple[] GetColumnHeaders();
    RowOrColumnMajor GetRowOrColumnMajor();
}
