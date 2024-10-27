using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
internal partial interface IValueProvider
{
    void SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value);

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetValue();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsReadOnly();
}
