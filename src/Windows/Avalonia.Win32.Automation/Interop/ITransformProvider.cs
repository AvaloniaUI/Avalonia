using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("6829ddc4-4f91-4ffa-b86f-bd3e2987cb4c")]
internal partial interface ITransformProvider
{
    void Move(double x, double y);
    void Resize(double width, double height);
    void Rotate(double degrees);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetCanMove();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetCanResize();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetCanRotate();
}
