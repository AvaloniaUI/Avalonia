using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
internal partial interface ISelectionProvider
{
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
    IRawElementProviderSimple[] GetSelection();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool CanSelectMultiple();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsSelectionRequired();
}
