using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("a0a839a9-8da1-4a82-806a-8e0d44e79f56")]
internal partial interface IRawElementProviderSimple2 : IRawElementProviderSimple
{
#if !NET8_0_OR_GREATER
    // Hack for the legacy COM interop
    // See https://learn.microsoft.com/en-us/dotnet/standard/native-interop/comwrappers-source-generation#derived-interfaces
    new ProviderOptions GetProviderOptions();
    [return: MarshalAs(UnmanagedType.Interface)]
    new object? GetPatternProvider(int patternId);
    [return: MarshalAs(UnmanagedType.Struct)]
    new object? GetPropertyValue(int propertyId);
    new IRawElementProviderSimple? GetHostRawElementProvider();
#endif
    void ShowContextMenu();
}
