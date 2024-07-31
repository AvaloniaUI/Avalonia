using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Interop.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComInterface]
#else
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
    [Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
    internal partial interface ISelectionProvider
    {
#if NET8_0_OR_GREATER
        [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
        IReadOnlyList<IRawElementProviderSimple> GetSelection();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool CanSelectMultiple();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsSelectionRequired();
    }
}
