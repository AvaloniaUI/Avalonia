using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
internal partial interface IExpandCollapseProvider
{
    void Expand();
    void Collapse();
    ExpandCollapseState GetExpandCollapseState();
}
