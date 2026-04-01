using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("a0a839a9-8da1-4a82-806a-8e0d44e79f56")]
internal partial interface IRawElementProviderSimple2 : IRawElementProviderSimple
{
    void ShowContextMenu();
}
