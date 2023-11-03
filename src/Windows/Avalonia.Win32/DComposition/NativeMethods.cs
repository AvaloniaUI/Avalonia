using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.DComposition;

internal class NativeMethods
{
    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionCreateDevice2( /* _In_opt_ */
        IntPtr renderingDevice, /* _In_ */
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid, /* _Outptr_ */ out IntPtr dcompositionDevice);
}
