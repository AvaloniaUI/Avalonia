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

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("9c860395-97b3-490a-b52a-858cc22af166")]
internal partial interface ITableProvider
{
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
    IRawElementProviderSimple[] GetRowHeaders();
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
    IRawElementProviderSimple[] GetColumnHeaders();
    RowOrColumnMajor GetRowOrColumnMajor();
}
