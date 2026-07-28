using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]

[Guid("6278cab1-b556-4a1a-b4e0-418acc523201")]
internal partial interface IMultipleViewProvider
{
    [return: MarshalAs(UnmanagedType.BStr)]
    string GetViewName(int viewId);
    void SetCurrentView(int viewId);
    int GetCurrentView();
    [return: MarshalUsing(typeof(SafeArrayMarshaller<int>))]
    int[] GetSupportedViews();
}
