using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("2360c714-4bf1-4b26-ba65-9b21316127eb")]
internal partial interface IScrollItemProvider
{
    void ScrollIntoView();
}
