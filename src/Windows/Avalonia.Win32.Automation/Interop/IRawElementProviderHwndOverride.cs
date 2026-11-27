using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("1d5df27c-8947-4425-b8d9-79787bb460b8")]
internal partial interface IRawElementProviderHwndOverride
{
    IRawElementProviderSimple? GetOverrideProviderForHwnd(IntPtr hwnd);
}
