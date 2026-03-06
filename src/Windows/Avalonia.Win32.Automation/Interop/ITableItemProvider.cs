using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("b9734fa6-771f-4d78-9c90-2517999349cd")]
internal partial interface ITableItemProvider
{
#if NET8_0_OR_GREATER
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
    IRawElementProviderSimple[] GetRowHeaderItems();
#if NET8_0_OR_GREATER
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
    IRawElementProviderSimple[] GetColumnHeaderItems();
}
