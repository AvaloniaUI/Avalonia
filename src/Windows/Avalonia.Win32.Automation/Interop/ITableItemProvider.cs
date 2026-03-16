using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("b9734fa6-771f-4d78-9c90-2517999349cd")]
internal partial interface ITableItemProvider
{
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
    IRawElementProviderSimple[] GetRowHeaderItems();
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
    IRawElementProviderSimple[] GetColumnHeaderItems();
}
