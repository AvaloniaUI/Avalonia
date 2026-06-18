using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
internal partial interface IValueProvider
{
    void SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value);

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetValue();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsReadOnly();
}
