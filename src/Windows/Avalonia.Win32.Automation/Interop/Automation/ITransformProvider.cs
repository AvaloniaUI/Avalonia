using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Interop.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
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
        bool CanMove();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool CanResize();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool CanRotate();
    }
}
